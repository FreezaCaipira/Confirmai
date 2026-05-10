namespace Confirmai.Models
{
    public class CryptoQuote
    {
        public decimal PriceBrl { get; set; }
        public decimal PriceUsd { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}


