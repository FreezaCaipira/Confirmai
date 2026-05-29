using Confirmai.Services;

namespace Confirmai.Tests;

public class AdminLogsFilterStateMergerTests
{
    [Fact]
    public void ApplyQueryOverrides_WhenNoOverrides_ReturnsOriginalState()
    {
        var state = new AdminLogsFilterState
        {
            GlobalSearch = "term",
            UserId = "user-1",
            Source = "Webhook",
            Message = "msg",
            Level = "Info",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 10),
            EventType = "payment.received",
            EntityType = "Payment",
            Page = 4
        };

        var overrides = new AdminLogsQueryOverrides(null, null, null, null, null, null);

        var merged = AdminLogsFilterStateMerger.ApplyQueryOverrides(state, overrides);

        Assert.Equal(state.GlobalSearch, merged.GlobalSearch);
        Assert.Equal(state.UserId, merged.UserId);
        Assert.Equal(state.Source, merged.Source);
        Assert.Equal(state.Level, merged.Level);
        Assert.Equal(state.StartDate, merged.StartDate);
        Assert.Equal(state.EndDate, merged.EndDate);
        Assert.Equal(state.EventType, merged.EventType);
        Assert.Equal(state.EntityType, merged.EntityType);
        Assert.Equal(state.Page, merged.Page);
    }

    [Fact]
    public void ApplyQueryOverrides_WhenOverridesExist_QueryValuesWinAndPageResets()
    {
        var state = new AdminLogsFilterState
        {
            GlobalSearch = "term",
            UserId = "user-1",
            Source = "Webhook",
            Message = "msg",
            Level = "Info",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 10),
            EventType = "payment.received",
            EntityType = "Payment",
            Page = 4
        };

        var overrides = new AdminLogsQueryOverrides(
            EventType: AuditEvents.PaymentReconciliationPanelStale,
            Source: "Payments",
            Level: "Warning",
            EntityType: "Payment",
            StartDate: new DateTime(2026, 5, 21),
            EndDate: new DateTime(2026, 5, 28));

        var merged = AdminLogsFilterStateMerger.ApplyQueryOverrides(state, overrides);

        Assert.Equal("term", merged.GlobalSearch);
        Assert.Equal("user-1", merged.UserId);
        Assert.Equal("Payments", merged.Source);
        Assert.Equal("Warning", merged.Level);
        Assert.Equal(new DateTime(2026, 5, 21), merged.StartDate);
        Assert.Equal(new DateTime(2026, 5, 28), merged.EndDate);
        Assert.Equal(AuditEvents.PaymentReconciliationPanelStale, merged.EventType);
        Assert.Equal("Payment", merged.EntityType);
        Assert.Equal(1, merged.Page);
    }

    [Fact]
    public void ApplyQueryOverrides_WhenOnlyDateRangeOverrides_PreservesOtherStoredFilters()
    {
        var state = new AdminLogsFilterState
        {
            GlobalSearch = "term",
            UserId = "user-1",
            Source = "Webhook",
            Message = "msg",
            Level = "Info",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 10),
            EventType = "payment.received",
            EntityType = "Payment",
            Page = 4
        };

        var overrides = new AdminLogsQueryOverrides(
            EventType: null,
            Source: null,
            Level: null,
            EntityType: null,
            StartDate: new DateTime(2026, 5, 21),
            EndDate: new DateTime(2026, 5, 28));

        var merged = AdminLogsFilterStateMerger.ApplyQueryOverrides(state, overrides);

        Assert.Equal("Webhook", merged.Source);
        Assert.Equal("Info", merged.Level);
        Assert.Equal("payment.received", merged.EventType);
        Assert.Equal("Payment", merged.EntityType);
        Assert.Equal(new DateTime(2026, 5, 21), merged.StartDate);
        Assert.Equal(new DateTime(2026, 5, 28), merged.EndDate);
        Assert.Equal(1, merged.Page);
    }

    [Fact]
    public void ApplyQueryOverrides_WhenOnlyEventTypeOverrides_PreservesStoredDateRange()
    {
        var state = new AdminLogsFilterState
        {
            Source = "Webhook",
            Level = "Info",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 10),
            EventType = "payment.received",
            EntityType = "Payment",
            Page = 2
        };

        var overrides = new AdminLogsQueryOverrides(
            EventType: AuditEvents.PaymentReconciliationPanelStale,
            Source: null,
            Level: null,
            EntityType: null,
            StartDate: null,
            EndDate: null);

        var merged = AdminLogsFilterStateMerger.ApplyQueryOverrides(state, overrides);

        Assert.Equal("Webhook", merged.Source);
        Assert.Equal("Info", merged.Level);
        Assert.Equal(new DateTime(2026, 5, 1), merged.StartDate);
        Assert.Equal(new DateTime(2026, 5, 10), merged.EndDate);
        Assert.Equal(AuditEvents.PaymentReconciliationPanelStale, merged.EventType);
        Assert.Equal(1, merged.Page);
    }
}
