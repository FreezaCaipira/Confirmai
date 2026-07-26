using Confirmai.Enums;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;

namespace Confirmai.Services.Payment;

public sealed class SummaryAgeTracker
{
    private readonly LogService _logService;
    private DateTime? _lastRefreshAt;
    private DateTime? _stalenessStartedAt;
    private bool _stalenessIncidentLogged;

    public string AgeLabel { get; private set; } = "n/d";
    public string AgeClass { get; private set; } = string.Empty;
    public string LastRefreshLabel { get; private set; } = "Ainda não atualizado.";

    public const int RefreshIntervalSeconds = 30;
    public const int WarningThresholdSeconds = RefreshIntervalSeconds * 2;
    public const int AuditThresholdSeconds = 5 * 60;

    public SummaryAgeTracker(LogService logService)
    {
        _logService = logService;
    }

    public void MarkRefreshed()
    {
        _lastRefreshAt = DateTime.UtcNow;
        LastRefreshLabel = _lastRefreshAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        UpdateAgeLabel();
    }

    public void UpdateAgeLabel()
    {
        if (!_lastRefreshAt.HasValue)
        {
            AgeLabel = "n/d";
            AgeClass = string.Empty;
            _stalenessStartedAt = null;
            _stalenessIncidentLogged = false;
            return;
        }

        var elapsed = DateTime.UtcNow - _lastRefreshAt.Value;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        if (elapsed.TotalHours >= 1)
        {
            AgeLabel = $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s";
        }
        else if (elapsed.TotalMinutes >= 1)
        {
            AgeLabel = $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds:D2}s";
        }
        else
        {
            AgeLabel = $"{elapsed.Seconds}s";
        }

        var isWarning = elapsed.TotalSeconds >= WarningThresholdSeconds;
        AgeClass = isWarning
            ? "admin-payments-summary-age admin-payments-summary-age--warning"
            : "admin-payments-summary-age";

        if (isWarning)
        {
            _stalenessStartedAt ??= DateTime.UtcNow;
            return;
        }

        _stalenessStartedAt = null;
        _stalenessIncidentLogged = false;
    }

    public async Task TryWriteStalenessAuditAsync(string actorUserId, bool autoRefreshEnabled, bool pausedByVisibility)
    {
        if (_stalenessIncidentLogged || !_stalenessStartedAt.HasValue)
            return;

        var elapsedSinceStaleness = DateTime.UtcNow - _stalenessStartedAt.Value;
        if (elapsedSinceStaleness.TotalSeconds < AuditThresholdSeconds)
            return;

        await _logService.AuditAsync(
            eventType: AuditEvents.PaymentReconciliationPanelStale,
            entityType: AuditEntities.Payment,
            entityId: null,
            message: "Painel de reconciliação permaneceu sem atualização efetiva por mais de 5 minutos.",
            actorUserId: actorUserId,
            source: AdminAuditSources.Payments,
            level: "Warning",
            metadata: new
            {
                origin = "admin.payments.panel",
                staleForSeconds = (int)Math.Round(elapsedSinceStaleness.TotalSeconds, MidpointRounding.AwayFromZero),
                staleAuditThresholdSeconds = AuditThresholdSeconds,
                warningThresholdSeconds = WarningThresholdSeconds,
                autoRefreshEnabled,
                pausedByVisibility,
                lastSummaryRefreshAtUtc = _lastRefreshAt
            });

        _stalenessIncidentLogged = true;
    }
}
