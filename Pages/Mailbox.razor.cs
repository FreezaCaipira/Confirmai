using System.Globalization;
using System.Security.Claims;
using Confirmai.Pages.Components;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Confirmai.Pages;

public partial class Mailbox
{
    private bool isLoading = true;
    private string currentUserId = string.Empty;
    private string searchTerm = string.Empty;
    private bool inboxUnreadOnly;
    private bool showArchivedConversations;
    private int pageSize = 20;
    private int conversationPage = 1;
    private int conversationTotal;
    private int activeConversationTotal;
    private int archivedConversationTotal;
    private List<ConversationContactView> conversationContacts = new();
    private List<ConversationMessageView> conversationMessages = new();
    private string? selectedConversationKey;
    private bool isThreadLoading;
    private bool isSendingReply;
    private bool shouldAutoScrollThread;
    private long threadLoadVersion;
    private string composerBody = string.Empty;
    private string composerRecipientUserId = string.Empty;
    private string? composerInfoMessage;
    private ElementReference threadStreamRef = default;

    private ConversationContactView? SelectedContact => conversationContacts
        .FirstOrDefault(c => string.Equals(c.ConversationKey, selectedConversationKey, StringComparison.Ordinal));

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? authState.User.FindFirstValue("sub")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            Navigation.NavigateTo("/account/login", true);
            return;
        }

        await LoadMessagesAsync();
        isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!shouldAutoScrollThread)
        {
            return;
        }

        shouldAutoScrollThread = false;
        await JS.InvokeVoidAsync("ConfirmaiMailbox.scrollThreadToBottom", threadStreamRef);
    }

    private async Task LoadMessagesAsync()
    {
        var result = await MailboxQueryService.LoadConversationsAsync(
            currentUserId, searchTerm, inboxUnreadOnly,
            showArchivedConversations, conversationPage, pageSize);

        conversationContacts = result.Contacts;
        conversationTotal = result.TotalConversations;
        activeConversationTotal = result.ActiveConversationTotal;
        archivedConversationTotal = result.ArchivedConversationTotal;

        EnsureValidPageBounds();
        EnsureConversationSelection();
        await LoadThreadForSelectedMessageAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        conversationPage = 1;
        await LoadMessagesAsync();
    }

    private async Task ResetFiltersAsync()
    {
        searchTerm = string.Empty;
        inboxUnreadOnly = false;
        pageSize = 20;
        conversationPage = 1;
        showArchivedConversations = false;
        await LoadMessagesAsync();
    }

    private async Task SwitchConversationFolderAsync(bool archived)
    {
        if (showArchivedConversations == archived)
        {
            return;
        }

        showArchivedConversations = archived;
        conversationPage = 1;
        selectedConversationKey = null;
        await LoadMessagesAsync();
    }

    private async Task PreviousConversationPageAsync()
    {
        if (conversationPage <= 1)
        {
            return;
        }

        conversationPage--;
        await LoadMessagesAsync();
    }

    private async Task NextConversationPageAsync()
    {
        if (conversationPage >= TotalPages(conversationTotal))
        {
            return;
        }

        conversationPage++;
        await LoadMessagesAsync();
    }

    private async Task SelectConversationAsync(string conversationKey)
    {
        selectedConversationKey = conversationKey;
        await LoadThreadForSelectedMessageAsync();
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

        return (int)Math.Ceiling(total / (double)pageSize);
    }

    private void EnsureValidPageBounds()
    {
        var maxPage = TotalPages(conversationTotal);
        if (conversationPage > maxPage)
        {
            conversationPage = maxPage;
        }
    }

    private void EnsureConversationSelection()
    {
        if (!conversationContacts.Any())
        {
            selectedConversationKey = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedConversationKey) || !conversationContacts.Any(c => string.Equals(c.ConversationKey, selectedConversationKey, StringComparison.Ordinal)))
        {
            selectedConversationKey = conversationContacts[0].ConversationKey;
        }
    }

    private async Task LoadThreadForSelectedMessageAsync()
    {
        var requestedConversationKey = selectedConversationKey;
        var requestVersion = Interlocked.Increment(ref threadLoadVersion);

        var selected = SelectedContact;
        if (selected == null)
        {
            conversationMessages.Clear();
            composerRecipientUserId = string.Empty;
            composerInfoMessage = null;
            return;
        }

        composerRecipientUserId = selected.ContactUserId;
        composerInfoMessage = null;

        isThreadLoading = true;

        if (string.IsNullOrWhiteSpace(selected.ContactUserId))
        {
            conversationMessages.Clear();
            isThreadLoading = false;
            return;
        }

        if (selected.ContactUserId == "SYSTEM")
        {
            composerRecipientUserId = string.Empty;
        }

        conversationMessages = await MailboxQueryService.LoadThreadAsync(currentUserId, selected.ContactUserId);

        if (requestVersion != threadLoadVersion || !string.Equals(requestedConversationKey, selectedConversationKey, StringComparison.Ordinal))
        {
            return;
        }

        isThreadLoading = false;
        shouldAutoScrollThread = true;
    }

    private async Task MarkConversationAsReadAsync(string conversationKey)
    {
        if (string.IsNullOrWhiteSpace(conversationKey))
        {
            return;
        }

        var selected = conversationContacts.FirstOrDefault(c => string.Equals(c.ConversationKey, conversationKey, StringComparison.Ordinal));
        if (selected == null)
        {
            return;
        }

        await MailboxQueryService.MarkConversationAsReadAsync(currentUserId, selected.ContactUserId);
        selectedConversationKey = conversationKey;
        await LoadMessagesAsync();
    }

    private async Task SendQuickReplyAsync()
    {
        composerInfoMessage = null;

        var selected = SelectedContact;
        if (selected == null)
        {
            composerInfoMessage = "Selecione uma conversa antes de enviar.";
            return;
        }

        if (string.IsNullOrWhiteSpace(composerBody))
        {
            composerInfoMessage = "Digite uma mensagem para enviar.";
            return;
        }

        var normalizedBody = composerBody.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBody))
        {
            composerInfoMessage = "Digite uma mensagem para enviar.";
            return;
        }

        isSendingReply = true;

        if (string.IsNullOrWhiteSpace(composerRecipientUserId))
        {
            composerInfoMessage = "Selecione uma conversa antes de enviar.";
            isSendingReply = false;
            return;
        }

        await MailboxQueryService.SendQuickReplyAsync(
            currentUserId, composerRecipientUserId, selected.ContactName, normalizedBody);

        composerBody = string.Empty;
        composerInfoMessage = "Mensagem enviada.";
        isSendingReply = false;

        selectedConversationKey = selected.ConversationKey;
        conversationPage = 1;
        await LoadMessagesAsync();
        EnsureConversationSelection();
        await LoadThreadForSelectedMessageAsync();
        shouldAutoScrollThread = true;
    }

    private async Task SetConversationArchivedAsync(string conversationKey, bool archive)
    {
        if (string.IsNullOrWhiteSpace(conversationKey))
        {
            return;
        }

        var selected = conversationContacts.FirstOrDefault(c => string.Equals(c.ConversationKey, conversationKey, StringComparison.Ordinal));
        if (selected == null)
        {
            return;
        }

        await MailboxQueryService.SetConversationArchivedAsync(currentUserId, selected.ContactUserId, archive);

        selectedConversationKey = null;
        await LoadMessagesAsync();
    }

    private async Task SendReplyCallback(string body)
    {
        composerBody = body;
        await SendQuickReplyAsync();
    }

    private async Task SelectConversationCallback(string conversationKey)
        => await SelectConversationAsync(conversationKey);

    private async Task MarkAsReadCallback(string conversationKey)
        => await MarkConversationAsReadAsync(conversationKey);

    private async Task ArchiveCallback(string conversationKey)
        => await SetConversationArchivedAsync(conversationKey, !showArchivedConversations);

    private async Task SwitchFolderCallback(bool showArchived)
        => await SwitchConversationFolderAsync(showArchived);

    private async Task PreviousPageCallback()
        => await PreviousConversationPageAsync();

    private async Task NextPageCallback()
        => await NextConversationPageAsync();

}
