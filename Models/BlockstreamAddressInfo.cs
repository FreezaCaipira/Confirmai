public class BlockstreamAddressInfo
{
    public ChainStats? chain_stats { get; set; }
    public class ChainStats
    {
        public long funded_txo_sum { get; set; }
    }
}