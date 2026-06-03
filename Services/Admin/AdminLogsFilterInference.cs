using Confirmai.Services.Admin;
using Confirmai.Services.Core;
namespace Confirmai.Services.Admin;

public static class AdminLogsFilterInference
{
    public static AdminLogsQuickRangePreset InferQuickRangePreset(DateTime? startDate, DateTime? endDate, DateTime today)
    {
        var start = startDate?.Date;
        var end = endDate?.Date;
        var referenceToday = today.Date;

        if (!start.HasValue && !end.HasValue)
        {
            return AdminLogsQuickRangePreset.None;
        }

        if (start == referenceToday && end == referenceToday)
        {
            return AdminLogsQuickRangePreset.Today;
        }

        if (start == referenceToday.AddDays(-6) && end == referenceToday)
        {
            return AdminLogsQuickRangePreset.Last7Days;
        }

        if (start == referenceToday.AddDays(-29) && end == referenceToday)
        {
            return AdminLogsQuickRangePreset.Last30Days;
        }

        var firstDayOfMonth = new DateTime(referenceToday.Year, referenceToday.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        if (start == firstDayOfMonth && end == lastDayOfMonth)
        {
            return AdminLogsQuickRangePreset.CurrentMonth;
        }

        return AdminLogsQuickRangePreset.Custom;
    }

    public static AdminLogsAuditQuickFilter InferAuditQuickFilter(string? source, string? level, string? eventType = null)
    {
        if (string.Equals(source, AdminAuditSources.SecurityPolicy, StringComparison.Ordinal))
        {
            return AdminLogsAuditQuickFilter.SecurityPolicy;
        }

        if (string.Equals(eventType, AuditEvents.PaymentReconciliationPanelStale, StringComparison.Ordinal))
        {
            return AdminLogsAuditQuickFilter.PaymentPanelStale;
        }

        return AdminLogsAuditQuickFilter.All;
    }
}

