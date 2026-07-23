using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages;

public partial class About
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Navigation.NavigateTo("/servers", replace: true);
        }
    }
}
