using System.Reflection;
using Confirmai.Pages.Admin;
using Xunit;

namespace Confirmai.Tests;

public class AdminPaymentsCharacterizationTests
{
    private static readonly Type AdminPaymentsType = typeof(AdminPayments);

    private static (string Label, string CssClass) InvokeGetGatewaySeverity(int pending, int stalePending)
    {
        var method = AdminPaymentsType.GetMethod("GetGatewaySeverity",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(int), typeof(int)],
            modifiers: null);
        Assert.NotNull(method);
        var result = ((string, string))method!.Invoke(null, [pending, stalePending])!;
        return result;
    }

    private static (string Label, int Delta) InvokeBuildPendingTrendLabel(int? latestPending, int? baselinePending)
    {
        var method = AdminPaymentsType.GetMethod("BuildPendingTrendLabel",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(int?), typeof(int?), typeof(int).MakeByRefType()],
            modifiers: null);
        Assert.NotNull(method);
        var args = new object?[] { latestPending, baselinePending, 0 };
        var label = (string)method!.Invoke(null, args)!;
        return (label, (int)args[2]!);
    }

    private static int? InvokeGetSweepMetric(string? metadataJson, string propertyName)
    {
        var method = AdminPaymentsType.GetMethod("GetSweepMetric",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string), typeof(string)],
            modifiers: null);
        Assert.NotNull(method);
        return (int?)method!.Invoke(null, [metadataJson, propertyName]);
    }

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
        var (label, cssClass) = InvokeGetGatewaySeverity(pending, stalePending);

        Assert.Equal(expectedLabel, label);
        Assert.Equal(expectedClass, cssClass);
    }

    [Fact]
    public void BuildPendingTrendLabel_NullLatest_ReturnsNoDataMessage()
    {
        var (label, delta) = InvokeBuildPendingTrendLabel(null, 5);

        Assert.Equal("Sem dados suficientes para tend\u00EAncia.", label);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_NullBaseline_ReturnsNoBaselineMessage()
    {
        var (label, delta) = InvokeBuildPendingTrendLabel(10, null);

        Assert.Equal("Atual: 10 pendente(s); sem baseline de 24h.", label);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_PositiveDelta_ReturnsRisingMessage()
    {
        var (label, delta) = InvokeBuildPendingTrendLabel(15, 10);

        Assert.Equal("Subindo (+5) nas \u00FAltimas 24h.", label);
        Assert.Equal(5, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_NegativeDelta_ReturnsFallingMessage()
    {
        var (label, delta) = InvokeBuildPendingTrendLabel(5, 10);

        Assert.Equal("Caindo (-5) nas \u00FAltimas 24h.", label);
        Assert.Equal(-5, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_ZeroDelta_ReturnsStableMessage()
    {
        var (label, delta) = InvokeBuildPendingTrendLabel(10, 10);

        Assert.Equal("Est\u00E1vel (sem varia\u00E7\u00E3o nas \u00FAltimas 24h).", label);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void GetSweepMetric_NullJson_ReturnsNull()
    {
        var result = InvokeGetSweepMetric(null, "considered");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_EmptyJson_ReturnsNull()
    {
        var result = InvokeGetSweepMetric("", "considered");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_ValidJson_ReturnsValue()
    {
        var json = """{"considered": 50, "updated": 45, "stillPending": 5}""";

        var result = InvokeGetSweepMetric(json, "considered");

        Assert.Equal(50, result);
    }

    [Fact]
    public void GetSweepMetric_MissingProperty_ReturnsNull()
    {
        var json = """{"considered": 50}""";

        var result = InvokeGetSweepMetric(json, "updated");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_NonNumberProperty_ReturnsNull()
    {
        var json = """{"considered": "fifty"}""";

        var result = InvokeGetSweepMetric(json, "considered");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_InvalidJson_ReturnsNull()
    {
        var result = InvokeGetSweepMetric("not valid json", "considered");

        Assert.Null(result);
    }

    // ── HasSweepOrigin ───────────────────────────────────────────────────

    private static bool InvokeHasSweepOrigin(string? metadataJson, string expectedOrigin)
    {
        var method = AdminPaymentsType.GetMethod("HasSweepOrigin",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string), typeof(string)],
            modifiers: null);
        Assert.NotNull(method);
        return (bool)method!.Invoke(null, [metadataJson, expectedOrigin])!;
    }

    [Fact]
    public void HasSweepOrigin_NullJson_ReturnsFalse()
    {
        Assert.False(InvokeHasSweepOrigin(null, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_EmptyJson_ReturnsFalse()
    {
        Assert.False(InvokeHasSweepOrigin("", "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_MatchingOrigin_ReturnsTrue()
    {
        var json = """{"origin": "automatic", "considered": 50}""";

        Assert.True(InvokeHasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_NonMatchingOrigin_ReturnsFalse()
    {
        var json = """{"origin": "manual", "considered": 50}""";

        Assert.False(InvokeHasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_CaseInsensitive_ReturnsTrue()
    {
        var json = """{"origin": "Automatic"}""";

        Assert.True(InvokeHasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_MissingOriginProperty_ReturnsFalse()
    {
        var json = """{"considered": 50}""";

        Assert.False(InvokeHasSweepOrigin(json, "automatic"));
    }

    [Fact]
    public void HasSweepOrigin_InvalidJson_ReturnsFalse()
    {
        Assert.False(InvokeHasSweepOrigin("not json", "automatic"));
    }

    // ── BuildSweepDetails ────────────────────────────────────────────────

    private static string InvokeBuildSweepDetails(string? metadataJson)
    {
        var method = AdminPaymentsType.GetMethod("BuildSweepDetails",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [metadataJson])!;
    }

    [Fact]
    public void BuildSweepDetails_NullJson_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InvokeBuildSweepDetails(null));
    }

    [Fact]
    public void BuildSweepDetails_EmptyJson_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InvokeBuildSweepDetails(""));
    }

    [Fact]
    public void BuildSweepDetails_ValidJson_ReturnsFormattedDetails()
    {
        var json = """{"considered": 50, "updated": 45, "stillPending": 3, "notFound": 2}""";

        var result = InvokeBuildSweepDetails(json);

        Assert.Contains("Considerados=50", result);
        Assert.Contains("Atualizados=45", result);
        Assert.Contains("Pendentes=3", result);
        Assert.Contains("N\u00E3o encontrados=2", result);
    }

    [Fact]
    public void BuildSweepDetails_MissingProperties_ReturnsZeros()
    {
        var json = """{"considered": 10}""";

        var result = InvokeBuildSweepDetails(json);

        Assert.Contains("Considerados=10", result);
        Assert.Contains("Atualizados=0", result);
        Assert.Contains("Pendentes=0", result);
        Assert.Contains("N\u00E3o encontrados=0", result);
    }

    [Fact]
    public void BuildSweepDetails_InvalidJson_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InvokeBuildSweepDetails("not json"));
    }
}
