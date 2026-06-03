using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class AdminLogsQueryOverridesParserTests
{
    [Fact]
    public void Parse_WhenQueryIsEmpty_ReturnsNoOverrides()
    {
        var uri = new Uri("https://localhost/admin/logs");

        var result = AdminLogsQueryOverridesParser.Parse(uri);

        Assert.False(result.HasAny);
        Assert.Null(result.EventType);
        Assert.Null(result.Source);
        Assert.Null(result.Level);
        Assert.Null(result.EntityType);
        Assert.Null(result.StartDate);
        Assert.Null(result.EndDate);
    }

    [Fact]
    public void Parse_WhenQueryHasFilters_ParsesAllSupportedFields()
    {
        var uri = new Uri("https://localhost/admin/logs?eventType=payment.reconciliation.panel.stale&source=Payments&level=Warning&entityType=Payment&startDate=2026-05-21&endDate=2026-05-28");

        var result = AdminLogsQueryOverridesParser.Parse(uri);

        Assert.True(result.HasAny);
        Assert.Equal("payment.reconciliation.panel.stale", result.EventType);
        Assert.Equal("Payments", result.Source);
        Assert.Equal("Warning", result.Level);
        Assert.Equal("Payment", result.EntityType);
        Assert.Equal(new DateTime(2026, 5, 21), result.StartDate);
        Assert.Equal(new DateTime(2026, 5, 28), result.EndDate);
    }

    [Fact]
    public void Parse_WhenDateIsInvalid_IgnoresDateOnly()
    {
        var uri = new Uri("https://localhost/admin/logs?eventType=payment.reconciliation.panel.stale&startDate=2026-99-99&endDate=2026-05-28");

        var result = AdminLogsQueryOverridesParser.Parse(uri);

        Assert.True(result.HasAny);
        Assert.Equal("payment.reconciliation.panel.stale", result.EventType);
        Assert.Null(result.StartDate);
        Assert.Equal(new DateTime(2026, 5, 28), result.EndDate);
    }
}
