using Confirmai.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Confirmai.Services;

public sealed record AdminLogExportRow(
    DateTime Timestamp,
    string Level,
    string Source,
    string? UserId,
    string? UserName,
    string? Message,
    string? Exception,
    string? EventType = null,
    string? EntityType = null,
    string? EntityId = null,
    string? MetadataJson = null);

public class AdminLogsExportService
{
    public string BuildCsv(IEnumerable<AdminLogExportRow> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine("TimestampUtc,Level,Source,UserId,UserName,EventType,EntityType,EntityId,Message,Exception,MetadataJson");

        foreach (var row in rows)
        {
            csv
                .Append(EscapeCsv(row.Timestamp.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsv(row.Level)).Append(',')
                .Append(EscapeCsv(row.Source)).Append(',')
                .Append(EscapeCsv(row.UserId)).Append(',')
                .Append(EscapeCsv(row.UserName)).Append(',')
                .Append(EscapeCsv(row.EventType)).Append(',')
                .Append(EscapeCsv(row.EntityType)).Append(',')
                .Append(EscapeCsv(row.EntityId)).Append(',')
                .Append(EscapeCsv(row.Message)).Append(',')
                .Append(EscapeCsv(row.Exception)).Append(',')
                .Append(EscapeCsv(row.MetadataJson))
                .AppendLine();
        }

        return csv.ToString();
    }

    public string BuildCsv(IEnumerable<AppLog> rows)
    {
        return BuildCsv(rows.Select(row => new AdminLogExportRow(
            Timestamp: row.Timestamp,
            Level: row.Level,
            Source: row.Source,
            UserId: row.UserId,
            UserName: row.User?.UserName,
            Message: row.Message,
            Exception: row.Exception,
            EventType: row.EventType,
            EntityType: row.EntityType,
            EntityId: row.EntityId,
            MetadataJson: row.MetadataJson)));
    }

    public string BuildJson(IEnumerable<AdminLogExportRow> rows)
    {
        var payload = rows.Select(row => new
        {
            timestampUtc = row.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            row.Level,
            row.Source,
            row.UserId,
            userName = row.UserName,
            eventType = row.EventType,
            entityType = row.EntityType,
            entityId = row.EntityId,
            row.Message,
            row.Exception,
            metadataJson = row.MetadataJson
        });

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    public string BuildJson(IEnumerable<AppLog> rows)
    {
        return BuildJson(rows.Select(row => new AdminLogExportRow(
            Timestamp: row.Timestamp,
            Level: row.Level,
            Source: row.Source,
            UserId: row.UserId,
            UserName: row.User?.UserName,
            Message: row.Message,
            Exception: row.Exception,
            EventType: row.EventType,
            EntityType: row.EntityType,
            EntityId: row.EntityId,
            MetadataJson: row.MetadataJson)));
    }

    public string BuildExportFileName(
        string extension,
        string? filterLevel,
        string? filterSource,
        DateTime? filterStartDate,
        DateTime? filterEndDate,
        bool truncated = false,
        DateTime? utcNow = null)
    {
        var suffixParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(filterLevel))
        {
            suffixParts.Add($"level-{NormalizeForFileName(filterLevel)}");
        }

        if (!string.IsNullOrWhiteSpace(filterSource))
        {
            suffixParts.Add($"source-{NormalizeForFileName(filterSource)}");
        }

        if (filterStartDate.HasValue)
        {
            suffixParts.Add($"from-{filterStartDate.Value:yyyyMMdd}");
        }

        if (filterEndDate.HasValue)
        {
            suffixParts.Add($"to-{filterEndDate.Value:yyyyMMdd}");
        }

        if (truncated)
        {
            suffixParts.Add("truncated");
        }

        var suffix = suffixParts.Count > 0 ? "-" + string.Join("-", suffixParts) : "";
        var timestamp = (utcNow ?? DateTime.UtcNow).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"admin-logs{suffix}-{timestamp}.{extension}";
    }

    private static string NormalizeForFileName(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var normalized = new StringBuilder(trimmed.Length);

        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                normalized.Append(ch);
            }
            else if (ch == '-' || ch == '_')
            {
                normalized.Append(ch);
            }
            else
            {
                normalized.Append('-');
            }
        }

        return normalized.ToString().Trim('-');
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    /// <summary>
    /// Builds a CSV export for an entity audit timeline, including the
    /// audit-specific columns (EventType, EntityType, EntityId, MetadataJson).
    /// </summary>
    public string BuildTimelineCsv(IEnumerable<AppLog> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine("TimestampUtc,EventType,Level,Source,UserId,UserName,Message,MetadataJson,Exception");

        foreach (var row in rows)
        {
            csv
                .Append(EscapeCsv(row.Timestamp.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsv(row.EventType)).Append(',')
                .Append(EscapeCsv(row.Level)).Append(',')
                .Append(EscapeCsv(row.Source)).Append(',')
                .Append(EscapeCsv(row.UserId)).Append(',')
                .Append(EscapeCsv(row.User?.UserName)).Append(',')
                .Append(EscapeCsv(row.Message)).Append(',')
                .Append(EscapeCsv(row.MetadataJson)).Append(',')
                .Append(EscapeCsv(row.Exception))
                .AppendLine();
        }

        return csv.ToString();
    }

    /// <summary>
    /// Builds a JSON export for an entity audit timeline.
    /// </summary>
    public string BuildTimelineJson(IEnumerable<AppLog> rows)
    {
        var payload = rows.Select(row => new
        {
            timestampUtc = row.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            eventType = row.EventType,
            row.Level,
            row.Source,
            row.UserId,
            userName = row.User?.UserName,
            row.Message,
            metadataJson = row.MetadataJson,
            row.Exception
        });

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Produces a timeline-specific filename like
    /// <c>audit-timeline-Order-123-20260506-143000.csv</c>.
    /// </summary>
    public string BuildTimelineFileName(string entityType, string entityId, string extension, DateTime? utcNow = null)
    {
        var safeType = NormalizeForFileName(entityType);
        var safeId = NormalizeForFileName(entityId);
        var timestamp = (utcNow ?? DateTime.UtcNow).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"audit-timeline-{safeType}-{safeId}-{timestamp}.{extension}";
    }
}

