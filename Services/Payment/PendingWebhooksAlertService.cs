using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Payment;

/// <summary>
/// Monitora webhooks Pix pendentes por >24h e emite alertas via logs.
/// Executado a cada hora via IHostedService.
/// </summary>
public class PendingWebhooksAlertService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingWebhooksAlertService> _logger;
    private Timer _timer;
    private const int AlertThreshold = 5; // Alertar se > 5 pagamentos pendentes

    public PendingWebhooksAlertService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingWebhooksAlertService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PendingWebhooksAlertService iniciado");
        
        // Inicia verificação a cada 1 hora
        _timer = new Timer(
            async _ => await CheckPendingWebhooksAsync(),
            null,
            TimeSpan.FromMinutes(5), // Primeira verificação após 5min
            TimeSpan.FromHours(1)     // Depois a cada hora
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PendingWebhooksAlertService parado");
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task CheckPendingWebhooksAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Contar webhooks Pix pendentes há >24h
            var pendingCount = await db.EventConfirmations
                .Where(ec => ec.HasPaid == false
                    && ec.PaymentStatus == EventConfirmationPaymentStatus.Pending
                    && ec.ConfirmedAt < DateTime.UtcNow.AddHours(-24)
                    && ec.Event.StartsAt > DateTime.UtcNow) // Evento no futuro
                .CountAsync();

            if (pendingCount > AlertThreshold)
            {
                _logger.LogWarning(
                    "🚨 ALERTA: {PendingCount} confirmações de pagamento Pix pendentes há >24h. " +
                    "Verificar webhooks em EfiBank/BTCPay e firewall.",
                    pendingCount
                );
            }
            else if (pendingCount > 0)
            {
                _logger.LogInformation(
                    "ℹ️ {PendingCount} confirmações de pagamento Pix pendentes (< threshold)",
                    pendingCount
                );
            }
            else
            {
                _logger.LogDebug("✅ Nenhum webhook Pix pendente detectado");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar webhooks pendentes");
        }
    }
}
