using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Docs;

public partial class Integration
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    private async Task CopyCode(string elementId)
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText",
                await JS.InvokeAsync<string>("eval",
                    $"document.getElementById('{elementId}').innerText"));
        }
        catch
        {
            // clipboard blocked — silent fail
        }
    }
}
