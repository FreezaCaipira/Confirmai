using Confirmai.Models;

namespace Confirmai.Tests;

public class BitcoinDataTests
{
    [Fact]
    public void Brl_CanBeSetAndGet()
    {
        var data = new BitcoinData { Brl = 100.50m };
        Assert.Equal(100.50m, data.Brl);
    }

    [Fact]
    public void Usd_CanBeSetAndGet()
    {
        var data = new BitcoinData { Usd = 50000.75m };
        Assert.Equal(50000.75m, data.Usd);
    }

    [Fact]
    public void Brl_DefaultsToZero()
    {
        var data = new BitcoinData();
        Assert.Equal(0m, data.Brl);
    }

    [Fact]
    public void Usd_DefaultsToZero()
    {
        var data = new BitcoinData();
        Assert.Equal(0m, data.Usd);
    }

    [Fact]
    public void CanSetBothProperties()
    {
        var data = new BitcoinData { Brl = 250000m, Usd = 45000m };
        Assert.Equal(250000m, data.Brl);
        Assert.Equal(45000m, data.Usd);
    }
}
