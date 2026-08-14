using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

public record GroupPaymentsData(
    List<UserDelinquency> DelinquencyList,
    List<PaymentHistoryEntry> PaymentHistory,
    List<PendingProofEntry> PendingProofList);

public sealed class GroupPaymentsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly EventNotificationService _notificationService;
    private readonly LogService _logService;

    public GroupPaymentsService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        EventNotificationService notificationService,
        LogService logService)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _notificationService = notificationService;
        _logService = logService;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        return auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<(Group? Group, bool IsAdmin)> LoadGroupAndCheckAdminAsync(int groupId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group is null) return (null, false);

        var isAdmin = currentUserId is not null &&
                      group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);

        return (group, isAdmin);
    }

    public async Task<GroupPaymentsData> LoadPaymentsDataAsync(int groupId, Group group)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var nowUtc = DateTime.UtcNow;
        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
        var sport = group.Sport;

        var unpaidConfirmations = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.Event.StartsAt < nowUtc &&
                c.Event.Price != null &&
                c.Event.Price > 0 &&
                !c.HasPaid &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderBy(c => c.Event.StartsAt)
            .Select(c => new {
                c.Id, c.UserId, c.EventId, c.Event, c.User, c.PaymentGatewayName, c.Position,
                HasProof = c.PixProofUploadedAt != null,
            })
            .ToListAsync();

        var delinquencyList = unpaidConfirmations
            .GroupBy(c => c.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                var userName = user?.FullName ?? user?.UserName ?? "Jogador";
                var entries = g.Select(c =>
                {
                    var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
                    return new DelinquencyEntry(c.Id, c.EventId, c.Event.StartsAt, c.Event.Price!.Value, href, c.HasProof);
                }).ToList();
                return new UserDelinquency(g.Key, userName, entries);
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();

        var manualPaid = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.HasPaid &&
                c.MarkedPaidByUserId != null &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderByDescending(c => c.MarkedPaidAt)
            .Take(50)
            .ToListAsync();

        var adminIds = manualPaid.Select(c => c.MarkedPaidByUserId!).Distinct().ToList();
        var adminUsers = await db.Users
            .Where(u => adminIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? u.Email })
            .ToListAsync();
        var adminMap = adminUsers.ToDictionary(u => u.Id, u => u.Name ?? "Admin");

        var paymentHistory = manualPaid.Select(c =>
        {
            var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
            var userName = c.User?.FullName ?? c.User?.UserName ?? "Jogador";
            var adminName = adminMap.TryGetValue(c.MarkedPaidByUserId!, out var n) ? n : "Admin";
            return new PaymentHistoryEntry(userName, c.Event.StartsAt, c.Event.Price ?? 0, href, adminName, c.MarkedPaidAt!.Value, c.Id, c.PixProofImageData != null && c.PixProofImageData.Length > 0);
        }).ToList();

        var pendingProofs = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.PixProofUploadedAt != null &&
                !c.HasPaid &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderByDescending(c => c.PixProofUploadedAt)
            .ToListAsync();

        var pendingProofList = pendingProofs.Select(c =>
        {
            var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
            var userName = c.User?.FullName ?? c.User?.UserName ?? "Jogador";
            var eventName = c.Event?.Location ?? "Partida";
            return new PendingProofEntry(c.Id, c.UserId, userName, c.EventId, eventName, group.Name, c.Event!.StartsAt, c.Event.Price ?? 0, href, c.PixProofUploadedAt!.Value);
        }).ToList();

        return new GroupPaymentsData(delinquencyList, paymentHistory, pendingProofList);
    }

    public async Task MarkPaidAsync(int confirmationId, string userId, string? currentUserId, int groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conf = await db.EventConfirmations.FindAsync(confirmationId);
        if (conf is not null && !conf.HasPaid && conf.PaymentGatewayName == null)
        {
            conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
            conf.HasPaid = true;
            conf.MarkedPaidByUserId = currentUserId;
            conf.MarkedPaidAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await _logService.AuditAsync(
                AuditEvents.EventConfirmationPaidManual,
                AuditEntities.EventConfirmation,
                confirmationId.ToString(),
                $"Admin marcou confirmacao #{confirmationId} do jogador {userId} como paga manualmente (grupo #{groupId})",
                currentUserId, "GroupAdmin");
        }
    }

    public async Task NotifyDelinquencyAsync(UserDelinquency d, string? currentUserId, string groupName, int groupId)
    {
        if (currentUserId is null) return;

        var entries = d.Entries.Select(e => (e.EventDate, e.EventPrice)).ToList();

        await _notificationService.NotifyDelinquencyAsync(
            adminUserId: currentUserId,
            targetUserId: d.UserId,
            groupName: groupName,
            entries: entries);

        await _logService.AuditAsync(
            AuditEvents.DelinquencyNotified,
            AuditEntities.Group,
            groupId.ToString(),
            $"Admin notificou jogador {d.UserId} ({d.UserName}) sobre {d.Entries.Count} partida(s) em aberto - R$ {d.TotalAmount:F2} (grupo #{groupId})",
            currentUserId, "GroupAdmin");
    }

    /// <summary>
    /// Rejects a player's uploaded Pix proof: clears the proof image and timestamp,
    /// keeping the confirmation (player stays confirmed but unpaid).
    /// </summary>
    public async Task RejectProofAsync(int confirmationId, string? currentUserId, int groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var conf = await db.EventConfirmations.FindAsync(confirmationId);
        if (conf is null || conf.PixProofUploadedAt is null) return;

        conf.PixProofImageData = null;
        conf.PixProofContentType = null;
        conf.PixProofUploadedAt = null;
        await db.SaveChangesAsync();

        await _logService.AuditAsync(
            AuditEvents.EventConfirmationProofRejected,
            AuditEntities.EventConfirmation,
            confirmationId.ToString(),
            $"Admin rejeitou comprovante da confirmacao #{confirmationId} (grupo #{groupId})",
            currentUserId, "GroupAdmin");
    }
}
