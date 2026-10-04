using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Groups.Components;

/// <summary>C31 — links a WhatsApp group JID (...@g.us) to this group.</summary>
public partial class WhatsAppGroupJidEditor
{
    [Parameter]
    public string? CurrentJid { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public bool IsError { get; set; }

    [Parameter]
    public EventCallback<string?> OnSaveJid { get; set; }

    private string? localJid;

    protected override void OnParametersSet()
    {
        localJid = CurrentJid;
    }

    private async Task HandleSave()
        => await OnSaveJid.InvokeAsync(localJid);
}
