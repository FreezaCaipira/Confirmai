using Confirmai.Services.Payment;
using Confirmai.Services.Crypto;
using Confirmai.Services.Utility;
using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Confirmai.Tests;

public class PaymentInitializationServiceTests
{
    [Fact]
    public void ParseQueryParameters_ReturnsDefaultValues_WhenQueryIsEmpty()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var configuration = new ConfigurationBuilder().Build();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BitcoinQuoteService>.Instance;

        var bitcoinQuoteService = new BitcoinQuoteService(httpFactory, configuration, null, logger);
        var gatewayService = new GatewayService(dbFactory);
        var service = new PaymentInitializationService(dbFactory, bitcoinQuoteService, gatewayService);

        var uri = new Uri("https://example.com/payment");
        var result = service.ParseQueryParameters(uri);

        Assert.Equal(1, result.Quantity);
        Assert.Equal(1, result.MaxQuantity);
        Assert.Null(result.UnitPrice);
        Assert.Equal("BTC", result.Currency);
    }

    [Fact]
    public void ParseQueryParameters_ReturnsParsedValues_WhenQueryHasValidParameters()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var configuration = new ConfigurationBuilder().Build();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BitcoinQuoteService>.Instance;

        var bitcoinQuoteService = new BitcoinQuoteService(httpFactory, configuration, null, logger);
        var gatewayService = new GatewayService(dbFactory);
        var service = new PaymentInitializationService(dbFactory, bitcoinQuoteService, gatewayService);

        var uri = new Uri("https://example.com/payment?qty=5&maxQty=10&unitPrice=100.50&currency=BRL");
        var result = service.ParseQueryParameters(uri);

        Assert.Equal(5, result.Quantity);
        Assert.Equal(10, result.MaxQuantity);
        Assert.Equal(100.50m, result.UnitPrice);
        Assert.Equal("BRL", result.Currency);
    }

    [Fact]
    public void ParseQueryParameters_ReturnsDefaultCurrency_WhenCurrencyIsInvalid()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var configuration = new ConfigurationBuilder().Build();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BitcoinQuoteService>.Instance;

        var bitcoinQuoteService = new BitcoinQuoteService(httpFactory, configuration, null, logger);
        var gatewayService = new GatewayService(dbFactory);
        var service = new PaymentInitializationService(dbFactory, bitcoinQuoteService, gatewayService);

        var uri = new Uri("https://example.com/payment?currency=EUR");
        var result = service.ParseQueryParameters(uri);

        Assert.Equal("BTC", result.Currency);
    }

    [Fact]
    public void CalculateTotalAmount_ReturnsCorrectValue()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var configuration = new ConfigurationBuilder().Build();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BitcoinQuoteService>.Instance;

        var bitcoinQuoteService = new BitcoinQuoteService(httpFactory, configuration, null, logger);
        var gatewayService = new GatewayService(dbFactory);
        var service = new PaymentInitializationService(dbFactory, bitcoinQuoteService, gatewayService);

        var result = service.CalculateTotalAmount(100m, 5);

        Assert.Equal(500m, result);
    }
}
