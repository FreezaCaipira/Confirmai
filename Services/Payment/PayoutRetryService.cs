using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Payment;

/// <summary>
/// Background service that retries failed Pix payouts with exponential backoff.
/// Runs every 5 minutes, delegates actual retry logic to PayoutService.RetryFailedPayoutsAsync.
/// </summary>
public class PayoutRetryService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PayoutRetryService> _logger;
    private Timer _timer = default!;

    public PayoutRetryService(
        IServiceScopeFactory scopeFactory,
        ILogger<PayoutRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayoutRetryService iniciado");

        _timer = new Timer(
            async _ => await ProcessRetriesAsync(),
            null,
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(5));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayoutRetryService parado");
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task ProcessRetriesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var payoutService = scope.ServiceProvider.GetRequiredService<PayoutService>();
            await payoutService.RetryFailedPayoutsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar retentativas de payout");
        }
    }
}
