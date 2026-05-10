using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Confirmai.Tests;

public class PaymentConfirmationServiceTests
{
    [Fact]
    public async Task ConfirmAsync_Returns_NotConfirmed_WhenPaymentDoesNotExist()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var result = await service.ConfirmAsync(new PaymentRecord { Id = 999 });

        Assert.False(result.Confirmed);
        Assert.False(result.AlreadyPaid);
        Assert.Equal(0m, result.ReceivedAmount);
    }

    [Fact]
    public async Task ConfirmAsync_Returns_AlreadyPaid_WhenPaymentIsAlreadyPaid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "Testnet", paymentId: "pay-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var result = await service.ConfirmAsync(payment);

        Assert.True(result.Confirmed);
        Assert.True(result.AlreadyPaid);
        Assert.Equal(payment.Amount, result.ReceivedAmount);
    }

    [Fact]
    public async Task ConfirmAsync_Returns_NotConfirmed_WhenReceivedIsLessThanExpected()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "Testnet", paymentId: "pay-1");
        var fakeGateway = new FakeBitcoinPaymentService("Testnet", 0.00002m);
        var service = CreateConfirmationService(db, fakeGateway);

        var result = await service.ConfirmAsync(payment);
        var persisted = await db.Payments.FirstAsync(p => p.Id == payment.Id);

        Assert.False(result.Confirmed);
        Assert.False(result.AlreadyPaid);
        Assert.Equal(0.00002m, result.ReceivedAmount);
        Assert.False(persisted.IsPaid);
        Assert.Equal(payment.Address, fakeGateway.LastParameter);
    }

    [Fact]
    public async Task ConfirmAsync_MarksAsPaid_WhenReceivedIsEnough()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "BTCPayServer", paymentId: "inv-123");
        var fakeGateway = new FakeBitcoinPaymentService("BTCPayServer", 0.00003m);
        var service = CreateConfirmationService(db, fakeGateway);

        var result = await service.ConfirmAsync(payment);
        var persisted = await db.Payments.FirstAsync(p => p.Id == payment.Id);

        Assert.True(result.Confirmed);
        Assert.False(result.AlreadyPaid);
        Assert.Equal(0.00003m, result.ReceivedAmount);
        Assert.True(persisted.IsPaid);
        Assert.NotNull(persisted.PaidAt);
        Assert.Equal("inv-123", fakeGateway.LastParameter);
    }

    [Fact]
    public async Task ConfirmAsync_WithTestnetService_CreatesOrder_WhenReceivedIsEnough()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "Testnet", paymentId: "testnet-1");

        var httpFactory = new StubHttpClientFactory(request =>
        {
            if (request.RequestUri?.Host == "api.blockcypher.com")
                return HttpTestResponses.Json("{\"total_received\":3000}");

            return HttpTestResponses.Json("{\"chain_stats\":{\"funded_txo_sum\":0,\"spent_txo_sum\":0}}");
        });

        var testnetService = new TestnetBitcoinPaymentService(httpFactory);
        var service = CreateConfirmationService(db, testnetService);

        var result = await service.ConfirmAsync(payment);
        var persistedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(result.Confirmed);
        Assert.False(result.AlreadyPaid);
        Assert.Equal(0.00003m, result.ReceivedAmount);
        Assert.True(persistedPayment.IsPaid);
        Assert.NotNull(order);
        Assert.Equal(order!.Id, persistedPayment.OrderId);
    }

    [Fact]
    public async Task ConfirmAsync_UsesTestnetAsDefault_WhenPaymentMethodIsNull()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: null, paymentId: "testnet-2");

        string? requestedUrl = null;
        var httpFactory = new StubHttpClientFactory(request =>
        {
            requestedUrl = request.RequestUri?.ToString();
            return HttpTestResponses.Json("{\"total_received\":3000}");
        });

        var testnetService = new TestnetBitcoinPaymentService(httpFactory);
        var service = CreateConfirmationService(db, testnetService);

        var result = await service.ConfirmAsync(payment);

        Assert.True(result.Confirmed);
        Assert.Contains(payment.Address, requestedUrl);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyPaidInTestnet_EnsuresOrderExists()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "Testnet", paymentId: "testnet-3");

        var httpFactory = new StubHttpClientFactory(_ => HttpTestResponses.Json("{}"));
        var testnetService = new TestnetBitcoinPaymentService(httpFactory);
        var service = CreateConfirmationService(db, testnetService);

        var result = await service.ConfirmAsync(payment);
        var persistedPayment = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(result.Confirmed);
        Assert.True(result.AlreadyPaid);
        Assert.NotNull(order);
        Assert.Equal(order!.Id, persistedPayment.OrderId);
    }

    private static PaymentConfirmationService CreateConfirmationService(AppDbContext db, IBitcoinPaymentService paymentService)
    {
        var factory = new BitcoinPaymentFactory(new[] { paymentService });
        var logService = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);
        var hubContext = SignalRTestFactory.CreateHubContext();
        var eventBus = new PaymentEventBus();
        return new PaymentConfirmationService(db, factory, logService, hubContext, eventBus);
    }

    private sealed class FakeBitcoinPaymentService : IBitcoinPaymentService
    {
        private readonly decimal _receivedAmount;

        public FakeBitcoinPaymentService(string name, decimal receivedAmount)
        {
            Name = name;
            _receivedAmount = receivedAmount;
        }

        public string Name { get; }
        public string? LastParameter { get; private set; }

        public Task<(string Address, string PaymentId)> GenerateAddressAsync(decimal amount, string? orderId = null)
        {
            throw new NotSupportedException();
        }

        public Task<(string Address, string PaymentId, string PrivateKey)> GenerateAddressWithKeyAsync(decimal amount, string? orderId = null)
        {
            throw new NotSupportedException();
        }

        public Task<decimal> GetReceivedAmountAsync(string address)
        {
            LastParameter = address;
            return Task.FromResult(_receivedAmount);
        }
    }

    // -- ConfirmPixReceiptAsync ---------------------------------------

    [Fact]
    public async Task ConfirmPixReceiptAsync_ReturnsError_WhenPaymentNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, error) = await service.ConfirmPixReceiptAsync(999, "seller-1");

        Assert.False(success);
        Assert.Equal("Pagamento não encontrado.", error);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_ReturnsError_WhenAlreadyPaid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: true, sellerId: "seller-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, error) = await service.ConfirmPixReceiptAsync(payment.Id, "seller-1");

        Assert.False(success);
        Assert.Equal("Este pagamento já foi confirmado.", error);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_ReturnsError_WhenNotAuthorized()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1", productOwnerId: "owner-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, error) = await service.ConfirmPixReceiptAsync(payment.Id, "random-user");

        Assert.False(success);
        Assert.Equal("Você não tem permissão para confirmar este pagamento.", error);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_AuthorizesSeller()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, _) = await service.ConfirmPixReceiptAsync(payment.Id, "seller-1");

        Assert.True(success);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_AuthorizesProductOwner()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1", productOwnerId: "owner-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, _) = await service.ConfirmPixReceiptAsync(payment.Id, "owner-1");

        Assert.True(success);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_MarksAsPaidAndCreatesOrder()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1", buyerId: "buyer-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, error) = await service.ConfirmPixReceiptAsync(payment.Id, "seller-1");
        var persisted = await db.Payments.FirstAsync(p => p.Id == payment.Id);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(success);
        Assert.Null(error);
        Assert.True(persisted.IsPaid);
        Assert.NotNull(persisted.PaidAt);
        Assert.NotNull(order);
        Assert.Equal(PaymentStatus.AguardandoEntrega, order!.Status);
        Assert.Equal("buyer-1", order.BuyerId);
        Assert.Equal("seller-1", order.SellerId);
        Assert.Equal(payment.Amount, order.Amount);
        Assert.Equal(order.Id, persisted.OrderId);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_CreatesOrderMessage()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1");
        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        await service.ConfirmPixReceiptAsync(payment.Id, "seller-1");

        var msg = await db.OrderMessages.FirstOrDefaultAsync();
        Assert.NotNull(msg);
        Assert.Equal("admin", msg!.UserRole);
        Assert.Contains("PIX", msg.Text);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_SkipsOrderCreation_WhenOrderAlreadyExists()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1", buyerId: "buyer-1");

        db.Orders.Add(new OrderModel
        {
            PaymentId = payment.Id,
            ProductId = payment.ProductId,
            BuyerId = "buyer-1",
            SellerId = "seller-1",
            Amount = payment.Amount,
            Status = PaymentStatus.AguardandoEntregaInGame
        });
        await db.SaveChangesAsync();

        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));
        var (success, _) = await service.ConfirmPixReceiptAsync(payment.Id, "seller-1");

        Assert.True(success);
        Assert.Equal(1, await db.Orders.CountAsync(o => o.PaymentId == payment.Id));
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_SendsSignalRNotification()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-1", buyerId: "buyer-1");

        var (hubContext, clientProxy) = SignalRTestFactory.CreateHubContextForUser("buyer-1");
        var factory = new BitcoinPaymentFactory(new IBitcoinPaymentService[] { new FakeBitcoinPaymentService("Testnet", 0m) });
        var logService = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);
        var eventBus = new PaymentEventBus();
        var service = new PaymentConfirmationService(db, factory, logService, hubContext, eventBus);

        await service.ConfirmPixReceiptAsync(payment.Id, "seller-1");

        clientProxy.Verify(
            x => x.SendCoreAsync("PaymentConfirmed", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_WhenParticipantDeleted_CreatesOrderInAguardandoRevisaoAdm()
    {
        using var db = TestDataFactory.CreateDbContext();
        // Simulate participantDeleted by nulling the buyer (UserId) on the payment after seeding
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-pixpd", buyerId: "buyer-pixpd");
        payment.UserId = null;
        await db.SaveChangesAsync();

        var service = CreateConfirmationService(db, new FakeBitcoinPaymentService("Testnet", 0m));

        var (success, _) = await service.ConfirmPixReceiptAsync(payment.Id, "seller-pixpd");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(PaymentStatus.AguardandoRevisaoAdm, order!.Status);
        Assert.Null(order.BuyerId);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_WhenParticipantDeleted_FiresOnOrderEnteredReviewEvent()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-pixpd-ev", buyerId: "buyer-pixpd-ev");
        payment.UserId = null;
        await db.SaveChangesAsync();

        var eventBus = new PaymentEventBus();
        int? firedOrderId = null;
        eventBus.OnOrderEnteredReview += id => firedOrderId = id;

        var factory = new BitcoinPaymentFactory(new IBitcoinPaymentService[] { new FakeBitcoinPaymentService("Testnet", 0m) });
        var logService = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);
        var service = new PaymentConfirmationService(db, factory, logService, SignalRTestFactory.CreateHubContext(), eventBus);

        await service.ConfirmPixReceiptAsync(payment.Id, "seller-pixpd-ev");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == payment.Id);
        Assert.NotNull(firedOrderId);
        Assert.Equal(order!.Id, firedOrderId);
    }

    [Fact]
    public async Task ConfirmPixReceiptAsync_WhenBothParticipantsPresent_DoesNotFireOnOrderEnteredReview()
    {
        using var db = TestDataFactory.CreateDbContext();
        var payment = SeedPixPayment(db, isPaid: false, sellerId: "seller-pixnopd", buyerId: "buyer-pixnopd");

        var eventBus = new PaymentEventBus();
        var fired = false;
        eventBus.OnOrderEnteredReview += _ => fired = true;

        var factory = new BitcoinPaymentFactory(new IBitcoinPaymentService[] { new FakeBitcoinPaymentService("Testnet", 0m) });
        var logService = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);
        var service = new PaymentConfirmationService(db, factory, logService, SignalRTestFactory.CreateHubContext(), eventBus);

        await service.ConfirmPixReceiptAsync(payment.Id, "seller-pixnopd");

        Assert.False(fired);
    }

    private static PaymentRecord SeedPixPayment(
        AppDbContext db,
        bool isPaid,
        string sellerId,
        string? productOwnerId = null,
        string buyerId = "buyer-1",
        decimal amount = 50m)
    {
        var product = new Product
        {
            Name = "Item PIX",
            Description = "d",
            Price = amount,
            UserId = productOwnerId ?? sellerId
        };
        db.Products.Add(product);
        db.SaveChanges();

        var payment = new PaymentRecord
        {
            ProductId = product.Id,
            Address = "pix-address",
            PaymentMethod = "PIX",
            Amount = amount,
            IsPaid = isPaid,
            UserId = buyerId,
            SellerId = sellerId
        };
        db.Payments.Add(payment);
        db.SaveChanges();
        return payment;
    }
}


