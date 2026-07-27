using System.Globalization;

namespace Confirmai.Services.Utility;

public static class MailboxFormatter
{
    public static string BuildSnippet(string body, string noContentText)
    {
        if (string.IsNullOrWhiteSpace(body))
            return noContentText;

        var trimmed = body.Trim();
        return trimmed.Length <= 96
            ? trimmed
            : $"{trimmed[..96]}...";
    }

    public static string BuildAvatarInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0][..1].ToUpperInvariant();

        return string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant();
    }

    public static string FormatRelativeTime(DateTime utcDate, string nowText, string yesterdayText)
    {
        var now = DateTime.UtcNow;
        var delta = now - utcDate;
        if (delta.TotalMinutes < 1)
            return nowText;

        if (delta.TotalMinutes < 60)
            return $"{Math.Max(1, (int)Math.Floor(delta.TotalMinutes))}m";

        var localDate = utcDate.ToLocalTime();
        var today = DateTime.Now.Date;
        if (localDate.Date == today)
            return $"{localDate:HH:mm}";

        if (localDate.Date == today.AddDays(-1))
            return yesterdayText;

        return localDate.ToString("dd/MM", CultureInfo.InvariantCulture);
    }

    public static int TotalPages(int total, int pageSize)
    {
        if (total <= 0)
            return 1;
        return (int)Math.Ceiling(total / (double)pageSize);
    }
}
