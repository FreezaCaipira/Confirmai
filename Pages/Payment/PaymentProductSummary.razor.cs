using Confirmai.Models;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Payment;

public partial class PaymentProductSummary
{
    [Parameter]
    public Confirmai.Models.Product? Product { get; set; }

    [Parameter]
    public ApplicationUser? SellerUser { get; set; }

    [Parameter]
    public int SelectedQuantity { get; set; } = 1;

    [Parameter]
    public decimal UnitPrice { get; set; }

    [Parameter]
    public decimal TotalAmount { get; set; }

    [Parameter]
    public string Currency { get; set; } = "BTC";

    [Parameter]
    public decimal? BtcUsdRate { get; set; }

    [Parameter]
    public decimal? BtcBrlRate { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private string FormatOfferPrice(decimal amount)
    {
        return Currency switch
        {
            "BRL" => $"R$ {amount:N2}",
            "USD" => $"$ {amount:N2}",
            _ => BtcUsdFormatter.Format(amount, BtcUsdRate, BtcBrlRate, "BRL")
        };
    }

    private decimal GetUnitPriceAmount()
    {
        return UnitPrice > 0 ? UnitPrice : Product?.Price ?? 0m;
    }

    private string BuildSellerProfileUrl(string sellerUserId)
    {
        var safeUserId = Uri.EscapeDataString(sellerUserId ?? string.Empty);
        var currentRelativePath = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var profileUrl = $"/profile/{safeUserId}?returnUrl={Uri.EscapeDataString(currentRelativePath)}";
        return profileUrl;
    }
}
