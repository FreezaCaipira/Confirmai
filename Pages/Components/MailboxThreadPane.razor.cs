using System.Globalization;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components;

public partial class MailboxThreadPane
{
    [Parameter]
    public ConversationContactView? SelectedContact { get; set; }

    [Parameter]
    public List<ConversationMessageView> ConversationMessages { get; set; } = new();

    [Parameter]
    public string ComposerBody { get; set; } = string.Empty;

    [Parameter]
    public string? ComposerInfoMessage { get; set; }

    [Parameter]
    public bool IsThreadLoading { get; set; }

    [Parameter]
    public bool IsSendingReply { get; set; }

    [Parameter]
    public ElementReference ThreadStreamRef { get; set; }

    [Parameter]
    public EventCallback<string> OnSendReply { get; set; }

    [Inject] private UiTextService T { get; set; } = default!;

    private async Task HandleSendReply()
    {
        await OnSendReply.InvokeAsync(ComposerBody);
    }
}
