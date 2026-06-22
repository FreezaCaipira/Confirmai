using Confirmai.Services.Core;
using System.Net;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;

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
    public async Task CheckAndMarkPaymentAsync_MarksAsPaid()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")));
        var factory = TestDbContextFactory.CreateInMemoryFactory($"testnet-bitcoin-{Guid.NewGuid()}");
        var log = new LogService(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.Core.LogService>.Instance);

        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 0.00003m, method: "Testnet", paymentId: "pay-testnet-1", address: "tb1qaddress");

        var result = await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-1");

        var persistedPayment = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Payments, p => p.Id == payment.Id);

        Assert.True(result);
        Assert.True(persistedPayment.IsPaid);
        Assert.NotNull(persistedPayment.PaidAt);
    }

    [Fact]
    public async Task CheckAndMarkPaymentAsync_WhenAlreadyPaid_ReturnsTrue()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new TestnetBitcoinPaymentService(new StubHttpClientFactory(_ => HttpTestResponses.Json("{}")));
        var factory = TestDbContextFactory.CreateInMemoryFactory($"testnet-bitcoin-{Guid.NewGuid()}");
        var log = new LogService(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.Core.LogService>.Instance);

        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 0.00003m, method: "Testnet", paymentId: "pay-testnet-2", address: "tb1qaddress");

        var result = await service.CheckAndMarkPaymentAsync(db, log, "pay-testnet-2");

        Assert.True(result);
    }
}

