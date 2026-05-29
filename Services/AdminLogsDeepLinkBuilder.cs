using System.Globalization;

namespace Confirmai.Services;

public static class AdminLogsDeepLinkBuilder
{
    public static string BuildPaymentPanelStaleLink(DateTime today)
    {
        var endDate = today.Date;
        var startDate = endDate.AddDays(-6);

        return $"/admin/logs?eventType={AuditEvents.PaymentReconciliationPanelStale}&startDate={startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&endDate={endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
    }
}