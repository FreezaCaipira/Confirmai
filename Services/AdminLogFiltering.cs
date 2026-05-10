using Confirmai.Models;

namespace Confirmai.Services;

public sealed class AdminLogFilterCriteria
{
    public string? GlobalTerm { get; init; }
    public string? UserId { get; init; }
    public string? Source { get; init; }
    public string? Message { get; init; }
    public string? Level { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? EventType { get; init; }
    public string? EntityType { get; init; }
}

public static class AdminLogFiltering
{
    public static IQueryable<AppLog> Apply(IQueryable<AppLog> source, AdminLogFilterCriteria criteria)
    {
        var query = source;

        if (!string.IsNullOrWhiteSpace(criteria.GlobalTerm))
        {
            query = query.Where(log =>
                (log.UserId != null && log.UserId.Contains(criteria.GlobalTerm))
                || log.Source.Contains(criteria.GlobalTerm)
                || log.Message.Contains(criteria.GlobalTerm));
        }

        if (!string.IsNullOrWhiteSpace(criteria.UserId))
        {
            query = query.Where(log => log.UserId != null && log.UserId.Contains(criteria.UserId));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Source))
        {
            query = query.Where(log => log.Source.Contains(criteria.Source));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Message))
        {
            query = query.Where(log => log.Message.Contains(criteria.Message));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Level))
        {
            query = query.Where(log => log.Level == criteria.Level);
        }

        if (criteria.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(criteria.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(log => log.Timestamp >= startUtc);
        }

        if (criteria.EndDate.HasValue)
        {
            var endExclusiveUtc = DateTime.SpecifyKind(criteria.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(log => log.Timestamp < endExclusiveUtc);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EventType))
        {
            query = query.Where(log => log.EventType != null && log.EventType.Contains(criteria.EventType));
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            query = query.Where(log => log.EntityType != null && log.EntityType.Contains(criteria.EntityType));
        }

        return query;
    }
}


