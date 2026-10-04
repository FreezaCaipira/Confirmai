using Confirmai.Services.Notification;

namespace Confirmai.Services.Notification;

/// <summary>
/// C31 itens 2-3 — sweeps every 15 minutes and asks the dispatch service to
/// send the day-of and one-hour reminders. All idempotency and guards live in
/// WhatsAppDispatchService; this is only the timer.
/// </summary>
public sealed class WhatsAppReminderSchedulerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhatsAppReminderSchedulerService> _logger;

    public WhatsAppReminderSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<WhatsAppReminderSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dispatch = scope.ServiceProvider.GetRequiredService<WhatsAppDispatchService>();
                var attempts = await dispatch.RunReminderSweepAsync(DateTime.UtcNow, stoppingToken);
                if (attempts > 0)
                    _logger.LogInformation("WhatsApp sweep: {Attempts} lembrete(s) processado(s).", attempts);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsAppReminderSchedulerService: erro no sweep de lembretes.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
