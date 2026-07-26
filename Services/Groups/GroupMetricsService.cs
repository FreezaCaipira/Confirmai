using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

public class GroupMetricsSnapshot
{
    public int TotalMembers { get; set; }
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public int PendingJoinRequests { get; set; }
    public int TotalConfirmations { get; set; }
    public int PaidConfirmations { get; set; }
    public decimal PaymentCompletionRate { get; set; }
}

public class GroupMetricsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GroupMetricsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<GroupMetricsSnapshot> GetSnapshotAsync(int groupId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var group = await db.Groups
            .AsNoTracking()
            .Include(g => g.Members)
            .Include(g => g.Events)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            return new GroupMetricsSnapshot();
        }

        var totalMembers = group.Members.Count;
        var totalEvents = group.Events.Count;
        var upcomingEvents = group.Events.Count(e => e.StartsAt > DateTime.UtcNow);
        var pendingJoinRequests = await db.GroupJoinRequests
            .CountAsync(r => r.GroupId == groupId && r.Status == JoinRequestStatus.Pending);

        // Calculate attendance rate
        var totalConfirmations = 0;
        var totalSlots = 0;
        foreach (var evt in group.Events.Where(e => e.StartsAt < DateTime.UtcNow))
        {
            var confirmations = await db.EventConfirmations
                .CountAsync(c => c.EventId == evt.Id);
            totalConfirmations += confirmations;
            totalSlots += evt.MaxPlayers;
        }
        var averageAttendanceRate = totalSlots > 0 
            ? (decimal)totalConfirmations / totalSlots * 100 
            : 0;

        // Calculate payment metrics
        var eventIds = group.Events.Select(e => e.Id).ToList();
        var totalConfirmationsWithPayment = await db.EventConfirmations
            .CountAsync(c => eventIds.Contains(c.EventId));
        var paidConfirmations = await db.EventConfirmations
            .CountAsync(c => eventIds.Contains(c.EventId) && c.PaymentStatus == EventConfirmationPaymentStatus.Paid);
        var paymentCompletionRate = totalConfirmationsWithPayment > 0 
            ? (decimal)paidConfirmations / totalConfirmationsWithPayment * 100 
            : 0;

        return new GroupMetricsSnapshot
        {
            TotalMembers = totalMembers,
            TotalEvents = totalEvents,
            UpcomingEvents = upcomingEvents,
            AverageAttendanceRate = Math.Round(averageAttendanceRate, 1),
            PendingJoinRequests = pendingJoinRequests,
            TotalConfirmations = totalConfirmationsWithPayment,
            PaidConfirmations = paidConfirmations,
            PaymentCompletionRate = Math.Round(paymentCompletionRate, 1)
        };
    }

    public async Task<Dictionary<int, GroupMetricsSnapshot>> GetMultipleSnapshotsAsync(IEnumerable<int> groupIds)
    {
        var snapshots = new Dictionary<int, GroupMetricsSnapshot>();
        foreach (var groupId in groupIds)
        {
            snapshots[groupId] = await GetSnapshotAsync(groupId);
        }
        return snapshots;
    }
}
