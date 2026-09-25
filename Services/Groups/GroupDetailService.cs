using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

public enum JoinWithCodeResult
{
    Joined = 0,
    AlreadyMember = 1,
    InvalidCode = 2
}

public enum LeaveGroupResult
{
    Left = 0,
    NotMember = 1,
    SoleAdmin = 2,
    PendingPayment = 3
}

public sealed class GroupDetailData
{
    public Group? Group { get; set; }
    public List<GroupJoinRequest> PendingRequests { get; set; } = new();
    public GroupJoinRequest? UserJoinRequest { get; set; }
    public string? CurrentUserId { get; set; }
    public int UpcomingEventsCount { get; set; }
    public Event? NextEvent { get; set; }
}

public sealed class GroupDetailService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly LogService _log;
    private readonly Futsal.EventDetailService _eventDetailService;

    public GroupDetailService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        LogService log,
        Futsal.EventDetailService eventDetailService)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _log = log;
        _eventDetailService = eventDetailService;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        return auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<GroupDetailData> LoadAsync(int groupId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var group = await db.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        var data = new GroupDetailData { Group = group, CurrentUserId = currentUserId };

        if (group is not null)
        {
            var now = DateTime.UtcNow;
            data.UpcomingEventsCount = await db.Events
                .CountAsync(e => e.GroupId == groupId && e.IsActive && e.StartsAt > now);

            data.NextEvent = await db.Events
                .Where(e => e.GroupId == groupId && e.IsActive && e.StartsAt > now)
                .Include(e => e.Confirmations)
                .OrderBy(e => e.StartsAt)
                .FirstOrDefaultAsync();

            data.PendingRequests = await db.GroupJoinRequests
                .Where(r => r.GroupId == groupId && r.Status == JoinRequestStatus.Pending)
                .Include(r => r.User)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            if (currentUserId is not null)
            {
                data.UserJoinRequest = await db.GroupJoinRequests
                    .Where(r => r.GroupId == groupId && r.UserId == currentUserId)
                    .OrderByDescending(r => r.RequestedAt)
                    .FirstOrDefaultAsync();
            }
        }

        return data;
    }

    public async Task RequestToJoinAsync(int groupId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var already = await db.GroupJoinRequests
            .AnyAsync(r => r.GroupId == groupId && r.UserId == userId
                        && r.Status == JoinRequestStatus.Pending);
        if (!already)
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

            await _log.AuditAsync(
                AuditEvents.GroupJoinRequested,
                AuditEntities.GroupJoinRequest,
                req.Id.ToString(),
                "Solicitação de entrada no grupo",
                actorUserId: userId,
                metadata: new { requestId = req.Id, groupId });
        }
    }

    /// <summary>
    /// Pending join requests the user sent (not yet answered by an admin).
    /// Used by /grupos so the requester can see and cancel them.
    /// </summary>
    public async Task<List<GroupJoinRequest>> GetMyPendingRequestsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.GroupJoinRequests
            .Where(r => r.UserId == userId && r.Status == JoinRequestStatus.Pending)
            .Include(r => r.Group)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task CancelJoinRequestAsync(int requestId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is not null && req.Status == JoinRequestStatus.Pending && req.UserId == userId)
        {
            db.GroupJoinRequests.Remove(req);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Removes the caller's own membership. The sole admin of a group cannot
    /// leave — they must promote another admin first, otherwise the group
    /// becomes unmanageable.
    /// </summary>
    public async Task<LeaveGroupResult> LeaveGroupAsync(int groupId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var member = await db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member is null)
        {
            return LeaveGroupResult.NotMember;
        }

        if (member.Role == GroupMemberRole.Admin)
        {
            var otherAdminExists = await db.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId != userId
                            && m.Role == GroupMemberRole.Admin);
            if (!otherAdminExists)
            {
                return LeaveGroupResult.SoleAdmin;
            }
        }

        // Review C36-D: an unpaid pending confirmation on a priced match is
        // the player's debt — and the delinquency views filter by CURRENT
        // memberIds, so removing the membership would erase the debt from
        // the organizer's sight. The player resolves it first: pays (or has
        // the proof reviewed) via "Meus pagamentos", or cancels presence on
        // the match. Cancelled events carry no debt and never block.
        var hasPendingPayment = await db.EventConfirmations
            .AnyAsync(c =>
                c.UserId == userId &&
                c.Event.GroupId == groupId &&
                c.Event.IsActive &&
                c.Event.Price != null &&
                c.Event.Price > 0 &&
                c.Position != FutsalPosition.Goalkeeper &&
                !c.HasPaid &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending);
        if (hasPendingPayment)
        {
            return LeaveGroupResult.PendingPayment;
        }

        // Leaving frees the spots the player would keep holding: unpaid
        // confirmations on future matches are cancelled through the same
        // path as a self-cancel (waitlist promotion included) and every
        // waitlist entry in the group is removed. Paid or gateway-charged
        // confirmations are kept — they are financial records and a
        // purchased spot; the organizer can still remove them explicitly.
        var nowUtc = DateTime.UtcNow;
        var cancellableEventIds = await db.EventConfirmations
            .Where(c => c.UserId == userId &&
                c.Event.GroupId == groupId &&
                c.Event.StartsAt >= nowUtc &&
                !c.HasPaid &&
                c.PaymentStatus != EventConfirmationPaymentStatus.Paid &&
                c.PaymentGatewayName == null)
            .Select(c => c.EventId)
            .ToListAsync();

        foreach (var eventId in cancellableEventIds)
        {
            await _eventDetailService.CancelConfirmationAsync(eventId, userId);
        }

        var waitlistEntries = await db.WaitingLists
            .Where(w => w.UserId == userId && w.Event.GroupId == groupId)
            .ToListAsync();
        db.WaitingLists.RemoveRange(waitlistEntries);

        db.GroupMembers.Remove(member);
        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.GroupMemberRemoved,
            AuditEntities.GroupMember,
            member.Id.ToString(),
            "Membro saiu do grupo",
            actorUserId: userId,
            metadata: new
            {
                groupId,
                userId,
                leftVoluntarily = true,
                cancelledConfirmations = cancellableEventIds.Count,
                removedWaitlistEntries = waitlistEntries.Count,
            });

        return LeaveGroupResult.Left;
    }

    /// <summary>
    /// Joins a group by invite code. Returns an outcome code — the caller
    /// (page) translates it to the localized message; the service never
    /// returns user-facing strings.
    /// </summary>
    public async Task<JoinWithCodeResult> JoinWithCodeAsync(int groupId, string userId, string code)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null || string.IsNullOrWhiteSpace(group.InviteCode) ||
            !string.Equals(code, group.InviteCode, StringComparison.OrdinalIgnoreCase))
        {
            return JoinWithCodeResult.InvalidCode;
        }

        var exists = await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (exists)
        {
            return JoinWithCodeResult.AlreadyMember;
        }

        db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            Role = GroupMemberRole.Member,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.GroupMemberAdded,
            AuditEntities.Group,
            groupId.ToString(),
            "Usuário entrou no grupo via código de convite",
            actorUserId: userId,
            metadata: new { groupId, memberUserId = userId, via = "invite-code" });

        return JoinWithCodeResult.Joined;
    }

    public async Task ApproveRequestAsync(int requestId, string approverUserId, string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is null || req.Status != JoinRequestStatus.Pending) return;
        if (!await GroupAccess.IsGroupAdminAsync(db, req.GroupId, approverUserId)) return;

        var alreadyMember = await db.GroupMembers
            .AnyAsync(m => m.GroupId == req.GroupId && m.UserId == req.UserId);
        if (!alreadyMember)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId = req.GroupId,
                UserId = req.UserId,
                Role = GroupMemberRole.Member,
                CreatedAt = DateTime.UtcNow,
            });
        }

        req.Status = JoinRequestStatus.Approved;
        req.RespondedAt = DateTime.UtcNow;
        req.RespondedByUserId = approverUserId;

        var userName = (await db.Users.FindAsync(req.UserId) as ApplicationUser)?.UserName;
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = null,
            RecipientUserId = req.UserId,
            RecipientDisplayName = userName,
            Subject = $"Voce foi aprovado em \"{groupName}\"",
            Body = $"Sua solicitacao para entrar no grupo **{groupName}** foi aprovada! Voce ja pode acessar o grupo.",
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.GroupJoinApproved,
            AuditEntities.GroupJoinRequest,
            req.Id.ToString(),
            $"Solicitação aprovada no grupo \"{groupName}\"",
            actorUserId: approverUserId,
            metadata: new { requestId = req.Id, req.GroupId, req.UserId });
    }

    public async Task RejectRequestAsync(int requestId, string rejecterUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is null || req.Status != JoinRequestStatus.Pending) return;
        if (!await GroupAccess.IsGroupAdminAsync(db, req.GroupId, rejecterUserId)) return;
        req.Status = JoinRequestStatus.Rejected;
        req.RespondedAt = DateTime.UtcNow;
        req.RespondedByUserId = rejecterUserId;
        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.GroupJoinRejected,
            AuditEntities.GroupJoinRequest,
            req.Id.ToString(),
            "Solicitação de entrada rejeitada",
            actorUserId: rejecterUserId,
            metadata: new { requestId = req.Id, req.GroupId, req.UserId });
    }

    public async Task ApproveSelectedAsync(HashSet<int> requestIds, string approverUserId, string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var requests = await db.GroupJoinRequests
            .Where(r => requestIds.Contains(r.Id) && r.Status == JoinRequestStatus.Pending)
            .Include(r => r.User)
            .ToListAsync();

        // Only requests of groups where the caller is an admin are processed.
        var adminGroupIds = await GroupAccess.AdminGroupIdsAsync(
            db, requests.Select(r => r.GroupId), approverUserId);
        requests = requests.Where(r => adminGroupIds.Contains(r.GroupId)).ToList();

        foreach (var req in requests)
        {
            var alreadyMember = await db.GroupMembers
                .AnyAsync(m => m.GroupId == req.GroupId && m.UserId == req.UserId);
            if (!alreadyMember)
            {
                db.GroupMembers.Add(new GroupMember
                {
                    GroupId = req.GroupId,
                    UserId = req.UserId,
                    Role = GroupMemberRole.Member,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            req.Status = JoinRequestStatus.Approved;
            req.RespondedAt = DateTime.UtcNow;
            req.RespondedByUserId = approverUserId;

            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId = null,
                RecipientUserId = req.UserId,
                RecipientDisplayName = req.User?.UserName,
                Subject = $"Voce foi aprovado em \"{groupName}\"",
                Body = $"Sua solicitacao para entrar no grupo **{groupName}** foi aprovada! Voce ja pode acessar o grupo.",
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        foreach (var req in requests)
        {
            await _log.AuditAsync(
                AuditEvents.GroupJoinApproved,
                AuditEntities.GroupJoinRequest,
                req.Id.ToString(),
                $"Solicitação aprovada no grupo \"{groupName}\"",
                actorUserId: approverUserId,
                metadata: new { requestId = req.Id, req.GroupId, req.UserId });
        }
    }

    public async Task RejectSelectedAsync(HashSet<int> requestIds, string rejecterUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var requests = await db.GroupJoinRequests
            .Where(r => requestIds.Contains(r.Id) && r.Status == JoinRequestStatus.Pending)
            .ToListAsync();

        var adminGroupIds = await GroupAccess.AdminGroupIdsAsync(
            db, requests.Select(r => r.GroupId), rejecterUserId);
        requests = requests.Where(r => adminGroupIds.Contains(r.GroupId)).ToList();

        foreach (var req in requests)
        {
            req.Status = JoinRequestStatus.Rejected;
            req.RespondedAt = DateTime.UtcNow;
            req.RespondedByUserId = rejecterUserId;
        }

        await db.SaveChangesAsync();

        foreach (var req in requests)
        {
            await _log.AuditAsync(
                AuditEvents.GroupJoinRejected,
                AuditEntities.GroupJoinRequest,
                req.Id.ToString(),
                "Solicitação de entrada rejeitada",
                actorUserId: rejecterUserId,
                metadata: new { requestId = req.Id, req.GroupId, req.UserId });
        }
    }

    /// <summary>
    /// Approves every pending join request of a group (the "approve all" action
    /// on /grupos). Same semantics as <see cref="ApproveSelectedAsync"/> scoped
    /// to the group. No-ops unless the approver is an admin member of the group.
    /// </summary>
    public async Task ApproveAllPendingAsync(int groupId, string approverUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var isAdmin = await db.GroupMembers
            .AnyAsync(m => m.GroupId == groupId
                && m.UserId == approverUserId
                && m.Role == GroupMemberRole.Admin);
        if (!isAdmin) return;

        var group = await db.Groups.FindAsync(groupId);
        if (group is null) return;

        var pendingRequests = await db.GroupJoinRequests
            .Where(r => r.GroupId == groupId && r.Status == JoinRequestStatus.Pending)
            .Include(r => r.User)
            .ToListAsync();

        foreach (var req in pendingRequests)
        {
            var alreadyMember = await db.GroupMembers
                .AnyAsync(m => m.GroupId == req.GroupId && m.UserId == req.UserId);
            if (!alreadyMember)
            {
                db.GroupMembers.Add(new GroupMember
                {
                    GroupId = req.GroupId,
                    UserId = req.UserId,
                    Role = GroupMemberRole.Member,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            req.Status = JoinRequestStatus.Approved;
            req.RespondedAt = DateTime.UtcNow;
            req.RespondedByUserId = approverUserId;

            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId = null,
                RecipientUserId = req.UserId,
                RecipientDisplayName = req.User?.UserName,
                Subject = $"Voce foi aprovado em \"{group.Name}\"",
                Body = $"Sua solicitacao para entrar no grupo **{group.Name}** foi aprovada! Voce ja pode acessar o grupo.",
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        foreach (var req in pendingRequests)
        {
            await _log.AuditAsync(
                AuditEvents.GroupJoinApproved,
                AuditEntities.GroupJoinRequest,
                req.Id.ToString(),
                $"Solicitação aprovada no grupo \"{group.Name}\"",
                actorUserId: approverUserId,
                metadata: new { requestId = req.Id, req.GroupId, req.UserId });
        }
    }
}
