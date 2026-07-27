using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

public sealed class GroupDetailData
{
    public Group? Group { get; set; }
    public List<GroupJoinRequest> PendingRequests { get; set; } = new();
    public GroupJoinRequest? UserJoinRequest { get; set; }
    public string? CurrentUserId { get; set; }
}

public sealed class GroupDetailService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;

    public GroupDetailService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
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
            db.GroupJoinRequests.Add(new GroupJoinRequest
            {
                GroupId = groupId,
                UserId = userId,
                RequestedAt = DateTime.UtcNow,
                Status = JoinRequestStatus.Pending,
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task CancelJoinRequestAsync(int requestId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is not null && req.Status == JoinRequestStatus.Pending)
        {
            db.GroupJoinRequests.Remove(req);
            await db.SaveChangesAsync();
        }
    }

    public async Task<(bool Success, string? Error)> JoinWithCodeAsync(int groupId, string userId, string code)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null || string.IsNullOrWhiteSpace(group.InviteCode) ||
            !string.Equals(code, group.InviteCode, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Codigo invalido. Verifique e tente novamente.");
        }

        var exists = await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (!exists)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupMemberRole.Member,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task ApproveRequestAsync(int requestId, string approverUserId, string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is null || req.Status != JoinRequestStatus.Pending) return;

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
    }

    public async Task RejectRequestAsync(int requestId, string rejecterUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var req = await db.GroupJoinRequests.FindAsync(requestId);
        if (req is null || req.Status != JoinRequestStatus.Pending) return;
        req.Status = JoinRequestStatus.Rejected;
        req.RespondedAt = DateTime.UtcNow;
        req.RespondedByUserId = rejecterUserId;
        await db.SaveChangesAsync();
    }

    public async Task ApproveSelectedAsync(HashSet<int> requestIds, string approverUserId, string groupName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var requests = await db.GroupJoinRequests
            .Where(r => requestIds.Contains(r.Id) && r.Status == JoinRequestStatus.Pending)
            .Include(r => r.User)
            .ToListAsync();

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
    }

    public async Task RejectSelectedAsync(HashSet<int> requestIds, string rejecterUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var requests = await db.GroupJoinRequests
            .Where(r => requestIds.Contains(r.Id) && r.Status == JoinRequestStatus.Pending)
            .ToListAsync();

        foreach (var req in requests)
        {
            req.Status = JoinRequestStatus.Rejected;
            req.RespondedAt = DateTime.UtcNow;
            req.RespondedByUserId = rejecterUserId;
        }

        await db.SaveChangesAsync();
    }
}
