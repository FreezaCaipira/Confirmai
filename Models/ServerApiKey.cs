using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Confirmai.Models;

[Table("ServerApiKeys")]
public class ServerApiKey
{
    public int Id { get; set; }

    public int ServerId { get; set; }

    [Required, MaxLength(64)]
    public string KeyHash { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string KeyPrefix { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Label { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
