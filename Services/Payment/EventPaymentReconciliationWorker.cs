using Confirmai.Services.Events;
using Confirmai.Configuration;
using Confirmai.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

public sealed class EventPaymentReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventPaymentReconciliationWorker> _logger;
    private readonly TimeSpan _expiryWindow;

    public EventPaymentReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EventPaymentReconciliationWorker> logger,
        IOptions<EfiBankOptions> efiBankOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        // Use 2× the configured Pix expiry as the expiration window.
        // Default EfiBank expiry is 3600s (1h), so charges older than 2h get marked Expired.
        _expiryWindow = TimeSpan.FromSeconds(efiBankOptions.Value.PixExpiresInSeconds * 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventPaymentReconciliationWorker: erro inesperado na rotina automática.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var reconciliation = scope.ServiceProvider.GetRequiredService<EventPaymentReconciliationService>();

        var sweep = await reconciliation.ReconcilePendingConfirmationsAsync(take: 50, actorUserId: null, cancellationToken: ct);

        _logger.LogInformation(
            "EventPaymentReconciliationWorker: varredura concluída. Considerados={Considered}, Atualizados={Updated}, Pendentes={Pending}, NãoEncontrados={NotFound}.",
            sweep.Considered, sweep.Updated, sweep.StillPending, sweep.NotFound);

        var expired = await reconciliation.ExpireStalePixChargesAsync(_expiryWindow, ct);
        if (expired > 0)
            _logger.LogInformation(
                "EventPaymentReconciliationWorker: {Expired} cobrança(s) Pix expirada(s).", expired);
    }
}
