namespace Confirmai.Models
{
    public class ProductServer
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int ServerId { get; set; }
        public TibiaServer Server { get; set; } = null!;
    }
}
