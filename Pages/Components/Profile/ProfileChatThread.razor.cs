using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileChatThread
{
    [Parameter]
    public List<ProfileChatMessageView> ChatMessages { get; set; } = new();

    public sealed class ProfileChatMessageView
    {
        public string Body { get; init; } = string.Empty;
        public bool IsIncoming { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
