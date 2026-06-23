using Confirmai.Models;

namespace Confirmai.Tests;

public class BitcoinQuoteTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var btcBrl = 350000.50m;
        var btcUsd = 65000.75m;

        // Act
        var quote = new BitcoinQuote
        {
            btc_brl = btcBrl,
            btc_usd = btcUsd
        };

        // Assert
        Assert.Equal(btcBrl, quote.btc_brl);
        Assert.Equal(btcUsd, quote.btc_usd);
    }

    [Fact]
    public void Constructor_WithDefaultValues()
    {
        // Act
        var quote = new BitcoinQuote();

        // Assert
        Assert.Equal(0m, quote.btc_brl);
        Assert.Equal(0m, quote.btc_usd);
    }

    [Fact]
    public void Constructor_WithZeroValues()
    {
        // Arrange
        var btcBrl = 0m;
        var btcUsd = 0m;

        // Act
        var quote = new BitcoinQuote
        {
            btc_brl = btcBrl,
            btc_usd = btcUsd
        };

        // Assert
        Assert.Equal(0m, quote.btc_brl);
        Assert.Equal(0m, quote.btc_usd);
    }

    [Fact]
    public void Constructor_WithLargeValues()
    {
        // Arrange
        var btcBrl = 999999999.99m;
        var btcUsd = 999999999.99m;

        // Act
        var quote = new BitcoinQuote
        {
            btc_brl = btcBrl,
            btc_usd = btcUsd
        };

        // Assert
        Assert.Equal(999999999.99m, quote.btc_brl);
        Assert.Equal(999999999.99m, quote.btc_usd);
    }
}
