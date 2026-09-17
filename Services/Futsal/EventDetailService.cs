using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Core;
using Confirmai.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Futsal;

public sealed record EventDetailLoadResult(
    Event? Event,
    ApplicationUser? CreatorUser,
    WaitingList? UserWaitlistEntry,
    GroupJoinRequest? UserJoinRequest,
    List<PostMatchVote> MvpVotes,
    int EventNumber);

public sealed record ConfirmPresenceResult(
    bool Success,
    string? Error,
    bool AddedToWaitlist);

public class EventDetailService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LogService _logService;
    private readonly Confirmai.Services.Events.EventNotificationService _notificationService;

    public EventDetailService(
        IDbContextFactory<AppDbContext> dbFactory,
        LogService logService,
        Confirmai.Services.Events.EventNotificationService notificationService)
    {
        _dbFactory = dbFactory;
        _logService = logService;
        _notificationService = notificationService;
    }

    public async Task<EventDetailLoadResult> LoadAsync(int eventId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
            .Include(e => e.Venue)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .Include(e => e.WaitingList)
                .ThenInclude(w => w.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Sport == Sport.Futsal);

        ApplicationUser? creatorUser = null;
        if (ev?.CreatedByUserId is not null)
            creatorUser = await db.Users.FindAsync(ev.CreatedByUserId) as ApplicationUser;

        var userWaitlistEntry = currentUserId is not null
            ? ev?.WaitingList.FirstOrDefault(w => w.UserId == currentUserId)
            : null;

        GroupJoinRequest? userJoinRequest = null;
        if (currentUserId is not null && ev is not null)
        {
            userJoinRequest = await db.GroupJoinRequests
                .Where(r => r.GroupId == ev.GroupId && r.UserId == currentUserId)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();
        }

        var mvpVotes = new List<PostMatchVote>();
        if (ev is not null && ev.Group.EnableBestPlayerVoting)
        {
            var evEndsAt = ev.StartsAt.AddMinutes(ev.DurationMinutes ?? 120);
            if (evEndsAt < DateTime.UtcNow)
                mvpVotes = await db.PostMatchVotes.Where(v => v.EventId == ev.Id).ToListAsync();
        }

        var eventNumber = 0;
        if (ev is not null)
            eventNumber = 1 + await db.Events
                .CountAsync(e => e.GroupId == ev.GroupId && e.StartsAt < ev.StartsAt);

        return new EventDetailLoadResult(ev, creatorUser, userWaitlistEntry, userJoinRequest, mvpVotes, eventNumber);
    }

    public async Task RequestToJoinAsync(int groupId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var alreadyPending = await db.GroupJoinRequests
            .AnyAsync(r => r.GroupId == groupId && r.UserId == userId && r.Status == JoinRequestStatus.Pending);

        if (!alreadyPending)
        {
            var req = new GroupJoinRequest
            {
                GroupId = groupId,
                UserId = userId,
                RequestedAt = DateTime.UtcNow,
                Status = JoinRequestStatus.Pending,
            };
            db.GroupJoinRequests.Add(req);
            await db.SaveChangesAsync();

            await _logService.AuditAsync(
                AuditEvents.GroupJoinRequested,
                AuditEntities.GroupJoinRequest,
                req.Id.ToString(),
                "Solicitação de entrada no grupo",
                actorUserId: userId,
                metadata: new { requestId = req.Id, groupId });
        }
    }

    public async Task CancelJoinRequestAsync(int joinRequestId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(joinRequestId);
        if (req is not null && req.Status == JoinRequestStatus.Pending)
        {
            db.GroupJoinRequests.Remove(req);
            await db.SaveChangesAsync();
        }
    }

    public async Task<ConfirmPresenceResult> ConfirmPresenceAsync(int eventId, string userId, FutsalPosition chosenPos)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == userId);
        if (existing is not null)
            return new ConfirmPresenceResult(false, "Você já está confirmado.", false);

        var existingWait = await db.WaitingLists
            .FirstOrDefaultAsync(w => w.EventId == eventId && w.UserId == userId);
        if (existingWait is not null)
            return new ConfirmPresenceResult(false, "Você já está na lista de espera.", false);

        var eventEntity = await db.Events
            .Include(e => e.Confirmations)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (eventEntity is null)
            return new ConfirmPresenceResult(false, "Evento não encontrado.", false);

        bool isGk = chosenPos == FutsalPosition.Goalkeeper;
        int currentGk = eventEntity.Confirmations.Count(c => c.Position == FutsalPosition.Goalkeeper);
        int currentOut = eventEntity.Confirmations.Count(c => c.Position != FutsalPosition.Goalkeeper);
        int maxOutfield = eventEntity.MaxGoalkeepers.HasValue
            ? eventEntity.MaxPlayers - eventEntity.MaxGoalkeepers.Value
            : eventEntity.MaxPlayers;

        bool slotFull = isGk
            ? (eventEntity.MaxGoalkeepers > 0 && currentGk >= eventEntity.MaxGoalkeepers.Value)
            : (maxOutfield > 0 && currentOut >= maxOutfield);

        if (slotFull)
        {
            var wCount = await db.WaitingLists.CountAsync(w => w.EventId == eventId);
            db.WaitingLists.Add(new WaitingList
            {
                EventId = eventId,
                UserId = userId,
                Position = wCount + 1,
                DesiredPosition = eventEntity.MaxGoalkeepers > 0 ? chosenPos : null,
                JoinedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            return new ConfirmPresenceResult(true, null, true);
        }
        else
        {
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = eventId,
                UserId = userId,
                Position = eventEntity.MaxGoalkeepers > 0 ? chosenPos : FutsalPosition.Outfield,
                ConfirmedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            return new ConfirmPresenceResult(true, null, false);
        }
    }

    public async Task CancelConfirmationAsync(int eventId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conf = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == userId);
        if (conf is not null)
        {
            var position = conf.Position ?? FutsalPosition.Outfield;
            db.EventConfirmations.Remove(conf);
            await db.SaveChangesAsync();
            await PromoteFromWaitlistAsync(db, eventId, position);
        }
    }

    public async Task LeaveWaitlistAsync(int eventId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entry = await db.WaitingLists
            .FirstOrDefaultAsync(w => w.EventId == eventId && w.UserId == userId);
        if (entry is not null)
        {
            db.WaitingLists.Remove(entry);
            await db.SaveChangesAsync();
        }
    }

    public async Task AdminRemoveConfirmationAsync(int confirmationId, string? currentUserId, int? eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conf = await db.EventConfirmations.FindAsync(confirmationId);
        if (conf is null) return;
        var position = conf.Position ?? FutsalPosition.Outfield;
        var targetUserId = conf.UserId;
        db.EventConfirmations.Remove(conf);
        await db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(db, conf.EventId, position);
        await _logService.AuditAsync(AuditEvents.EventConfirmationRemoved, AuditEntities.EventConfirmation, confirmationId.ToString(),
            $"Admin removeu jogador {targetUserId} da confirmação #{confirmationId} (evento #{eventId})", currentUserId, "EventAdmin");
    }

    public async Task AdminRemoveFromWaitlistAsync(int waitlistId, string? currentUserId, int? eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entry = await db.WaitingLists.FindAsync(waitlistId);
        if (entry is not null)
        {
            var wlUserId = entry.UserId;
            db.WaitingLists.Remove(entry);
            await db.SaveChangesAsync();
            await _logService.AuditAsync(AuditEvents.EventWaitlistRemoved, AuditEntities.EventConfirmation, waitlistId.ToString(),
                $"Admin removeu jogador {wlUserId} da lista de espera (evento #{eventId})", currentUserId, "EventAdmin");
        }
    }

    public async Task AdminAddOutfieldSlotAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(eventId);
        if (evt is null) return;
        evt.MaxPlayers += 1;
        await db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(db, eventId, FutsalPosition.Outfield);
    }

    public async Task AdminRemoveOutfieldSlotAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(eventId);
        if (evt is null) return;
        evt.MaxPlayers -= 1;
        await db.SaveChangesAsync();
    }

    public async Task AdminAddGoalkeeperSlotAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(eventId);
        if (evt is null) return;
        evt.MaxGoalkeepers = (evt.MaxGoalkeepers ?? 0) + 1;
        await db.SaveChangesAsync();
        await PromoteFromWaitlistAsync(db, eventId, FutsalPosition.Goalkeeper);
    }

    public async Task AdminRemoveGoalkeeperSlotAsync(int eventId, int newMaxGk)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var evt = await db.Events.FindAsync(eventId);
        if (evt is null) return;
        evt.MaxGoalkeepers = newMaxGk;
        await db.SaveChangesAsync();
    }

    private async Task PromoteFromWaitlistAsync(AppDbContext db, int eventId, FutsalPosition position)
    {
        var next = await db.WaitingLists
            .Where(w => w.EventId == eventId &&
                        (w.DesiredPosition == null || w.DesiredPosition == position))
            .OrderBy(w => w.Position)
            .ThenBy(w => w.JoinedAt)
            .FirstOrDefaultAsync();
        if (next is null) return;

        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = eventId,
            UserId = next.UserId,
            Position = position,
            ConfirmedAt = DateTime.UtcNow,
        });
        db.WaitingLists.Remove(next);
        await db.SaveChangesAsync();

        _ = Task.Run(() => _notificationService.NotifyWaitlistPromotedAsync(eventId, next.UserId));
    }
}
