using Confirmai.Services.Payment;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Interfaces;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class PaymentConfirmationServiceTests
{
    [Fact]
    public async Task ConfirmAsync_Returns_NotConfirmed_WhenPaymentDoesNotExist()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var payment = new PaymentRecord
        {
            Address = "no-exist",
            PaymentMethod = "Testnet",
            Amount = 0.00003m,
            IsPaid = false
        };

        var httpFactory = new StubHttpClientFactory(_ => HttpTestResponses.Json("{\"total_received\":0}"));
        var testnetService = new TestnetBitcoinPaymentService(httpFactory);
        var service = CreateConfirmationService(factory, testnetService);

        var result = await service.ConfirmAsync(payment);

        Assert.False(result.Confirmed);
        Assert.False(result.AlreadyPaid);
    }

    [Fact]
    public async Task ConfirmAsync_ConfirmsPayment_WhenBlockchainReturnsEnoughFunds()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "Testnet", paymentId: "testnet-1");

        var httpFactory = new StubHttpClientFactory(request =>
        {
            if (request.RequestUri?.Host == "api.blockcypher.com")
                return HttpTestResponses.Json("{\"total_received\":3000}");

            return HttpTestResponses.Json("{\"chain_stats\":{\"funded_txo_sum\":0,\"spent_txo_sum\":0}}");
        });

        var testnetService = new TestnetBitcoinPaymentService(httpFactory);
        var service = CreateConfirmationService(factory, testnetService);

        var result = await service.ConfirmAsync(payment);
        await using var verifyDb = factory.CreateDbContext();
        var persistedPayment = await verifyDb.Payments.FirstAsync(p => p.Id == payment.Id);

        Assert.True(result.Confirmed);
        Assert.False(result.AlreadyPaid);
        Assert.Equal(0.00003m, result.ReceivedAmount);
        Assert.True(persistedPayment.IsPaid);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyPaidInTestnet_ReturnsAlreadyPaid()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "Testnet", paymentId: "testnet-3");

        var httpFactory = new StubHttpClientFactory(_ => HttpTestResponses.Json("{}"));
        var testnetService = new TestnetBitcoinPaymentService(httpFactory);
        var service = CreateConfirmationService(factory, testnetService);

        var result = await service.ConfirmAsync(payment);

        Assert.True(result.Confirmed);
        Assert.True(result.AlreadyPaid);
    }

    private static PaymentConfirmationService CreateConfirmationService(IDbContextFactory<AppDbContext> factory, IBitcoinPaymentService paymentService)
    {
        var paymentFactory = new BitcoinPaymentFactory(new[] { paymentService });
        var logService = new LogService(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.Core.LogService>.Instance);
        var hubContext = SignalRTestFactory.CreateHubContext();
        var eventBus = new PaymentEventBus();
        return new PaymentConfirmationService(factory, paymentFactory, logService, hubContext, eventBus);
    }
}
