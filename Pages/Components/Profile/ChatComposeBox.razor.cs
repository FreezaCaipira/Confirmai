using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ChatComposeBox
{
    [Parameter]
    public string MessageBody { get; set; } = string.Empty;

    [Parameter]
    public bool IsSending { get; set; }

    [Parameter]
    public string? Feedback { get; set; }

    [Parameter]
    public bool ShowRefreshButton { get; set; }

    [Parameter]
    public EventCallback<string> OnSendMessage { get; set; }

    [Parameter]
    public EventCallback OnRefreshChat { get; set; }

    private string localMessageBody = string.Empty;

    protected override void OnParametersSet()
    {
        localMessageBody = MessageBody;
    }

    private async Task HandleSendMessage()
    {
        await OnSendMessage.InvokeAsync(localMessageBody);
        localMessageBody = string.Empty;
    }

    private async Task HandleRefreshChat()
        => await OnRefreshChat.InvokeAsync();
}
