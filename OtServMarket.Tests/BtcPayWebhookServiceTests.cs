using Confirmai.Data;
using Confirmai.Hubs;
using Confirmai.Models;
using Confirmai.Services;
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
        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
        var conversationMessages = await db.OrderMessages
            .Where(m => m.OrderId == order!.Id)
            .ToListAsync();

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.True(persisted.IsPaid);
        Assert.NotNull(persisted.PaidAt);
        Assert.NotNull(order);
        Assert.Equal(order!.Id, persisted.OrderId);
        Assert.Single(conversationMessages);
        Assert.Equal("admin", conversationMessages[0].UserRole);
        Assert.Equal($"Conversa do pedido {order.Id} iniciada entre comprador e vendedor. Admin acompanha este chat.", conversationMessages[0].Text);

        clientProxy.Verify(
            p => p.SendCoreAsync(
                "PaymentConfirmed",
                It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], "inv-3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenPaymentAlreadyPaidAndOrderMissing_CreatesOrderAndLinksPayment()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-4", address: "tb1qaddress", buyerId: "buyer-1");

        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-4", eventType: "InvoiceSettled");

        var result = await service.HandleAsync(context);
        var persistedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
        var conversationMessages = await db.OrderMessages
            .Where(m => m.OrderId == order!.Id)
            .ToListAsync();

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.NotNull(order);
        Assert.Equal(order!.Id, persistedPayment.OrderId);
        Assert.Single(conversationMessages);
        Assert.Equal("admin", conversationMessages[0].UserRole);
        Assert.Equal($"Conversa do pedido {order.Id} iniciada entre comprador e vendedor. Admin acompanha este chat.", conversationMessages[0].Text);
    }

    [Fact]
    public async Task HandleAsync_WhenPaymentAlreadyPaidAndOrderExistsButLinkMissing_RepairsPaymentOrderLink()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-5", address: "tb1qaddress", buyerId: "buyer-1");

        var order = new OrderModel
        {
            BuyerId = payment.UserId ?? string.Empty,
            SellerId = "seller-1",
            ProductId = payment.ProductId,
            Amount = payment.Amount,
            IsPaid = true,
            PaymentId = payment.Id,
            Status = Confirmai.Enums.PaymentStatus.AguardandoEntrega,
            CreatedAt = DateTime.UtcNow
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        payment.OrderId = null;
        db.Payments.Update(payment);
        await db.SaveChangesAsync();

        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-5", eventType: "InvoiceSettled");

        var result = await service.HandleAsync(context);
        var persistedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.Equal(order.Id, persistedPayment.OrderId);
    }

    [Fact]
    public async Task HandleAsync_WhenSameSettledPayloadIsReceivedTwice_KeepsSingleOrder()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-6", address: "tb1qaddress", buyerId: "buyer-1");

        var service = CreateService(db, webhookSecret: "expected");
        var firstContext = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-6", eventType: "InvoiceSettled");
        var secondContext = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-6", eventType: "InvoiceSettled");

        await service.HandleAsync(firstContext);
        await service.HandleAsync(secondContext);

        var persistedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var orders = await db.Orders.Where(o => o.PaymentId == payment.Id).ToListAsync();
        var conversationMessages = await db.OrderMessages
            .Where(m => m.OrderId == orders[0].Id)
            .ToListAsync();

        Assert.True(persistedPayment.IsPaid);
        Assert.Single(orders);
        Assert.Equal(orders[0].Id, persistedPayment.OrderId);
        Assert.Single(conversationMessages);
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

        var orders = await db.Orders.Where(o => o.PaymentId != null).ToListAsync();
        Assert.Single(orders);
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

    [Fact]
    public async Task HandleAsync_WhenParticipantDeleted_CreatesOrderInAguardandoRevisaoAdm()
    {
        using var db = TestDataFactory.CreateDbContext();
        // SeedPayment uses buyerId="buyer-1" but product.UserId = sellerId; to simulate participantDeleted
        // set buyerId to null after seeding (UserId on the payment record)
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer",
            paymentId: "inv-participant-deleted", address: "tb1qpd",
            buyerId: "buyer-pd", sellerId: "seller-pd");
        payment.UserId = null;
        await db.SaveChangesAsync();

        var service = CreateService(db, webhookSecret: "expected");
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-participant-deleted", eventType: "InvoiceSettled");

        var result = await service.HandleAsync(context);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.NotNull(order);
        Assert.Equal(Confirmai.Enums.PaymentStatus.AguardandoRevisaoAdm, order!.Status);
        Assert.Null(order.BuyerId);
    }

    [Fact]
    public async Task HandleAsync_WhenParticipantDeleted_FiresOnOrderEnteredReviewEvent()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer",
            paymentId: "inv-pd-event", address: "tb1qpdevent",
            buyerId: "buyer-pde", sellerId: "seller-pde");
        payment.UserId = null;
        await db.SaveChangesAsync();

        var eventBus = new PaymentEventBus();
        int? firedOrderId = null;
        eventBus.OnOrderEnteredReview += id => firedOrderId = id;

        var service = CreateService(db, webhookSecret: "expected", eventBus: eventBus);
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-pd-event", eventType: "InvoiceSettled");

        await service.HandleAsync(context);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
        Assert.NotNull(firedOrderId);
        Assert.Equal(order!.Id, firedOrderId);
    }

    [Fact]
    public async Task HandleAsync_WhenBothParticipantsPresent_DoesNotFireOnOrderEnteredReview()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer",
            paymentId: "inv-nopd-event", address: "tb1qnopdevent",
            buyerId: "buyer-nopde", sellerId: "seller-nopde");

        var eventBus = new PaymentEventBus();
        var fired = false;
        eventBus.OnOrderEnteredReview += _ => fired = true;

        var service = CreateService(db, webhookSecret: "expected", eventBus: eventBus);
        var context = WebhookTestFactory.CreateContext(secret: "expected", invoiceId: "inv-nopd-event", eventType: "InvoiceSettled");

        await service.HandleAsync(context);

        Assert.False(fired);
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

        var logService = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        hubContext ??= SignalRTestFactory.CreateHubContext();
        eventBus ??= new PaymentEventBus();
        return new BtcPayWebhookService(db, logService, config, hubContext, eventBus);
    }

}

