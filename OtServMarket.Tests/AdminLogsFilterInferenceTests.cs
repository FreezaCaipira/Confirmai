using Confirmai.Services;

namespace Confirmai.Tests;

public class AdminLogsFilterInferenceTests
{
    [Fact]
    public void InferQuickRangePreset_WhenDatesAreEmpty_ReturnsNone()
    {
        var result = AdminLogsFilterInference.InferQuickRangePreset(null, null, new DateTime(2026, 3, 15));

        Assert.Equal(AdminLogsQuickRangePreset.None, result);
    }

    [Fact]
    public void InferQuickRangePreset_WhenRangeMatchesToday_ReturnsToday()
    {
        var today = new DateTime(2026, 3, 15);

        var result = AdminLogsFilterInference.InferQuickRangePreset(today, today, today);

        Assert.Equal(AdminLogsQuickRangePreset.Today, result);
    }

    [Fact]
    public void InferQuickRangePreset_WhenRangeMatchesLast7Days_ReturnsLast7Days()
    {
        var today = new DateTime(2026, 3, 15);

        var result = AdminLogsFilterInference.InferQuickRangePreset(today.AddDays(-6), today, today);

        Assert.Equal(AdminLogsQuickRangePreset.Last7Days, result);
    }

    [Fact]
    public void InferQuickRangePreset_WhenRangeMatchesCurrentMonth_ReturnsCurrentMonth()
    {
        var today = new DateTime(2026, 3, 15);

        var result = AdminLogsFilterInference.InferQuickRangePreset(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), today);

        Assert.Equal(AdminLogsQuickRangePreset.CurrentMonth, result);
    }

    [Fact]
    public void InferQuickRangePreset_WhenRangeIsCustom_ReturnsCustom()
    {
        var today = new DateTime(2026, 3, 15);

        var result = AdminLogsFilterInference.InferQuickRangePreset(new DateTime(2026, 3, 2), new DateTime(2026, 3, 10), today);

        Assert.Equal(AdminLogsQuickRangePreset.Custom, result);
    }

    [Fact]
    public void InferAuditQuickFilter_WhenSourceIsDifferent_ReturnsAll()
    {
        var result = AdminLogsFilterInference.InferAuditQuickFilter("Webhook", AdminAuditLevels.Success);

        Assert.Equal(AdminLogsAuditQuickFilter.All, result);
    }

    [Fact]
    public void InferAuditQuickFilter_WhenSourceMatchesAndLevelIsSuccess_ReturnsReleaseSuccess()
    {
        var result = AdminLogsFilterInference.InferAuditQuickFilter(AdminAuditSources.OrdersReview, AdminAuditLevels.Success);

        Assert.Equal(AdminLogsAuditQuickFilter.ReleaseSuccess, result);
    }

    [Fact]
    public void InferAuditQuickFilter_WhenSourceMatchesAndLevelIsRefused_ReturnsReleaseRefused()
    {
        var result = AdminLogsFilterInference.InferAuditQuickFilter(AdminAuditSources.OrdersReview, AdminAuditLevels.Refused);

        Assert.Equal(AdminLogsAuditQuickFilter.ReleaseRefused, result);
    }

    [Fact]
    public void InferAuditQuickFilter_WhenSourceMatchesAndLevelIsOther_ReturnsReleaseOnly()
    {
        var result = AdminLogsFilterInference.InferAuditQuickFilter(AdminAuditSources.OrdersReview, "Debug");

        Assert.Equal(AdminLogsAuditQuickFilter.ReleaseOnly, result);
    }

    [Fact]
    public void InferAuditQuickFilter_WhenSourceMatchesSecurityPolicy_ReturnsSecurityPolicy()
    {
        var result = AdminLogsFilterInference.InferAuditQuickFilter(AdminAuditSources.SecurityPolicy, AdminAuditLevels.Success);

        Assert.Equal(AdminLogsAuditQuickFilter.SecurityPolicy, result);
    }
}


