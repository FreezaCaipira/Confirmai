using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Product;

public partial class Products
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Navigation.NavigateTo("/servers", replace: true);
        }
    }
}
