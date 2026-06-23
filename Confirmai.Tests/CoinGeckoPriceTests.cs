using Confirmai.Models;

namespace Confirmai.Tests;

public class CoinGeckoPriceTests
{
    [Fact]
    public void Brl_CanBeSetAndGet()
    {
        var price = new CoinGeckoPrice { Brl = 150000m };
        Assert.Equal(150000m, price.Brl);
    }

    [Fact]
    public void Usd_CanBeSetAndGet()
    {
        var price = new CoinGeckoPrice { Usd = 30000m };
        Assert.Equal(30000m, price.Usd);
    }

    [Fact]
    public void Brl_DefaultsToZero()
    {
        var price = new CoinGeckoPrice();
        Assert.Equal(0m, price.Brl);
    }

    [Fact]
    public void Usd_DefaultsToZero()
    {
        var price = new CoinGeckoPrice();
        Assert.Equal(0m, price.Usd);
    }

    [Fact]
    public void CanSetBothProperties()
    {
        var price = new CoinGeckoPrice { Brl = 280000m, Usd = 52000m };
        Assert.Equal(280000m, price.Brl);
        Assert.Equal(52000m, price.Usd);
    }
}
