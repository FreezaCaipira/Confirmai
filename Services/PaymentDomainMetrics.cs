using System.Diagnostics.Metrics;
using System.Text.Json;
using Confirmai.Models;

namespace Confirmai.Services;

/// <summary>
/// Domain-level counters used by production payment/reconciliation alerts.
/// Metrics are emitted from audit/log events to avoid duplicating instrumentation
/// across webhook/admin/worker flows.
/// </summary>
public sealed class PaymentDomainMetrics
{
    public const string MeterName = "Confirmai.Payments";

    private static readonly Meter Meter = new(MeterName);

    private readonly Counter<long> _panelStaleCounter =
        Meter.CreateCounter<long>("confirmai_payment_reconciliation_panel_stale_total");

    private readonly Counter<long> _paymentFailedCounter =
        Meter.CreateCounter<long>("confirmai_payment_failed_total");

    private readonly Counter<long> _paymentRefundedCounter =
        Meter.CreateCounter<long>("confirmai_payment_refunded_total");

    private readonly Counter<long> _webhookWarningsCounter =
        Meter.CreateCounter<long>("confirmai_webhook_warnings_total");

    private readonly Counter<long> _reconciliationPendingGrowthEventsCounter =
        Meter.CreateCounter<long>("confirmai_payment_reconciliation_pending_growth_events_total");

    private readonly object _sync = new();
    private int? _lastStillPending;

    public void Track(AppLog log)
    {
        if (log.Source == AdminAuditSources.Webhook && log.Level == "Warning")
        {
            _webhookWarningsCounter.Add(1);
        }

        if (string.IsNullOrWhiteSpace(log.EventType))
        {
            return;
        }

        switch (log.EventType)
        {
            case AuditEvents.PaymentReconciliationPanelStale:
                _panelStaleCounter.Add(1);
                break;

            case AuditEvents.PaymentFailed:
                _paymentFailedCounter.Add(1);
                break;

            case AuditEvents.PaymentRefunded:
                _paymentRefundedCounter.Add(1);
                break;

            case AuditEvents.PaymentReconciliationSweep:
                TrackPendingGrowthFromSweepMetadata(log.MetadataJson);
                break;
        }
    }

    private void TrackPendingGrowthFromSweepMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (!doc.RootElement.TryGetProperty("stillPending", out var stillPendingProp) ||
                !stillPendingProp.TryGetInt32(out var stillPending))
            {
                return;
            }

            lock (_sync)
            {
                if (_lastStillPending.HasValue && stillPending > _lastStillPending.Value)
                {
                    _reconciliationPendingGrowthEventsCounter.Add(1);
                }

                _lastStillPending = stillPending;
            }
        }
        catch
        {
            // Metrics must never break request/worker execution.
        }
    }
}
