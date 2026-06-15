using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services.Events;

/// <summary>
/// BackgroundService que gera eventos de racha automaticamente a partir dos
/// RachaSchedules ativos, mantendo uma janela deslizante de 8 semanas à frente.
/// Roda diariamente após a inicialização do app.
/// </summary>
public class RachaSchedulerService : BackgroundService
{
    private const int WeeksAhead = 8;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RachaSchedulerService> _logger;

    public RachaSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<RachaSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda a inicialização completa do app antes de começar.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RachaSchedulerService: erro ao gerar eventos recorrentes.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    internal async Task GenerateEventsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<EventNotificationService>();
        var collisionService = scope.ServiceProvider.GetRequiredService<EventCollisionService>();

        var schedules = await db.RachaSchedules
            .Where(rs => rs.IsActive)
            .Include(rs => rs.Venue)
            .Include(rs => rs.Group)
            .ToListAsync(ct);

        if (schedules.Count == 0) return;

        var windowEnd = DateTime.UtcNow.AddDays(WeeksAhead * 7);
        var today     = DateTime.UtcNow.Date;
        var newEvents = new List<Event>();

        foreach (var schedule in schedules)
        {
            var occurrences = GetOccurrences(today, schedule.DayOfWeek, schedule.TimeOfDay, windowEnd);

            foreach (var startsAt in occurrences)
            {
                // Evita colisões com qualquer partida do mesmo grupo no mesmo horário.
                if (await collisionService.HasGroupTimeCollisionAsync(schedule.GroupId, startsAt, null, ct))
                    continue;

                var ev = new Event
                {
                    GroupId         = schedule.GroupId,
                    Sport           = Sport.Futsal,
                    VenueId         = schedule.VenueId,
                    Location        = schedule.Venue.Address,
                    StartsAt        = startsAt,
                    DurationMinutes = schedule.DurationMinutes,
                    Price           = schedule.Price,
                    MaxPlayers      = schedule.MaxPlayers > 0 ? schedule.MaxPlayers : 20,
                    MaxGoalkeepers  = schedule.MaxGoalkeepers,
                    LocalName       = schedule.LocalName,
                    RachaScheduleId = schedule.Id,
                    CreatedByUserId = schedule.CreatedByUserId,
                    IsActive        = true,
                };

                db.Events.Add(ev);
                newEvents.Add(ev);
            }
        }

        if (newEvents.Count == 0) return;

        await db.SaveChangesAsync(ct); // popula ev.Id em cada evento
        _logger.LogInformation(
            "RachaSchedulerService: {Count} evento(s) gerado(s) para a janela de {Weeks} semanas.",
            newEvents.Count, WeeksAhead);

        // Notificações de eventos recorrentes são enviadas diariamente às 8h da manhã
        // via EventNotificationSchedulerService (veja EventNotificationSchedulerService.cs)
    }

    /// <summary>
    /// Retorna todas as ocorrências de um DayOfWeek/TimeOfDay entre hoje e windowEnd (UTC).
    /// </summary>
    internal static IEnumerable<DateTime> GetOccurrences(
        DateTime from, DayOfWeek dayOfWeek, TimeOnly timeOfDay, DateTime windowEnd)
    {
        // Avança até o próximo dia da semana correto
        int daysUntil = ((int)dayOfWeek - (int)from.DayOfWeek + 7) % 7;
        var cursor = from.AddDays(daysUntil);

        while (cursor <= windowEnd)
        {
            var startsAt = DateTime.SpecifyKind(
                cursor.Date.Add(timeOfDay.ToTimeSpan()),
                DateTimeKind.Utc);

            yield return startsAt;
            cursor = cursor.AddDays(7);
        }
    }
}
