using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

public class DeliveryAgent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(180)]
    public string? Contact { get; set; }

    [Range(1, 60)]
    public int EstimatedBusinessDays { get; set; } = 2;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


