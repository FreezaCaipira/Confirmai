using Confirmai.Models;

namespace Confirmai.Tests;

public class CryptoQuoteTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var priceBrl = 350000.50m;
        var priceUsd = 65000.75m;
        var symbol = "BTC";
        var name = "Bitcoin";

        // Act
        var quote = new CryptoQuote
        {
            PriceBrl = priceBrl,
            PriceUsd = priceUsd,
            Symbol = symbol,
            Name = name
        };

        // Assert
        Assert.Equal(priceBrl, quote.PriceBrl);
        Assert.Equal(priceUsd, quote.PriceUsd);
        Assert.Equal(symbol, quote.Symbol);
        Assert.Equal(name, quote.Name);
    }

    [Fact]
    public void Constructor_WithDefaultValues()
    {
        // Act
        var quote = new CryptoQuote();

        // Assert
        Assert.Equal(0m, quote.PriceBrl);
        Assert.Equal(0m, quote.PriceUsd);
        Assert.Equal(string.Empty, quote.Symbol);
        Assert.Equal(string.Empty, quote.Name);
    }

    [Fact]
    public void Constructor_WithZeroPrices()
    {
        // Arrange
        var priceBrl = 0m;
        var priceUsd = 0m;
        var symbol = "ETH";
        var name = "Ethereum";

        // Act
        var quote = new CryptoQuote
        {
            PriceBrl = priceBrl,
            PriceUsd = priceUsd,
            Symbol = symbol,
            Name = name
        };

        // Assert
        Assert.Equal(0m, quote.PriceBrl);
        Assert.Equal(0m, quote.PriceUsd);
        Assert.Equal("ETH", quote.Symbol);
        Assert.Equal("Ethereum", quote.Name);
    }

    [Fact]
    public void Constructor_WithLargeValues()
    {
        // Arrange
        var priceBrl = 999999999.99m;
        var priceUsd = 999999999.99m;
        var symbol = "MAX";
        var name = "MaxCoin";

        // Act
        var quote = new CryptoQuote
        {
            PriceBrl = priceBrl,
            PriceUsd = priceUsd,
            Symbol = symbol,
            Name = name
        };

        // Assert
        Assert.Equal(999999999.99m, quote.PriceBrl);
        Assert.Equal(999999999.99m, quote.PriceUsd);
        Assert.Equal("MAX", quote.Symbol);
        Assert.Equal("MaxCoin", quote.Name);
    }
}
