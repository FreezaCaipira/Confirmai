using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    public class GatewayInfo
    {
        [Key]
        public string Name { get; set; } = "";
        public bool Enabled { get; set; }
    }
}

