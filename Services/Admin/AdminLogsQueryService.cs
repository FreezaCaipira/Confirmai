using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

public sealed record AdminLogsAuditCounts(
    int All,
    int SecurityPolicy,
    int PaymentPanelStale,
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
                SecurityPolicy = g.Count(log => log.Source == AdminAuditSources.SecurityPolicy),
                PaymentPanelStale = g.Count(log => log.EventType == AuditEvents.PaymentReconciliationPanelStale),
                WebhookWarnings = g.Count(log => log.Source == AdminAuditSources.Webhook && log.Level == "Warning"),
                ServerIntegration = g.Count(log => log.Source == AdminAuditSources.ServerIntegration)
            })
            .FirstOrDefaultAsync();

        var counts = countsProjection is null
            ? new AdminLogsAuditCounts(0, 0, 0, 0, 0)
            : new AdminLogsAuditCounts(
                All: countsProjection.All,
                SecurityPolicy: countsProjection.SecurityPolicy,
                PaymentPanelStale: countsProjection.PaymentPanelStale,
                WebhookWarnings: countsProjection.WebhookWarnings,
                ServerIntegration: countsProjection.ServerIntegration);

        return new AdminLogsPageData(totalLogs, effectivePage, logs, counts);
    }

    /// <summary>
    /// Returns all audit events for a specific entity, ordered oldest?newest.
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

    public async Task<(List<AdminLogExportRow> Rows, bool Truncated)> GetExportRowsAsync(
        AdminLogFilterCriteria criteria,
        int maxRows)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rows = await AdminLogFiltering.Apply(db.Logs.AsNoTracking(), criteria)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new AdminLogExportRow(
                l.Timestamp,
                l.Level,
                l.Source,
                l.UserId,
                l.User != null ? l.User.UserName : null,
                l.Message,
                l.Exception,
                l.EventType,
                l.EntityType,
                l.EntityId,
                l.MetadataJson))
            .Take(maxRows + 1)
            .ToListAsync();

        var truncated = rows.Count > maxRows;
        if (truncated)
        {
            rows = rows.Take(maxRows).ToList();
        }

        return (rows, truncated);
    }
}


