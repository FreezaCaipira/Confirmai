using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Groups.Components;

public partial class PixReceiverSelector
{
    [Parameter]
    public List<GroupMember> PixAdmins { get; set; } = new();

    [Parameter]
    public string SelectedPixReceiverId { get; set; } = string.Empty;

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public bool IsError { get; set; }

    [Parameter]
    public EventCallback<string> OnSavePixReceiver { get; set; }

    private string localSelectedId = string.Empty;

    protected override void OnParametersSet()
    {
        localSelectedId = SelectedPixReceiverId;
    }

    private async Task HandleSavePixReceiver()
        => await OnSavePixReceiver.InvokeAsync(localSelectedId);
}
