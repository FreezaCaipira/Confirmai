using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Confirmai.Services.Admin;

public sealed record AdminLogsQueryOverrides(
    string? EventType,
    string? Source,
    string? Level,
    string? EntityType,
    DateTime? StartDate,
    DateTime? EndDate)
{
    public bool HasAny =>
        !string.IsNullOrWhiteSpace(EventType)
        || !string.IsNullOrWhiteSpace(Source)
        || !string.IsNullOrWhiteSpace(Level)
        || !string.IsNullOrWhiteSpace(EntityType)
        || StartDate.HasValue
        || EndDate.HasValue;
}

public static class AdminLogsQueryOverridesParser
{
    public static AdminLogsQueryOverrides Parse(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return new AdminLogsQueryOverrides(null, null, null, null, null, null);
        }

        var query = QueryHelpers.ParseQuery(uri.Query);

        return new AdminLogsQueryOverrides(
            EventType: ReadString(query, "eventType"),
            Source: ReadString(query, "source"),
            Level: ReadString(query, "level"),
            EntityType: ReadString(query, "entityType"),
            StartDate: ReadDate(query, "startDate"),
            EndDate: ReadDate(query, "endDate"));
    }

    private static string? ReadString(IReadOnlyDictionary<string, StringValues> query, string key)
    {
        if (!query.TryGetValue(key, out var rawValue))
        {
            return null;
        }

        var parsed = rawValue.ToString().Trim();
        return string.IsNullOrWhiteSpace(parsed) ? null : parsed;
    }

    private static DateTime? ReadDate(IReadOnlyDictionary<string, StringValues> query, string key)
    {
        var rawDate = ReadString(query, key);
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            return null;
        }

        if (!DateTime.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return null;
        }

        return parsedDate.Date;
    }
}
