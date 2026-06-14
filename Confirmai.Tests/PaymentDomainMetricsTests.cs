using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;

namespace Confirmai.Tests;

public class PaymentDomainMetricsTests
{
    private static PaymentDomainMetrics CreateSut() => new();

    private static AppLog MakeLog(
        string? source = null,
        string level = "Info",
        string? eventType = null,
        string? metadataJson = null) => new()
    {
        Source = source ?? "",
        Level = level,
        EventType = eventType,
        MetadataJson = metadataJson
    };

    // ── Webhook warning counter ──────────────────────────────────────────

    [Fact]
    public void Track_WebhookWarning_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(source: AdminAuditSources.Webhook, level: "Warning");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_NonWebhookWarning_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(source: "Other", level: "Warning");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    // ── EventType dispatch ───────────────────────────────────────────────

    [Theory]
    [InlineData(AuditEvents.PaymentReconciliationPanelStale)]
    [InlineData(AuditEvents.PaymentFailed)]
    [InlineData(AuditEvents.PaymentRefunded)]
    public void Track_KnownPaymentEventType_DoesNotThrow(string eventType)
    {
        var sut = CreateSut();
        var log = MakeLog(eventType: eventType);

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_NullEventType_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(eventType: null);

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_EmptyEventType_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(eventType: "");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_UnknownEventType_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(eventType: "unknown.event");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    // ── Reconciliation sweep: pending growth tracking ────────────────────

    [Fact]
    public void Track_ReconciliationSweep_WithValidMetadata_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"stillPending":5}""");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_WithGrowingPending_DoesNotThrow()
    {
        var sut = CreateSut();

        // First sweep: stillPending=3
        sut.Track(MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"stillPending":3}"""));

        // Second sweep: stillPending=5 (growth)
        var ex = Record.Exception(() => sut.Track(MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"stillPending":5}""")));

        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_WithDecreasingPending_DoesNotThrow()
    {
        var sut = CreateSut();

        sut.Track(MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"stillPending":10}"""));

        var ex = Record.Exception(() => sut.Track(MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"stillPending":3}""")));

        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_NullMetadata_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: null);

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_EmptyMetadata_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: "");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_InvalidJson_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: "not json");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_MissingStillPending_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"other":"value"}""");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    [Fact]
    public void Track_ReconciliationSweep_StillPendingNotInt_DoesNotThrow()
    {
        var sut = CreateSut();
        var log = MakeLog(
            eventType: AuditEvents.PaymentReconciliationSweep,
            metadataJson: """{"stillPending":"abc"}""");

        var ex = Record.Exception(() => sut.Track(log));
        Assert.Null(ex);
    }

    // ── MeterName constant ───────────────────────────────────────────────

    [Fact]
    public void MeterName_IsExpected()
    {
        Assert.Equal("Confirmai.Payments", PaymentDomainMetrics.MeterName);
    }

    // ── Thread safety: concurrent Track calls ────────────────────────────

    [Fact]
    public void Track_ConcurrentCalls_DoNotThrow()
    {
        var sut = CreateSut();
        var logs = Enumerable.Range(0, 100)
            .Select(i => MakeLog(
                eventType: AuditEvents.PaymentReconciliationSweep,
                metadataJson: $$$"""{"stillPending":{{{i}}}}"""))
            .ToArray();

        var ex = Record.Exception(() =>
            Parallel.ForEach(logs, log => sut.Track(log)));

        Assert.Null(ex);
    }
}
