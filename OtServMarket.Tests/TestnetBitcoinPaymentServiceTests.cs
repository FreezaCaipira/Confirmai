using System.Net;
using Confirmai.Enums;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class TestnetBitcoinPaymentServiceTests
{
    [Fact]
    public async Task GetReceivedAmountAsync_ReturnsZero_WhenAddressIsEmpty()
    {
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP")));

        var received = await service.GetReceivedAmountAsync(" ");

        Assert.Equal(0m, received);
    }

    [Fact]
    public async Task GetReceivedAmountAsync_UsesBlockCypher_WhenValueIsPositive()
    {
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(request =>
        {
            if (request.RequestUri?.Host == "api.blockcypher.com")
            {
                return HttpTestResponses.Json("{\"total_received\":3000}");
            }

            throw new InvalidOperationException("Blockstream should not be called when BlockCypher already has value.");
        }));

        var received = await service.GetReceivedAmountAsync("tb1qtest");

        Assert.Equal(0.00003m, received);
    }

    [Fact]
    public async Task GetReceivedAmountAsync_FallsBackToBlockstream_WhenBlockCypherReturnsZero()
    {
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(request =>
        {
            if (request.RequestUri?.Host == "api.blockcypher.com")
            {
                return HttpTestResponses.Json("{\"total_received\":0}");
            }

            if (request.RequestUri?.Host == "blockstream.info")
            {
                return HttpTestResponses.Json("{\"chain_stats\":{\"funded_txo_sum\":7000,\"spent_txo_sum\":2000}}");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var received = await service.GetReceivedAmountAsync("tb1qtest");

        Assert.Equal(0.00005m, received);
    }

    [Fact]
    public async Task CheckAndMarkPaymentAsync_MarksAsPaid_AndCreatesOrder()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")));
        var log = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "Testnet", paymentId: "pay-testnet-1", address: "tb1qaddress");

        var result = await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-1");

        var persistedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var persistedOrder = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(result);
        Assert.True(persistedPayment.IsPaid);
        Assert.NotNull(persistedPayment.PaidAt);
        Assert.NotNull(persistedOrder);
        Assert.Equal(PaymentStatus.AguardandoEntrega, persistedOrder!.Status);
    }

    [Fact]
    public async Task CheckAndMarkPaymentAsync_WhenAlreadyPaid_CreatesMissingOrder()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")));
        var log = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "Testnet", paymentId: "pay-testnet-2", address: "tb1qaddress");

        var result = await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-2");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(result);
        Assert.NotNull(order);
        Assert.Equal(payment.UserId, order!.BuyerId);
    }

    [Fact]
    public async Task CheckAndMarkPaymentAsync_WhenParticipantDeleted_CreatesOrderInAguardandoRevisaoAdm()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")));
        var log = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        // Trigger participantDeleted by leaving UserId (buyer) null on the payment
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.001m, method: "Testnet",
            paymentId: "pay-testnet-participant-deleted", address: "tb1qparticipantdeleted",
            buyerId: "buyer-participant-deleted", sellerId: "seller-participant-deleted");
        payment.UserId = null;
        await db.SaveChangesAsync();

        var result = await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-participant-deleted");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(result);
        Assert.NotNull(order);
        Assert.Equal(Confirmai.Enums.PaymentStatus.AguardandoRevisaoAdm, order!.Status);
        Assert.Null(order.BuyerId);
    }

    [Fact]
    public async Task CheckAndMarkPaymentAsync_WhenParticipantDeleted_FiresOnOrderEnteredReviewEvent()
    {
        using var db = TestDataFactory.CreateDbContext();
        var eventBus = new Confirmai.Services.PaymentEventBus();
        var service = new TestnetBitcoinPaymentService(
            new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")),
            eventBus);
        var log = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        int? firedOrderId = null;
        eventBus.OnOrderEnteredReview += id => firedOrderId = id;

        // Trigger participantDeleted by leaving UserId (buyer) null
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.001m, method: "Testnet",
            paymentId: "pay-testnet-event", address: "tb1qeventtest",
            buyerId: "buyer-event", sellerId: "seller-event");
        payment.UserId = null;
        await db.SaveChangesAsync();

        await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-event");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
        Assert.NotNull(firedOrderId);
        Assert.Equal(order!.Id, firedOrderId);
    }

    [Fact]
    public async Task CheckAndMarkPaymentAsync_WhenBothParticipantsPresent_DoesNotFireOnOrderEnteredReview()
    {
        using var db = TestDataFactory.CreateDbContext();
        var eventBus = new Confirmai.Services.PaymentEventBus();
        var service = new TestnetBitcoinPaymentService(
            new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")),
            eventBus);
        var log = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        var fired = false;
        eventBus.OnOrderEnteredReview += _ => fired = true;

        var payment = TestDataFactory.SeedPayment(
            db, isPaid: false, amount: 0.001m, method: "Testnet",
            paymentId: "pay-testnet-noevent", address: "tb1qnoevent",
            buyerId: "buyer-noevent", sellerId: "seller-noevent");

        await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-noevent");

        Assert.False(fired);
    }
}

