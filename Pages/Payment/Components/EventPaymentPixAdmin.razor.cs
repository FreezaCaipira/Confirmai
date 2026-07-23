using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentPixAdmin
{
    [Parameter] public string? GroupAdminPixKey { get; set; }
    [Parameter] public string? GroupName { get; set; }
    [Parameter] public string? GroupCity { get; set; }
    [Parameter] public decimal Price { get; set; }
    [Parameter] public bool CopiedAdminPix { get; set; }
    [Parameter] public string? PixQrPayload { get; set; }

    [Parameter] public EventCallback OnCopyAdminPixKey { get; set; }

    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    private async Task HandleCopyAdminPixKey()
        => await OnCopyAdminPixKey.InvokeAsync();
}
