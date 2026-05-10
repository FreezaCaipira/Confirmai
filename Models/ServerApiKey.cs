using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    public class ServerApiKey
    {
        public int Id { get; set; }

        public int ServerId { get; set; }
        public TibiaServer Server { get; set; } = null!;

        [Required]
        [StringLength(64)]
        public string KeyHash { get; set; } = string.Empty;

        [Required]
        [StringLength(16)]
        public string KeyPrefix { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Label { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
    }
}
