using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Services.Utility;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;

namespace Confirmai.Pages.Product;

public partial class ProductForm
{
    [Parameter] public int Id { get; set; }
    private Confirmai.Models.Product product = new();
    private IBrowserFile? imageFile;
    [CascadingParameter] public Toast? ToastRef { get; set; }
    private bool isLoading = false;
    private string accentColorInput = "#BF4B13";
    private List<GatewayInfo> pricingGatewayOptions = new();
    private readonly List<CategoryOption> categoryOptions =
    [
        new("helmet", "Helmet"),
        new("armor", "Armor"),
        new("legs", "Legs"),
        new("boots", "Boots"),
        new("shield", "Shield"),
        new("necklace", "Necklace"),
        new("ring", "Ring"),
        new("axe", "Axe"),
        new("club", "Club"),
        new("sword", "Sword"),
        new("distance", "Distance"),
        new("ammunition", "Ammunition"),
        new("rune", "Rune"),
        new("valuable", "Valuable"),
        new("tool", "Tool")
    ];

    [Inject] private ProductService ProductService { get; set; } = default!;
    [Inject] private GatewayService GatewayService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        pricingGatewayOptions = await GatewayService.GetAllAsync();

        if (Id != 0)
        {
            isLoading = true;
            var existing = await ProductService.GetByIdAsync(Id);
            if (existing != null)
            {
                product = existing;
            }

            accentColorInput = string.IsNullOrWhiteSpace(product.AccentColor)
                ? "#BF4B13"
                : product.AccentColor!;
            EnsurePricingGatewayDefault();
            isLoading = false;
        }
        else
        {
            product.AccentColor = "#BF4B13";
            accentColorInput = product.AccentColor;
            ApplyCreateQueryDefaults();
            EnsurePricingGatewayDefault();
        }
    }

    private void EnsurePricingGatewayDefault()
    {
        if (!pricingGatewayOptions.Any())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(product.PricingGateway))
        {
            product.PricingGateway = pricingGatewayOptions
                .Select(g => g.Name)
                .FirstOrDefault(name => string.Equals(name, "Pix", StringComparison.OrdinalIgnoreCase))
                ?? pricingGatewayOptions[0].Name;
            return;
        }

        if (pricingGatewayOptions.Any(g => string.Equals(g.Name, product.PricingGateway, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        pricingGatewayOptions.Insert(0, new GatewayInfo
        {
            Name = product.PricingGateway,
            Enabled = false
        });
    }

    private void ApplyCreateQueryDefaults()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("itemName", out var itemNameValues))
        {
            var itemName = itemNameValues.LastOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                product.Name = itemName;
            }
        }

        if (query.TryGetValue("category", out var categoryValues))
        {
            var category = categoryValues.LastOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(category)
                && categoryOptions.Any(c => string.Equals(c.Value, category, StringComparison.OrdinalIgnoreCase)))
            {
                product.Category = category.ToLowerInvariant();
            }
        }

        if (query.TryGetValue("accentColor", out var accentValues))
        {
            var accent = accentValues.LastOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(accent))
            {
                accentColorInput = accent.StartsWith('#') ? accent : $"#{accent}";
            }
        }
    }

    private async Task HandleValidSubmit()
    {
        isLoading = true;
        product.AccentColor = accentColorInput;
        if (Id == 0)
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            product.UserId = user.FindFirst(c => c.Type == "sub" || c.Type.Contains("nameidentifier"))?.Value ?? "";

            await ProductService.AddAsync(product, imageFile);
            await LogService.LogAsync(
                string.Format(T["ProductForm.LogCreated"], product.Name, product.Id),
                source: "Product",
                level: "Info",
                userId: product.UserId
            );
            ToastRef?.Show(T["ProductForm.CreatedToast"], "success");
        }
        else
        {
            await ProductService.UpdateAsync(product, imageFile);
            await LogService.LogAsync(
                string.Format(T["ProductForm.LogUpdated"], product.Name, product.Id),
                source: "Product",
                level: "Info",
                userId: product.UserId
            );
            ToastRef?.Show(T["ProductForm.UpdatedToast"], "success");
        }

        isLoading = false;

        Navigation.NavigateTo("/products");
    }

    private void OnInputFileChange(InputFileChangeEventArgs e)
    {
        imageFile = e.File;
    }

    private sealed record CategoryOption(string Value, string Label);
}
