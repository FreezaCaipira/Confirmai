using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Events;

public enum EventCancellationResult
{
    Success,
    NotFound,
    Forbidden,
}

/// <summary>
/// Cancelamento de evento (futsal/poker). Guarda única: criador do evento ou
/// sysadmin. Extraído das pages no C34 Fase 3b para tornar a permissão testável
/// e garantir audit <c>event.cancelled</c> em todos os esportes.
/// </summary>
public class EventCancellationService(
    IDbContextFactory<AppDbContext> dbFactory,
    EventNotificationService notifications,
    LogService log)
{
    /// <summary>Regra de quem pode gerenciar (editar/cancelar) o evento carregado.</summary>
    public static bool CanManage(Event ev, string? userId, bool isAdmin)
        => ev.CreatedByUserId == userId || isAdmin;

    public async Task<EventCancellationResult> CancelAsync(int eventId, string? userId, bool isAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return EventCancellationResult.NotFound;
        if (!CanManage(ev, userId, isAdmin)) return EventCancellationResult.Forbidden;


        ev.IsActive = false;
        await db.SaveChangesAsync();

        await notifications.NotifyEventCancelledAsync(eventId, userId!);
        await log.AuditAsync(
            AuditEvents.EventCancelled,
            AuditEntities.Event,
            eventId.ToString(),
            $"Evento #{eventId} cancelado pelo admin/criador",
            userId, "EventEdit");
        return EventCancellationResult.Success;
    }
}
