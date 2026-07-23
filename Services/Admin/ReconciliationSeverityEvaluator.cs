namespace Confirmai.Services.Admin;

public record ReconciliationSeverityResult(string Label, string CssClass);

public class ReconciliationSeverityEvaluator
{
    public const string CriticalLabel = "Cr\u00EDtico";
    public const string WarningLabel = "Aten\u00E7\u00E3o";
    public const string OkLabel = "OK";

    public const string CriticalClass = "admin-payments-severity-pill--critical";
    public const string WarningClass = "admin-payments-severity-pill--warning";
    public const string OkClass = "admin-payments-severity-pill--ok";

    public ReconciliationSeverityResult Evaluate(
        int stalePending,
        int pendingTrendDelta24h,
        int pendingWithChargeId,
        int pendingTrendWarningThreshold,
        int pendingTrendCriticalThreshold)
    {
        var isCritical = stalePending >= 10
            || pendingTrendDelta24h >= 10
            || pendingWithChargeId >= 25;

        var isWarning = stalePending >= 3
            || pendingTrendDelta24h >= pendingTrendWarningThreshold
            || pendingWithChargeId >= 10;

        if (pendingTrendDelta24h >= pendingTrendCriticalThreshold)
            isCritical = true;

        if (isCritical)
            return new ReconciliationSeverityResult(CriticalLabel, CriticalClass);

        if (isWarning)
            return new ReconciliationSeverityResult(WarningLabel, WarningClass);

        return new ReconciliationSeverityResult(OkLabel, OkClass);
    }
}
