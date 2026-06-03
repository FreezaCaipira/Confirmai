namespace Confirmai.Pages.Components;

public sealed class ConversationContactView
{
    public string ConversationKey { get; init; } = string.Empty;
    public string ConversationTitle { get; init; } = string.Empty;
    public string ContactUserId { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string LastBody { get; init; } = string.Empty;
    public DateTime LastCreatedAt { get; init; }
    public int UnreadCount { get; init; }
    public bool HasUnread { get; init; }
}

public sealed class ConversationMessageView
{
    public int Id { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? SenderUserId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string RoleClass { get; set; } = string.Empty;
    public bool IsIncoming { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
