using System.Diagnostics.Metrics;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C32 Fase C — one MeterListener test per operational metric.
/// </summary>
public class C32OperationalMetricsTests
{
    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) Ctx() =>
        TestDataFactory.CreateDbContextWithFactory();

    /// <summary>Collects every long measurement of one instrument on the ops meter.</summary>
    private sealed class LongRecorder : IDisposable
    {
        private readonly MeterListener _listener = new();
        public List<long> Values { get; } = new();
        public List<IReadOnlyList<KeyValuePair<string, object?>>> TagSets { get; } = new();

        public LongRecorder(string instrumentName)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == OperationalMetrics.MeterName &&
                    instrument.Name == instrumentName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                Values.Add(value);
                TagSets.Add(tags.ToArray());
            });
            _listener.Start();
        }

        public void Observe() => _listener.RecordObservableInstruments();

        public void Dispose() => _listener.Dispose();
    }

    [Fact]
    public void CircuitsActive_CountsOpenAndClose()
    {
        var metrics = new OperationalMetrics();
        using var recorder = new LongRecorder("confirmai_circuits_active");

        metrics.CircuitOpened();
        metrics.CircuitOpened();
        metrics.CircuitClosed();

        Assert.Equal(new long[] { 1, 1, -1 }, recorder.Values);
    }

    [Fact]
    public void CircuitsReconnect_CountsOnlyReconnects()
    {
        var metrics = new OperationalMetrics();
        using var recorder = new LongRecorder("confirmai_circuits_reconnect_total");

        metrics.CircuitReconnected();

        Assert.Equal(new long[] { 1 }, recorder.Values);
    }

    [Fact]
    public async Task EmailSendFailed_CountsSmtpFailure()
    {
        var metrics = new OperationalMetrics();
        using var recorder = new LongRecorder("confirmai_email_send_failed_total");

        var options = Options.Create(new EmailOptions
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = 1, // nothing listens here — the SMTP send fails fast
            FromEmail = "sender@test.local",
            Username = "u",
            Password = "p",
            UseSsl = false
        });
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");
        var sender = new IdentityEmailSender(
            NullLogger<IdentityEmailSender>.Instance, options, env.Object, metrics);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            sender.SendEmailAsync("dest@test.local", "assunto", "<p>corpo</p>"));

        Assert.Equal(new long[] { 1 }, recorder.Values);
    }

    [Fact]
    public async Task ProofUploadRejected_CountsWithReasonTag()
    {
        var (db, factory) = Ctx();
        var metrics = new OperationalMetrics();
        using var recorder = new LongRecorder("confirmai_proof_upload_rejected_total");

        var uploadSvc = new PixProofUploadService(factory,
            NullLogger<PixProofUploadService>.Instance,
            new LogService(factory, NullLogger<LogService>.Instance),
            metrics);

        var badMime = await uploadSvc.UploadProofAsync(1, new byte[] { 0xFF, 0xD8, 0xFF }, "application/pdf", "u1");
        var tooLarge = await uploadSvc.UploadProofAsync(1, new byte[6 * 1024 * 1024], "image/jpeg", "u1");

        Assert.False(badMime.Success);
        Assert.False(tooLarge.Success);
        Assert.Equal(2, recorder.Values.Count);
        Assert.Equal("mime", recorder.TagSets[0].Single(t => t.Key == "reason").Value);
        Assert.Equal("size", recorder.TagSets[1].Single(t => t.Key == "reason").Value);
    }

    [Fact]
    public async Task StaleFeeSettlements_GaugeReportsStaleCount()
    {
        var (db, factory) = Ctx();
        var group = TestDataFactory.CreateGroup("Test");
        db.Groups.Add(group);
        var organizer = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        db.Users.Add(organizer);
        db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            SubmittedAt = DateTime.UtcNow.AddDays(-4), // stale (> 3d default)
            Status = PlatformFeeSettlementStatus.EmAnalise
        });
        db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            SubmittedAt = DateTime.UtcNow.AddHours(-1), // fresh — not stale
            Status = PlatformFeeSettlementStatus.EmAnalise
        });
        await db.SaveChangesAsync();

        var metrics = new OperationalMetrics();
        var service = new StaleSettlementMetricsService(factory, metrics,
            new ConfigurationBuilder().Build(),
            NullLogger<StaleSettlementMetricsService>.Instance);

        using var recorder = new LongRecorder("confirmai_fee_settlements_pending_stale");
        await service.RefreshOnceAsync();
        recorder.Observe();

        Assert.Equal(new long[] { 1 }, recorder.Values);
    }
}
