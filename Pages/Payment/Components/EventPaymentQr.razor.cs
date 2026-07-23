using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentQr
{
    [Parameter] public string? BrCode { get; set; }
    [Parameter] public bool Copied { get; set; }

    [Parameter] public EventCallback OnCopyBrCode { get; set; }

    private async Task HandleCopyBrCode()
        => await OnCopyBrCode.InvokeAsync();
}
