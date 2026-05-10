using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

public sealed record AdminLogsAuditCounts(
    int All,
    int ReleaseOnly,
    int ReleaseSuccess,
    int ReleaseRefused,
    int SecurityPolicy,
    int WebhookWarnings,
    int ServerIntegration);

public sealed record AdminLogsPageData(
    int TotalLogs,
    int EffectivePage,
    List<AppLog> Logs,
    AdminLogsAuditCounts AuditCounts);

public class AdminLogsQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AdminLogsQueryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AdminLogsPageData> GetPageDataAsync(
        AdminLogFilterCriteria primaryCriteria,
        AdminLogFilterCriteria auditCountsCriteria,
        AdminLogSortColumn sortColumn,
        bool sortAscending,
        int requestedPage,
        int pageSize)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var effectivePageSize = Math.Max(pageSize, 1);
        var filteredQuery = AdminLogFiltering.Apply(db.Logs.AsNoTracking(), primaryCriteria);
        var totalLogs = await filteredQuery.CountAsync();

        var totalPages = Math.Max(1, (int)Math.Ceiling(totalLogs / (double)effectivePageSize));
        var effectivePage = Math.Min(Math.Max(requestedPage, 1), totalPages);

        var logs = await AdminLogSorting
            .Apply(filteredQuery, sortColumn, sortAscending)
            .Include(l => l.User)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync();

        var auditBase = AdminLogFiltering.Apply(db.Logs.AsNoTracking(), auditCountsCriteria);

        var countsProjection = await auditBase
            .GroupBy(_ => 1)
            .Select(g => new
            {
                All = g.Count(),
                ReleaseOnly = g.Count(log => log.Source == AdminAuditSources.OrdersReview),
                ReleaseSuccess = g.Count(log => log.Source == AdminAuditSources.OrdersReview && log.Level == AdminAuditLevels.Success),
                ReleaseRefused = g.Count(log => log.Source == AdminAuditSources.OrdersReview && log.Level == AdminAuditLevels.Refused),
                SecurityPolicy = g.Count(log => log.Source == AdminAuditSources.SecurityPolicy),
                WebhookWarnings = g.Count(log => log.Source == AdminAuditSources.Webhook && log.Level == "Warning"),
                ServerIntegration = g.Count(log => log.Source == AdminAuditSources.ServerIntegration)
            })
            .FirstOrDefaultAsync();

        var counts = countsProjection is null
            ? new AdminLogsAuditCounts(0, 0, 0, 0, 0, 0, 0)
            : new AdminLogsAuditCounts(
                All: countsProjection.All,
                ReleaseOnly: countsProjection.ReleaseOnly,
                ReleaseSuccess: countsProjection.ReleaseSuccess,
                ReleaseRefused: countsProjection.ReleaseRefused,
                SecurityPolicy: countsProjection.SecurityPolicy,
                WebhookWarnings: countsProjection.WebhookWarnings,
                ServerIntegration: countsProjection.ServerIntegration);

        return new AdminLogsPageData(totalLogs, effectivePage, logs, counts);
    }

    /// <summary>
    /// Returns all audit events for a specific entity, ordered oldest→newest.
    /// Used by the admin timeline page /admin/audit/{entityType}/{entityId}.
    /// </summary>
    public async Task<List<AppLog>> GetEntityTimelineAsync(string entityType, string entityId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Logs
            .AsNoTracking()
            .Include(l => l.User)
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
    }
}


