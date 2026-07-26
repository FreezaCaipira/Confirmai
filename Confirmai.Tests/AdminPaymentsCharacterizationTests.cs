using Confirmai.Services.Admin;
using Xunit;

namespace Confirmai.Tests;

public class AdminPaymentsCharacterizationTests
{
    [Theory]
    [InlineData(0, 0, "OK", "admin-payments-gateway-severity--ok")]
    [InlineData(4, 0, "OK", "admin-payments-gateway-severity--ok")]
    [InlineData(0, 1, "Aten\u00E7\u00E3o", "admin-payments-gateway-severity--warning")]
    [InlineData(5, 0, "Aten\u00E7\u00E3o", "admin-payments-gateway-severity--warning")]
    [InlineData(10, 0, "Cr\u00EDtico", "admin-payments-gateway-severity--critical")]
    [InlineData(0, 3, "Cr\u00EDtico", "admin-payments-gateway-severity--critical")]
    [InlineData(9, 3, "Cr\u00EDtico", "admin-payments-gateway-severity--critical")]
    public void GetGatewaySeverity_ReturnsCorrectSeverity(int pending, int stalePending, string expectedLabel, string expectedClass)
    {
        var (label, cssClass) = AdminPaymentsSummaryService.GetGatewaySeverity(pending, stalePending);

        Assert.Equal(expectedLabel, label);
        Assert.Equal(expectedClass, cssClass);
    }

    [Fact]
    public void BuildPendingTrendLabel_NullLatest_ReturnsNoDataMessage()
    {
        var label = AdminPaymentsSummaryService.BuildPendingTrendLabel(null, 5, out var delta);

        Assert.Equal("Sem dados suficientes para tend\u00EAncia.", label);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_NullBaseline_ReturnsNoBaselineMessage()
    {
        var label = AdminPaymentsSummaryService.BuildPendingTrendLabel(10, null, out var delta);

        Assert.Equal("Atual: 10 pendente(s); sem baseline de 24h.", label);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_PositiveDelta_ReturnsRisingMessage()
    {
        var label = AdminPaymentsSummaryService.BuildPendingTrendLabel(15, 10, out var delta);

        Assert.Equal("Subindo (+5) nas \u00FAltimas 24h.", label);
        Assert.Equal(5, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_NegativeDelta_ReturnsFallingMessage()
    {
        var label = AdminPaymentsSummaryService.BuildPendingTrendLabel(5, 10, out var delta);

        Assert.Equal("Caindo (-5) nas \u00FAltimas 24h.", label);
        Assert.Equal(-5, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_ZeroDelta_ReturnsStableMessage()
    {
        var label = AdminPaymentsSummaryService.BuildPendingTrendLabel(10, 10, out var delta);

        Assert.Equal("Est\u00E1vel (sem varia\u00E7\u00E3o nas \u00FAltimas 24h).", label);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void GetSweepMetric_NullJson_ReturnsNull()
    {
        Assert.Null(AdminPaymentsSummaryService.GetSweepMetric(null, "considered"));
    }

    [Fact]
    public void GetSweepMetric_EmptyJson_ReturnsNull()
    {
        Assert.Null(AdminPaymentsSummaryService.GetSweepMetric("", "considered"));
    }

    [Fact]
    public void GetSweepMetric_ValidJson_ReturnsValue()
    {
        var json = """{"considered": 50, "updated": 45, "stillPending": 5}""";
        Assert.Equal(50, AdminPaymentsSummaryService.GetSweepMetric(json, "considered"));
    }

    [Fact]
    public void GetSweepMetric_MissingProperty_ReturnsNull()
    {
        var json = """{"considered": 50}""";
        Assert.Null(AdminPaymentsSummaryService.GetSweepMetric(json, "updated"));
    }

    [Fact]
    public void GetSweepMetric_NonNumberProperty_ReturnsNull()
    {
        var json = """{"considered": "fifty"}""";
        Assert.Null(AdminPaymentsSummaryService.GetSweepMetric(json, "considered"));
    }

    [Fact]
    public void GetSweepMetric_InvalidJson_ReturnsNull()
    {
        Assert.Null(AdminPaymentsSummaryService.GetSweepMetric("not valid json", "considered"));
    }

    [Fact]
    public void HasSweepOrigin_NullJson_ReturnsFalse()
    {
        Assert.False(AdminPaymentsSummaryService.HasSweepOrigin(null, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_EmptyJson_ReturnsFalse()
    {
        Assert.False(AdminPaymentsSummaryService.HasSweepOrigin("", "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_MatchingOrigin_ReturnsTrue()
    {
        var json = """{"origin": "automatic", "considered": 50}""";
        Assert.True(AdminPaymentsSummaryService.HasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_NonMatchingOrigin_ReturnsFalse()
    {
        var json = """{"origin": "manual", "considered": 50}""";
        Assert.False(AdminPaymentsSummaryService.HasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_CaseInsensitive_ReturnsTrue()
    {
        var json = """{"origin": "Automatic"}""";
        Assert.True(AdminPaymentsSummaryService.HasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_MissingOriginProperty_ReturnsFalse()
    {
        var json = """{"considered": 50}""";
        Assert.False(AdminPaymentsSummaryService.HasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_InvalidJson_ReturnsFalse()
    {
        Assert.False(AdminPaymentsSummaryService.HasSweepOrigin("not json", "automatic"));
    }

    [Fact]
    public void BuildSweepDetails_NullJson_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, AdminPaymentsSummaryService.BuildSweepDetails(null));
    }

    [Fact]
    public void BuildSweepDetails_EmptyJson_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, AdminPaymentsSummaryService.BuildSweepDetails(""));
    }

    [Fact]
    public void BuildSweepDetails_ValidJson_ReturnsFormattedDetails()
    {
        var json = """{"considered": 50, "updated": 45, "stillPending": 3, "notFound": 2}""";
        var result = AdminPaymentsSummaryService.BuildSweepDetails(json);
        Assert.Contains("Considerados=50", result);
        Assert.Contains("Atualizados=45", result);
        Assert.Contains("Pendentes=3", result);
        Assert.Contains("N\u00E3o encontrados=2", result);
    }

    [Fact]
    public void BuildSweepDetails_MissingProperties_ReturnsZeros()
    {
        var json = """{"considered": 10}""";
        var result = AdminPaymentsSummaryService.BuildSweepDetails(json);
        Assert.Contains("Considerados=10", result);
        Assert.Contains("Atualizados=0", result);
        Assert.Contains("Pendentes=0", result);
        Assert.Contains("N\u00E3o encontrados=0", result);
    }

    [Fact]
    public void BuildSweepDetails_InvalidJson_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, AdminPaymentsSummaryService.BuildSweepDetails("not json"));
    }
}
