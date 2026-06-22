using Confirmai.Services.Payment;
using Confirmai.Services.Core;
using Confirmai.Data;
using Confirmai.Hubs;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Confirmai.Tests;

public class BtcPayWebhookServiceTests
{
    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenJsonIsInvalid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: "{invalid-json");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenRequiredFieldsAreMissing()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: "{\"invoice\":\"inv-1\"}");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenInvoiceIdHasInvalidType()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: "{\"invoiceId\":123,\"type\":\"InvoiceSettled\"}");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenTypeHasInvalidType()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: "{\"invoiceId\":\"inv-7\",\"type\":5}");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenInvoiceIdIsBlank()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: "{\"invoiceId\":\"   \",\"type\":\"InvoiceSettled\"}");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenTypeIsBlank()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: "{\"invoiceId\":\"inv-8\",\"type\":\"   \"}");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSecretIsInvalid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContext(secret: "wrong", invoiceId: "inv-1", eventType: "InvoiceSettled");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenConfiguredSecretIsMissing()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "");
        var context = WebhookTestFactory.CreateContext(secret: "anything", invoiceId: "inv-0", eventType: "InvoiceSettled");

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPayloadTooLarge_WhenBodyExceedsLimit()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected");

        var hugeValue = new string('a', 70 * 1024);
        var rawBody = $"{{\"invoiceId\":\"{hugeValue}\",\"type\":\"InvoiceSettled\"}}";
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: rawBody);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_RespectsConfiguredWebhookMaxBodyBytes()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected", webhookMaxBodyBytes: 64);

        var rawBody = "{\"invoiceId\":\"" + new string('b', 120) + "\",\"type\":\"InvoiceSettled\"}";
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: rawBody);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UsesDefaultLimit_WhenWebhookMaxBodyBytesIsNotANumber()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected", webhookMaxBodyBytesRaw: "abc");

        var mediumValue = new string('x', 10 * 1024);
        var rawBody = $"{{\"invoiceId\":\"{mediumValue}\",\"type\":\"InvoiceSettled\"}}";
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: rawBody);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UsesDefaultLimit_WhenWebhookMaxBodyBytesIsNegative()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateService(db, webhookSecret: "expected", webhookMaxBodyBytesRaw: "-5");

        var mediumValue = new string('y', 10 * 1024);
        var rawBody = $"{{\"invoiceId\":\"{mediumValue}\",\"type\":\"InvoiceSettled\"}}";
        var context = WebhookTestFactory.CreateContextRaw(secret: "expected", rawBody: rawBody);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_DoesNotConfirm_WhenEventTypeIsNotSettled()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-2", address: "tb1qaddress");
        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-2", eventType: "InvoiceProcessing");

        var result = await service.HandleAsync(context);
        var persisted = await db.Payments.FirstAsync(p => p.Id == payment.Id);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.False(persisted.IsPaid);
    }

    [Fact]
    public async Task HandleAsync_ConfirmsPaymentAndNotifiesUser_WhenInvoiceSettled()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-3", address: "tb1qaddress", buyerId: "buyer-1");

        var (hubContext, clientProxy) = SignalRTestFactory.CreateHubContextForUser("buyer-1");
        var service = CreateService(db, webhookSecret: "expected", hubContext: hubContext);
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-3", eventType: "InvoiceSettled");

        var result = await service.HandleAsync(context);
        var persisted = await db.Payments.FirstAsync(p => p.Id == payment.Id);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.True(persisted.IsPaid);
        Assert.NotNull(persisted.PaidAt);

        clientProxy.Verify(
            p => p.SendCoreAsync(
                "PaymentConfirmed",
                It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], "inv-3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenTimestampIsTooOld()
    {
        using var db = TestDataFactory.CreateDbContext();
        TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-ts-old", address: "tb1qaddress");
        var service = CreateService(db, webhookSecret: "expected");

        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var context = WebhookTestFactory.CreateContext(
            secret: "expected", invoiceId: "inv-ts-old", eventType: "InvoiceSettled",
            deliveryId: "del-ts-old", timestamp: oldTimestamp);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);

        var payment = await db.Payments.FirstAsync(p => p.PaymentId == "inv-ts-old");
        Assert.False(payment.IsPaid, "Payment should NOT be confirmed when timestamp is too old.");
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenTimestampIsTooFarInFuture()
    {
        using var db = TestDataFactory.CreateDbContext();
        TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-ts-future", address: "tb1qaddress");
        var service = CreateService(db, webhookSecret: "expected");

        var futureTimestamp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var context = WebhookTestFactory.CreateContext(
            secret: "expected", invoiceId: "inv-ts-future", eventType: "InvoiceSettled",
            deliveryId: "del-ts-future", timestamp: futureTimestamp);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);

        var payment = await db.Payments.FirstAsync(p => p.PaymentId == "inv-ts-future");
        Assert.False(payment.IsPaid, "Payment should NOT be confirmed when timestamp is too far in future.");
    }

    [Fact]
    public async Task HandleAsync_RejectsSecondDeliveryWithSameDeliveryId()
    {
        using var db = TestDataFactory.CreateDbContext();
        TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-dedup", address: "tb1qaddress", buyerId: "buyer-1");
        var service = CreateService(db, webhookSecret: "expected");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var first = WebhookTestFactory.CreateContext(
            secret: "expected", invoiceId: "inv-dedup", eventType: "InvoiceSettled",
            deliveryId: "del-unique-dedup", timestamp: now);
        var second = WebhookTestFactory.CreateContext(
            secret: "expected", invoiceId: "inv-dedup", eventType: "InvoiceSettled",
            deliveryId: "del-unique-dedup", timestamp: now);

        await service.HandleAsync(first);
        var result = await service.HandleAsync(second);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ProcessesWebhook_WhenTimestampIsRecentAndDeliveryIdIsNew()
    {
        using var db = TestDataFactory.CreateDbContext();
        TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-replay-ok", address: "tb1qaddress", buyerId: "buyer-1");
        var service = CreateService(db, webhookSecret: "expected");

        var recentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var context = WebhookTestFactory.CreateContext(
            secret: "expected", invoiceId: "inv-replay-ok", eventType: "InvoiceSettled",
            deliveryId: "del-replay-ok-unique", timestamp: recentTimestamp);

        var result = await service.HandleAsync(context);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);

        var payment = await db.Payments.FirstAsync(p => p.PaymentId == "inv-replay-ok");
        Assert.True(payment.IsPaid, "Payment should be confirmed when timestamp is recent and deliveryId is new.");
    }

    private static BtcPayWebhookService CreateService(
        AppDbContext db,
        string webhookSecret,
        int? webhookMaxBodyBytes = null,
        string? webhookMaxBodyBytesRaw = null,
        IHubContext<PaymentHub>? hubContext = null,
        PaymentEventBus? eventBus = null)
    {
        var configEntries = new Dictionary<string, string?>
        {
            ["BtcPay:WebhookSecret"] = webhookSecret
        };

        if (webhookMaxBodyBytes.HasValue)
        {
            configEntries["BtcPay:WebhookMaxBodyBytes"] = webhookMaxBodyBytes.Value.ToString();
        }

        if (webhookMaxBodyBytesRaw != null)
        {
            configEntries["BtcPay:WebhookMaxBodyBytes"] = webhookMaxBodyBytesRaw;
        }

        var config = TestConfigurationFactory.Create(configEntries);

        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"btcpay-webhook-{Guid.NewGuid()}");
        var logService = new LogService(dbFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.Core.LogService>.Instance);

        hubContext ??= SignalRTestFactory.CreateHubContext();
        eventBus ??= new PaymentEventBus();
        return new BtcPayWebhookService(db, logService, config, hubContext, eventBus);
    }

}

