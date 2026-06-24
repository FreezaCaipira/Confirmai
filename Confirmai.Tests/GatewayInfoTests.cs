using Confirmai.Models;

namespace Confirmai.Tests;

public class GatewayInfoTests
{
    [Fact]
    public void Name_CanBeSetAndGet()
    {
        var info = new GatewayInfo { Name = "TestGateway" };
        Assert.Equal("TestGateway", info.Name);
    }

    [Fact]
    public void Name_DefaultsToEmptyString()
    {
        var info = new GatewayInfo();
        Assert.Equal("", info.Name);
    }

    [Fact]
    public void Enabled_CanBeSetAndGet()
    {
        var info = new GatewayInfo { Enabled = true };
        Assert.True(info.Enabled);
    }

    [Fact]
    public void Enabled_DefaultsToFalse()
    {
        var info = new GatewayInfo();
        Assert.False(info.Enabled);
    }

    [Fact]
    public void CanSetBothProperties()
    {
        var info = new GatewayInfo { Name = "PixGateway", Enabled = true };
        Assert.Equal("PixGateway", info.Name);
        Assert.True(info.Enabled);
    }
}
