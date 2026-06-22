using Confirmai.Services.Payment;
using Confirmai.Services.Payment.Shared;
using Confirmai.Services.Core;
using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

/// <summary>
/// Unit tests for <see cref="EfiBankWebhookService.HandleAsync"/>.
/// All tests use an in-memory DB — no real network or certificate needed.
/// </summary>
public class EfiBankWebhookServiceTests
{
    // ── Secret validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSecretIsWrong()
    {
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db, webhookSecret: "correct-secret");
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TX1"), secret: "wrong");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(result));
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSecretIsMissing()
    {
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db, webhookSecret: "correct-secret");
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TX1"), secret: null);

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(result));
    }

    [Fact]
    public async Task HandleAsync_SkipsSecretCheck_WhenWebhookSecretNotConfigured()
    {
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db, webhookSecret: ""); // empty → no check
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("NOOP"), secret: null);

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
    }

    // ── Payload validation ────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenJsonIsInvalid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db);
        var ctx = WebhookTestFactory.CreateEfiBankContext("{not-valid-json", secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(result));
    }

    [Fact]
    public async Task HandleAsync_ReturnsPayloadTooLarge_WhenBodyExceedsLimit()
    {
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db);
        var hugeBody = "{\"pix\":[{\"txid\":\"" + new string('A', 70 * 1024) + "\"}]}";
        var ctx = WebhookTestFactory.CreateEfiBankContext(hugeBody, secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, StatusOf(result));
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_MarksPaid_WhenTxIdMatchesPendingConfirmation()
    {
        using var db = TestDataFactory.CreateDbContext();
        var conf = SeedConfirmation(db, txId: "TXPAID1", status: EventConfirmationPaymentStatus.Pending);
        var svc = BuildService(db);
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TXPAID1"), secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
        await db.Entry(conf).ReloadAsync();
        Assert.Equal(EventConfirmationPaymentStatus.Paid, conf.PaymentStatus);
        Assert.True(conf.HasPaid);
        Assert.Equal("EfiBank", conf.PaymentGatewayName);
    }

    [Fact]
    public async Task HandleAsync_NotifiesEventBus_WhenPaymentConfirmed()
    {
        using var db = TestDataFactory.CreateDbContext();
        SeedConfirmation(db, txId: "TXNOTIFY", userId: "user-42");
        var bus = new PaymentEventBus();
        string? notifiedUserId = null;
        bus.OnPaymentConfirmed += (uid, _) => { notifiedUserId = uid; };

        var svc = BuildService(db, eventBus: bus);
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TXNOTIFY"), secret: "test-secret");
        await svc.HandleAsync(ctx);

        Assert.Equal("user-42", notifiedUserId);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_IsIdempotent_WhenConfirmationAlreadyPaid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var conf = SeedConfirmation(db, txId: "TXIDEM", status: EventConfirmationPaymentStatus.Paid);
        var svc = BuildService(db);
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TXIDEM"), secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
        // Should still be Paid and not throw
        await db.Entry(conf).ReloadAsync();
        Assert.Equal(EventConfirmationPaymentStatus.Paid, conf.PaymentStatus);
    }

    [Fact]
    public async Task HandleAsync_DoesNotMarkPaid_WhenConfirmationIsRefunded()
    {
        using var db = TestDataFactory.CreateDbContext();
        var conf = SeedConfirmation(db, txId: "TXREFUND", status: EventConfirmationPaymentStatus.Refunded);
        var svc = BuildService(db);
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TXREFUND"), secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
        await db.Entry(conf).ReloadAsync();
        Assert.Equal(EventConfirmationPaymentStatus.Refunded, conf.PaymentStatus);
    }

    // ── Unknown txId ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenTxIdNotFound()
    {
        // EfiBank expects 200 OK even if we don't recognise the txId.
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db);
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("TXUNKNOWN"), secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
    }

    // ── mTLS client-cert validation ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ReturnsForbidden_WhenClientCertSubjectConfigured_AndNoCert()
    {
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db, webhookClientCertSubject: "conta.efipay.com.br");
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("NOOP"), secret: "test-secret");
        // No client certificate on the connection.

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf(result));
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenClientCertSubjectNotConfigured()
    {
        // mTLS not configured → cert check is skipped entirely.
        using var db = TestDataFactory.CreateDbContext();
        var svc = BuildService(db, webhookClientCertSubject: null);
        var ctx = WebhookTestFactory.CreateEfiBankContext(PixPayload("NOOP"), secret: "test-secret");

        var result = await svc.HandleAsync(ctx);

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EfiBankWebhookService BuildService(
        Confirmai.Data.AppDbContext db,
        string webhookSecret = "test-secret",
        string? webhookClientCertSubject = null,
        PaymentEventBus? eventBus = null)
    {
        var opts = Options.Create(new EfiBankOptions
        {
            WebhookSecret           = webhookSecret,
            WebhookClientCertSubject = webhookClientCertSubject,
        });
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"efibank-webhook-{Guid.NewGuid()}");
        var log  = new LogService(dbFactory, NullLogger<LogService>.Instance);
        eventBus ??= new PaymentEventBus();
        var paymentMarker = new WebhookPaymentMarker(db, log, eventBus);
        return new EfiBankWebhookService(log, opts, paymentMarker);
    }

    private static EventConfirmation SeedConfirmation(
        Confirmai.Data.AppDbContext db,
        string txId,
        string userId = "user-1",
        EventConfirmationPaymentStatus status = EventConfirmationPaymentStatus.Pending)
    {
        var conf = new EventConfirmation
        {
            EventId       = 1,
            UserId        = userId,
            PixTxId       = txId,
            PaymentStatus = status,
            HasPaid       = status == EventConfirmationPaymentStatus.Paid,
        };
        db.EventConfirmations.Add(conf);
        db.SaveChanges();
        return conf;
    }

    private static string PixPayload(string txId) =>
        $$"""{"pix":[{"txid":"{{txId}}","valor":"1.00","horario":"2026-05-29T10:00:00Z"}]}""";

    private static int StatusOf(IResult result) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode
        ?? StatusCodes.Status200OK;
}
