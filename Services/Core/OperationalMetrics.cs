using System.Diagnostics.Metrics;

namespace Confirmai.Services.Core;

/// <summary>
/// C32 Fase C — operational metrics for failures that happen in production
/// without anyone watching: Blazor circuit health, SMTP delivery failures,
/// rejected proof uploads, and fee settlements stuck awaiting review.
/// Same pattern as PaymentDomainMetrics: a singleton with domain-named
/// instruments under one meter, registered via AddMeter in Program.cs.
/// </summary>
public sealed class OperationalMetrics
{
    public const string MeterName = "Confirmai.Ops";

    private static readonly Meter Meter = new(MeterName);

    private readonly UpDownCounter<long> _circuitsActive =
        Meter.CreateUpDownCounter<long>("confirmai_circuits_active");

    private readonly Counter<long> _circuitsReconnect =
        Meter.CreateCounter<long>("confirmai_circuits_reconnect_total");

    private readonly Counter<long> _emailSendFailed =
        Meter.CreateCounter<long>("confirmai_email_send_failed_total");

    private readonly Counter<long> _proofUploadRejected =
        Meter.CreateCounter<long>("confirmai_proof_upload_rejected_total");

    private int _staleFeeSettlements;

    public OperationalMetrics()
    {
        Meter.CreateObservableGauge("confirmai_fee_settlements_pending_stale",
            () => (long)_staleFeeSettlements);
    }

    public void CircuitOpened() => _circuitsActive.Add(1);

    public void CircuitClosed() => _circuitsActive.Add(-1);

    public void CircuitReconnected() => _circuitsReconnect.Add(1);

    /// <summary>Never tag the recipient address or token — count only.</summary>
    public void EmailSendFailed() => _emailSendFailed.Add(1);

    /// <summary>Reason tags: mime, empty, size, signature, ownership, not_found, io, unexpected.</summary>
    public void ProofUploadRejected(string reason) =>
        _proofUploadRejected.Add(1, new KeyValuePair<string, object?>("reason", reason));

    /// <summary>Latest stale-settlement count, refreshed by StaleSettlementMetricsService.</summary>
    public void RecordStaleFeeSettlements(int count) =>
        Interlocked.Exchange(ref _staleFeeSettlements, count);
}
