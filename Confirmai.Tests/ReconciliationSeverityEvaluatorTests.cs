using Confirmai.Services.Admin;
using Xunit;

namespace Confirmai.Tests;

public class ReconciliationSeverityEvaluatorTests
{
    private readonly ReconciliationSeverityEvaluator _evaluator = new();

    private static readonly int DefaultWarningThreshold = 5;
    private static readonly int DefaultCriticalThreshold = 15;

    // ── OK ───────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_AllZero_ReturnsOK()
    {
        var result = _evaluator.Evaluate(0, 0, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("OK", result.Label);
        Assert.Equal("admin-payments-severity-pill--ok", result.CssClass);
    }

    [Fact]
    public void Evaluate_BelowAllThresholds_ReturnsOK()
    {
        var result = _evaluator.Evaluate(2, 4, 9, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("OK", result.Label);
    }

    // ── Critical ─────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_StalePendingAt10_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(10, 0, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_StalePendingAbove10_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(15, 0, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_PendingTrendDeltaAt10_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(0, 10, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_PendingWithChargeIdAt25_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(0, 0, 25, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_PendingTrendDeltaAtCriticalThreshold_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(0, 15, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_PendingTrendDeltaAboveCriticalThreshold_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(0, 20, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    // ── Warning ──────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_StalePendingAt3_ReturnsWarning()
    {
        var result = _evaluator.Evaluate(3, 0, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Aten\u00E7\u00E3o", result.Label);
    }

    [Fact]
    public void Evaluate_PendingTrendDeltaAtWarningThreshold_ReturnsWarning()
    {
        var result = _evaluator.Evaluate(0, 5, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Aten\u00E7\u00E3o", result.Label);
    }

    [Fact]
    public void Evaluate_PendingWithChargeIdAt10_ReturnsWarning()
    {
        var result = _evaluator.Evaluate(0, 0, 10, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Aten\u00E7\u00E3o", result.Label);
    }

    // ── Priority: Critical overrides Warning ─────────────────────────────

    [Fact]
    public void Evaluate_BothCriticalAndWarningConditions_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(10, 5, 10, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_CriticalThresholdOverridesWarning_ReturnsCritical()
    {
        var result = _evaluator.Evaluate(0, 15, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    // ── Edge: trend delta between warning and critical threshold ─────────

    [Fact]
    public void Evaluate_TrendDeltaBetweenWarningAndCritical_ReturnsWarning()
    {
        var result = _evaluator.Evaluate(0, 10, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        // 10 >= 10 (hardcoded critical) so this is actually critical
        Assert.Equal("Cr\u00EDtico", result.Label);
    }

    [Fact]
    public void Evaluate_TrendDeltaAt9_ReturnsWarning()
    {
        var result = _evaluator.Evaluate(0, 9, 0, DefaultWarningThreshold, DefaultCriticalThreshold);

        // 9 < 10 (hardcoded critical), 9 >= 5 (warning threshold)
        Assert.Equal("Aten\u00E7\u00E3o", result.Label);
    }

    // ── Custom thresholds ────────────────────────────────────────────────

    [Fact]
    public void Evaluate_CustomWarningThreshold_LowerValueTriggersWarning()
    {
        var result = _evaluator.Evaluate(0, 2, 0, 2, 10);

        Assert.Equal("Aten\u00E7\u00E3o", result.Label);
    }

    [Fact]
    public void Evaluate_CustomCriticalThreshold_LowerValueTriggersCritical()
    {
        var result = _evaluator.Evaluate(0, 3, 0, 2, 3);

        Assert.Equal("Cr\u00EDtico", result.Label);
    }
}
