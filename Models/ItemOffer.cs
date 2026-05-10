using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

public class ItemOffer
{
    public int Id { get; set; }

    public int ServerId { get; set; }
    public TibiaServer? Server { get; set; }

    [Required]
    [StringLength(120)]
    public string ItemKey { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string ItemName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.00001", "79228162514264337593543950335")]
    public decimal UnitPrice { get; set; }

    [StringLength(64)]
    public string? PricingGateway { get; set; }

    public bool UseSiteIntermediary { get; set; }

    [Required]
    public string SellerUserId { get; set; } = string.Empty;
    public ApplicationUser? SellerUser { get; set; }

    [StringLength(9)]
    public string? AccentColor { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the last time the Lua integration script confirmed this seller
    /// has the item in their depot during a poll cycle. Null means never verified.
    /// </summary>
    public DateTime? InventoryLastVerifiedAt { get; set; }

    /// <summary>
    /// Quantity the Lua confirmed in the seller's depot at InventoryLastVerifiedAt.
    /// </summary>
    public int? InventoryVerifiedQty { get; set; }
}
