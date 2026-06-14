using Confirmai.Services;

namespace Confirmai.Tests;

public class BitcoinPaymentFactoryTests
{
    private sealed class FakeBitcoinService : IBitcoinPaymentService
    {
        public string Name { get; init; } = "";

        public Task<(string Address, string PaymentId)> GenerateAddressAsync(decimal amount, string? orderId = null)
            => Task.FromResult(("addr", "pid"));

        public Task<(string Address, string PaymentId, string PrivateKey)> GenerateAddressWithKeyAsync(decimal amount, string? orderId = null)
            => Task.FromResult(("addr", "pid", "key"));

        public Task<decimal> GetReceivedAmountAsync(string address) => Task.FromResult(0m);
    }

    [Fact]
    public void GetService_ReturnsMatchingService()
    {
        var testnet = new FakeBitcoinService { Name = "Testnet" };
        var btcPay = new FakeBitcoinService { Name = "BTCPayServer" };
        var factory = new BitcoinPaymentFactory(new IBitcoinPaymentService[] { testnet, btcPay });

        var result = factory.GetService("BTCPayServer");

        Assert.Same(btcPay, result);
    }

    [Fact]
    public void GetService_ReturnsFirst_WhenNameNotFound()
    {
        var testnet = new FakeBitcoinService { Name = "Testnet" };
        var factory = new BitcoinPaymentFactory(new[] { testnet });

        var result = factory.GetService("NonExistent");

        Assert.Same(testnet, result);
    }

    [Fact]
    public void GetService_IsCaseSensitive()
    {
        var testnet = new FakeBitcoinService { Name = "Testnet" };
        var factory = new BitcoinPaymentFactory(new[] { testnet });

        // "testnet" != "Testnet" → falls back to first
        var result = factory.GetService("testnet");

        Assert.Same(testnet, result);
    }

    [Fact]
    public void GetAvailableMethods_ReturnsAllServiceNames()
    {
        var a = new FakeBitcoinService { Name = "Testnet" };
        var b = new FakeBitcoinService { Name = "BTCPayServer" };
        var factory = new BitcoinPaymentFactory(new IBitcoinPaymentService[] { a, b });

        var methods = factory.GetAvailableMethods().ToList();

        Assert.Equal(2, methods.Count);
        Assert.Contains("Testnet", methods);
        Assert.Contains("BTCPayServer", methods);
    }

    [Fact]
    public void GetAvailableMethods_ReturnsEmpty_WhenNoServices()
    {
        var factory = new BitcoinPaymentFactory(Array.Empty<IBitcoinPaymentService>());

        var methods = factory.GetAvailableMethods().ToList();

        Assert.Empty(methods);
    }

    [Fact]
    public void GetService_ThrowsInvalidOperation_WhenNoServicesAndNameNotFound()
    {
        var factory = new BitcoinPaymentFactory(Array.Empty<IBitcoinPaymentService>());

        Assert.Throws<InvalidOperationException>(() => factory.GetService("anything"));
    }
}
