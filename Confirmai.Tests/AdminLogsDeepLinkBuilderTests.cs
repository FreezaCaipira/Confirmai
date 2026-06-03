using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class AdminLogsDeepLinkBuilderTests
{
    [Fact]
    public void BuildPaymentPanelStaleLink_UsesExpectedEventTypeAndDefault7DayRange()
    {
        var today = new DateTime(2026, 5, 28);

        var href = AdminLogsDeepLinkBuilder.BuildPaymentPanelStaleLink(today);

        Assert.Equal(
            "/admin/logs?eventType=payment.reconciliation.panel.stale&startDate=2026-05-22&endDate=2026-05-28",
            href);
    }

    [Fact]
    public void BuildPaymentPanelStaleLink_NormalizesInputToDateOnly()
    {
        var todayWithTime = new DateTime(2026, 5, 28, 21, 45, 10, DateTimeKind.Local);

        var href = AdminLogsDeepLinkBuilder.BuildPaymentPanelStaleLink(todayWithTime);

        Assert.Contains("startDate=2026-05-22", href, StringComparison.Ordinal);
        Assert.Contains("endDate=2026-05-28", href, StringComparison.Ordinal);
    }
}
