using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

/// <summary>
/// Service for generating revenue reports (fees collected and payouts sent).
/// </summary>
public sealed class AdminRevenueReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AdminRevenueReportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Gets revenue summary for a date range.
    /// </summary>
    public async Task<RevenueSummary> GetRevenueSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.Payments
            .AsNoTracking()
            .Where(p => p.FeeAmount.HasValue && p.FeeAmount > 0);

        if (startDate.HasValue)
            query = query.Where(p => p.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));

        var payments = await query.ToListAsync();

        var summary = new RevenueSummary
        {
            TotalTransactions = payments.Count,
            TotalFeesCollected = payments.Sum(p => p.FeeAmount ?? 0),
            TotalBaseAmount = payments.Sum(p => p.BaseAmount ?? 0),
            TotalPayoutsSent = payments.Count(p => p.PayoutStatus == PayoutStatus.Sent),
            TotalPayoutsConfirmed = payments.Count(p => p.PayoutStatus == PayoutStatus.Confirmed),
            TotalPayoutsFailed = payments.Count(p => p.PayoutStatus == PayoutStatus.Failed),
            TotalPayoutsPending = payments.Count(p => p.PayoutStatus == PayoutStatus.Pending)
        };

        return summary;
    }

    /// <summary>
    /// Gets revenue breakdown by group.
    /// </summary>
    public async Task<List<GroupRevenueBreakdown>> GetRevenueByGroupAsync(DateTime? startDate, DateTime? endDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.Payments
            .AsNoTracking()
            .Include(p => p.Product)
            .Where(p => p.FeeAmount.HasValue && p.FeeAmount > 0);

        if (startDate.HasValue)
            query = query.Where(p => p.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));

        var payments = await query.ToListAsync();

        var byGroup = payments
            .GroupBy(p => p.Product?.Name ?? "Unknown")
            .Select(g => new GroupRevenueBreakdown
            {
                GroupName = g.Key,
                TotalTransactions = g.Count(),
                TotalFeesCollected = g.Sum(p => p.FeeAmount ?? 0),
                TotalBaseAmount = g.Sum(p => p.BaseAmount ?? 0),
                TotalPayoutsSent = g.Count(p => p.PayoutStatus == PayoutStatus.Sent),
                TotalPayoutsConfirmed = g.Count(p => p.PayoutStatus == PayoutStatus.Confirmed),
                TotalPayoutsFailed = g.Count(p => p.PayoutStatus == PayoutStatus.Failed),
                TotalPayoutsPending = g.Count(p => p.PayoutStatus == PayoutStatus.Pending)
            })
            .OrderByDescending(g => g.TotalFeesCollected)
            .ToList();

        return byGroup;
    }
}

public record RevenueSummary
{
    public int TotalTransactions { get; init; }
    public decimal TotalFeesCollected { get; init; }
    public decimal TotalBaseAmount { get; init; }
    public int TotalPayoutsSent { get; init; }
    public int TotalPayoutsConfirmed { get; init; }
    public int TotalPayoutsFailed { get; init; }
    public int TotalPayoutsPending { get; init; }
}

public record GroupRevenueBreakdown
{
    public string GroupName { get; init; } = string.Empty;
    public int TotalTransactions { get; init; }
    public decimal TotalFeesCollected { get; init; }
    public decimal TotalBaseAmount { get; init; }
    public int TotalPayoutsSent { get; init; }
    public int TotalPayoutsConfirmed { get; init; }
    public int TotalPayoutsFailed { get; init; }
    public int TotalPayoutsPending { get; init; }
}
