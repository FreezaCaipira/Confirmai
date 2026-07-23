using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class EscalacaoShare
{
    [Parameter]
    public bool Copied { get; set; }

    [Parameter]
    public EventCallback OnCopyToClipboard { get; set; }

    [Parameter]
    public EventCallback OnShareOnWhatsApp { get; set; }

    private async Task HandleCopyToClipboard()
        => await OnCopyToClipboard.InvokeAsync();

    private async Task HandleShareOnWhatsApp()
        => await OnShareOnWhatsApp.InvokeAsync();
}
