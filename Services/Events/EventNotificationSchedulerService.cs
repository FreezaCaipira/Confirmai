using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Models;

namespace Confirmai.Services.Events
{
    /// <summary>
    /// BackgroundService que envia notificações sobre partidas recorrentes
    /// apenas na manhã do dia do jogo (8h da manhã).
    /// 
    /// Roda diariamente, verifica quais eventos começam naquele dia,
    /// e notifica os membros do grupo (exceto o criador).
    /// </summary>
    public class EventNotificationSchedulerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventNotificationSchedulerService> _logger;

        public EventNotificationSchedulerService(
            IServiceScopeFactory scopeFactory,
            ILogger<EventNotificationSchedulerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Aguarda a inicialização completa do app antes de começar.
            await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndNotifyTodaysEventsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EventNotificationSchedulerService: erro ao notificar eventos do dia.");
                }

                // Aguarda até a próxima execução (1 hora após 8h da manhã do próximo dia)
                var nextRun = GetNextNotificationTime();
                var delay = nextRun - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    // Se já passou das 8h, espera até amanhã
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
            }
        }

        /// <summary>
        /// Calcula a próxima vez que a notificação deve ser executada.
        /// Sempre às 8h da manhã (horário UTC).
        /// </summary>
        private static DateTime GetNextNotificationTime()
        {
            var nowUtc = DateTime.UtcNow;
            var todayNotificationTime = nowUtc.Date.AddHours(8);

            if (nowUtc < todayNotificationTime)
            {
                return todayNotificationTime;
            }
            else
            {
                return todayNotificationTime.AddDays(1);
            }
        }

        /// <summary>
        /// Verifica eventos que começam "hoje" (conforme horário local de cada evento)
        /// e envia notificações aos membros do grupo.
        /// </summary>
        private async Task CheckAndNotifyTodaysEventsAsync(CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<EventNotificationService>();

            var nowUtc = DateTime.UtcNow;
            var todayStart = nowUtc.Date;
            var todayEnd = todayStart.AddDays(1);

            // Busca eventos que começam entre as 00:00 e 23:59 de hoje (UTC)
            var todaysEvents = await db.Events
                .Where(e =>
                    e.IsActive &&
                    e.StartsAt >= todayStart &&
                    e.StartsAt < todayEnd &&
                    e.RachaScheduleId.HasValue) // Apenas eventos recorrentes
                .ToListAsync(ct);

            if (todaysEvents.Count == 0)
            {
                _logger.LogInformation(
                    "EventNotificationSchedulerService: Nenhum evento recorrente agendado para hoje.");
                return;
            }

            _logger.LogInformation(
                "EventNotificationSchedulerService: {Count} evento(s) recorrente(s) agendado(s) para hoje. Notificando...",
                todaysEvents.Count);

            // Notifica para cada evento (best-effort)
            foreach (var ev in todaysEvents)
            {
                try
                {
                    await notificationService.NotifyNewRecurringEventAsync(ev.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "EventNotificationSchedulerService: falha ao notificar evento {EventId}.", ev.Id);
                }
            }
        }
    }
}

