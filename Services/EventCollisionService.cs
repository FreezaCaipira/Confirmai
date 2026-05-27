using Confirmai.Data;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

public class EventCollisionService
{
    public const string OpenExistingEventLinkText = "Abrir partida existente";

    public static string BuildConflictMessage(DateTime startsAtUtc)
    {
        var whenLocal = startsAtUtc.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
        return $"Conflito de horário: já existe uma partida deste grupo em {whenLocal}.";
    }

    public sealed class EventCollisionInfo
    {
        public int EventId { get; init; }
        public int GroupId { get; init; }
        public DateTime StartsAt { get; init; }
    }

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EventCollisionService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<bool> HasGroupTimeCollisionAsync(
        int groupId,
        DateTime startsAt,
        int? excludingEventId = null,
        CancellationToken cancellationToken = default)
    {
        return await FindGroupTimeCollisionAsync(groupId, startsAt, excludingEventId, cancellationToken) is not null;
    }

    public async Task<EventCollisionInfo?> FindGroupTimeCollisionAsync(
        int groupId,
        DateTime startsAt,
        int? excludingEventId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Events
            .Where(e => e.GroupId == groupId
                     && e.StartsAt == startsAt
                     && (!excludingEventId.HasValue || e.Id != excludingEventId.Value))
            .OrderBy(e => e.Id)
            .Select(e => new EventCollisionInfo
            {
                EventId = e.Id,
                GroupId = e.GroupId,
                StartsAt = e.StartsAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}