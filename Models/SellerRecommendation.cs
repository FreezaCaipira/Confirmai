using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

public class SellerRecommendation
{
    public int Id { get; set; }

    public int ServerId { get; set; }
    public TibiaServer? Server { get; set; }

    [Required]
    [StringLength(120)]
    public string ItemKey { get; set; } = string.Empty;

    [Required]
    public string SellerUserId { get; set; } = string.Empty;
    public ApplicationUser? SellerUser { get; set; }

    [Required]
    public string RecommenderUserId { get; set; } = string.Empty;
    public ApplicationUser? RecommenderUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
