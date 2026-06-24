using Confirmai.Models;

namespace Confirmai.Tests;

public class BlockstreamAddressInfoTests
{
    [Fact]
    public void ChainStats_CanBeSetAndGet()
    {
        var info = new BlockstreamAddressInfo
        {
            chain_stats = new BlockstreamAddressInfo.ChainStats
            {
                funded_txo_sum = 100000
            }
        };
        
        Assert.NotNull(info.chain_stats);
        Assert.Equal(100000, info.chain_stats.funded_txo_sum);
    }

    [Fact]
    public void ChainStats_DefaultsToNull()
    {
        var info = new BlockstreamAddressInfo();
        Assert.Null(info.chain_stats);
    }

    [Fact]
    public void FundedTxoSum_CanBeSetAndGet()
    {
        var stats = new BlockstreamAddressInfo.ChainStats { funded_txo_sum = 50000 };
        Assert.Equal(50000, stats.funded_txo_sum);
    }

    [Fact]
    public void FundedTxoSum_DefaultsToZero()
    {
        var stats = new BlockstreamAddressInfo.ChainStats();
        Assert.Equal(0, stats.funded_txo_sum);
    }

    [Fact]
    public void CanSetNegativeFundedTxoSum()
    {
        var stats = new BlockstreamAddressInfo.ChainStats { funded_txo_sum = -1000 };
        Assert.Equal(-1000, stats.funded_txo_sum);
    }
}
