namespace Confirmai.Configuration;

/// <summary>
/// Product metrics knobs (C42-B).
/// Configuration section: "Metrics" (env: Metrics__PageViewSampleRate).
/// </summary>
public sealed class MetricsOptions
{
    public const string Section = "Metrics";

    /// <summary>
    /// Fraction (0.0–1.0) of page-view events persisted to the audit log.
    /// 1.0 keeps every view; 0.5 keeps ~half. Affects only the persisted
    /// <c>page.view</c> rows — the OTel render histogram always records.
    /// </summary>
    public double PageViewSampleRate { get; init; } = 1.0;

    /// <summary>
    /// Viewport width below which a page view is classified as "mobile".
    /// </summary>
    public int MobileBreakpointPx { get; init; } = 768;
}
