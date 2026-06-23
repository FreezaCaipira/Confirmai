using Confirmai.Models;

namespace Confirmai.Tests;

public class CoinGeckoResponseTests
{
    [Fact]
    public void Bitcoin_CanBeSetAndGet()
    {
        var response = new CoinGeckoResponse
        {
            bitcoin = new BitcoinData { Brl = 200000m, Usd = 40000m }
        };
        
        Assert.NotNull(response.bitcoin);
        Assert.Equal(200000m, response.bitcoin.Brl);
        Assert.Equal(40000m, response.bitcoin.Usd);
    }

    [Fact]
    public void Bitcoin_DefaultsToNull()
    {
        var response = new CoinGeckoResponse();
        Assert.Null(response.bitcoin);
    }

    [Fact]
    public void CanSetBitcoinToNull()
    {
        var response = new CoinGeckoResponse { bitcoin = new BitcoinData() };
        response.bitcoin = null;
        Assert.Null(response.bitcoin);
    }
}
