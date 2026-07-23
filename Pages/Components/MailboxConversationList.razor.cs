using System.Globalization;
using Confirmai.Data;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components;

public partial class MailboxConversationList
{
    [Parameter]
    public List<ConversationContactView> Conversations { get; set; } = new();

    [Parameter]
    public string? SelectedConversationKey { get; set; }

    [Parameter]
    public int CurrentPage { get; set; } = 1;

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public int ConversationTotal { get; set; }

    [Parameter]
    public int ActiveConversationTotal { get; set; }

    [Parameter]
    public int ArchivedConversationTotal { get; set; }

    [Parameter]
    public bool ShowArchived { get; set; }

    [Parameter]
    public EventCallback<string> OnSelectConversation { get; set; }

    [Parameter]
    public EventCallback<string> OnMarkAsRead { get; set; }

    [Parameter]
    public EventCallback<string> OnArchive { get; set; }

    [Parameter]
    public EventCallback<bool> OnSwitchFolder { get; set; }

    [Parameter]
    public EventCallback OnPreviousPage { get; set; }

    [Parameter]
    public EventCallback OnNextPage { get; set; }

    [Inject] private UiTextService T { get; set; } = default!;

    private async Task HandleSelectConversation(string conversationKey)
    {
        await OnSelectConversation.InvokeAsync(conversationKey);
    }

    private async Task HandleMarkAsRead(string conversationKey)
    {
        await OnMarkAsRead.InvokeAsync(conversationKey);
    }

    private async Task HandleArchive(string conversationKey)
    {
        await OnArchive.InvokeAsync(conversationKey);
    }

    private async Task HandleSwitchFolder(bool archived)
    {
        await OnSwitchFolder.InvokeAsync(archived);
    }

    private async Task HandlePreviousPage()
    {
        await OnPreviousPage.InvokeAsync();
    }

    private async Task HandleNextPage()
    {
        await OnNextPage.InvokeAsync();
    }

    private string BuildSnippet(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return T["Mailbox.NoContent"];
        }

        var trimmed = body.Trim();
        return trimmed.Length <= 96
            ? trimmed
            : $"{trimmed[..96]}...";
    }

    private string BuildAvatarInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0][..1].ToUpperInvariant();
        }

        return string.Concat(parts[0][0], parts[^1][0]).ToUpperInvariant();
    }

    private string FormatRelativeTime(DateTime utcDate)
    {
        var now = DateTime.UtcNow;
        var delta = now - utcDate;
        if (delta.TotalMinutes < 1)
        {
            return T["Mailbox.Now"];
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{Math.Max(1, (int)Math.Floor(delta.TotalMinutes))}m";
        }

        var localDate = utcDate.ToLocalTime();
        var today = DateTime.Now.Date;
        if (localDate.Date == today)
        {
            return $"{localDate:HH:mm}";
        }

        if (localDate.Date == today.AddDays(-1))
        {
            return T["Mailbox.Yesterday"];
        }

        return localDate.ToString("dd/MM", CultureInfo.InvariantCulture);
    }

    private int TotalPages(int total)
    {
        if (total <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(total / (double)PageSize);
    }
}
