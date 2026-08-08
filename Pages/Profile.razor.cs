using System.Security.Claims;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Pages.Components.Profile;
using Confirmai.Services.Core;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Confirmai.Pages;

public partial class Profile
{
    [Parameter] public string Id { get; set; } = string.Empty;
    private ApplicationUser? user;
    private bool loading = true;
    private bool isAuthenticated;
    private bool isOwnProfile;
    private string currentUserId = string.Empty;
    private string messageBody = string.Empty;
    private string? messageFeedback;
    private bool isSendingMessage;
    private bool isSavingProfile;
    private string? profileSaveFeedback;
    private IBrowserFile? avatarFile;
    private string? avatarFeedback;
    private bool isSavingAvatar;
    private List<ProfileChatThread.ProfileChatMessageView> chatMessages = new();

    private Sport activeSport = Sport.Futsal;
    private int futsalTotal, futsalGoalkeeperTotal, futsalOutfieldTotal, pokerTotal;

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ProfileService ProfileSvc { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    private string DisplayName => user?.FullName?.Trim() is { Length: > 0 } name ? name : user?.UserName ?? "—";
    private string AvatarInitial => DisplayName.Length > 0 ? DisplayName[0].ToString().ToUpperInvariant() : "?";

    private ProfileEditForm.ProfileEditModel profileEditModel = new();

    protected override async Task OnParametersSetAsync()
    {
        loading = true;
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? authState.User.FindFirstValue("sub") ?? string.Empty;
        isAuthenticated = !string.IsNullOrWhiteSpace(currentUserId);
        isOwnProfile = isAuthenticated && string.Equals(currentUserId, Id, StringComparison.Ordinal);

        user = await UserManager.FindByIdAsync(Id);
        if (user != null)
        {
            profileEditModel = new ProfileEditForm.ProfileEditModel
            {
                InstagramHandle = user.InstagramHandle,
                DiscordHandle = user.DiscordHandle,
                PixKey = user.PixKey,
            };
            PrefillIntentMessage();
            if (!isOwnProfile && isAuthenticated) await LoadChatMessagesAsync();
            await LoadSportStatsAsync();
        }
        loading = false;
    }

    private async Task LoadSportStatsAsync()
    {
        var (total, gk, outfield, poker) = await ProfileSvc.LoadSportStatsAsync(Id);
        futsalTotal = total; futsalGoalkeeperTotal = gk; futsalOutfieldTotal = outfield; pokerTotal = poker;
    }

    private void SetSport(Sport sport) => activeSport = sport;

    private void PrefillIntentMessage()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        _ = query.TryGetValue("intent", out _);
    }

    private async Task SendMailboxMessageAsync(string body)
    {
        if (!isAuthenticated || string.IsNullOrWhiteSpace(currentUserId) || user == null) return;
        if (isOwnProfile) { messageFeedback = "Nao e possivel enviar mensagem para o proprio perfil."; return; }
        if (string.IsNullOrWhiteSpace(body)) { messageFeedback = "Escreva uma mensagem antes de enviar."; return; }

        isSendingMessage = true;
        messageFeedback = null;
        try
        {
            await ProfileSvc.SendMessageAsync(currentUserId, Id, body);
            messageBody = string.Empty;
            messageFeedback = "Mensagem enviada.";
            await LoadChatMessagesAsync();
        }
        finally { isSendingMessage = false; }
    }

    private async Task SaveOwnProfileAsync()
    {
        if (user == null || !isOwnProfile || !string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        { profileSaveFeedback = "Nao foi possivel salvar."; return; }

        isSavingProfile = true;
        profileSaveFeedback = null;
        user.InstagramHandle = NormalizeOptional(profileEditModel.InstagramHandle);
        user.DiscordHandle = NormalizeOptional(profileEditModel.DiscordHandle);
        user.PixKey = NormalizeOptional(profileEditModel.PixKey);
        var result = await UserManager.UpdateAsync(user);
        isSavingProfile = false;
        profileSaveFeedback = result.Succeeded ? "Dados atualizados." : "Erro ao salvar.";
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Task OnAvatarSelected(IBrowserFile file) { avatarFile = file; avatarFeedback = null; return Task.CompletedTask; }

    private async Task SaveAvatarAsync()
    {
        if (user == null || !isOwnProfile || avatarFile == null) return;
        isSavingAvatar = true;
        avatarFeedback = null;
        try
        {
            var (success, msg) = await ProfileSvc.SaveAvatarAsync(user, avatarFile);
            avatarFeedback = msg;
            if (success) avatarFile = null;
        }
        finally { isSavingAvatar = false; }
    }

    private async Task RefreshChatAsync() => await LoadChatMessagesAsync();

    private async Task LoadChatMessagesAsync()
    {
        if (!isAuthenticated || isOwnProfile || string.IsNullOrWhiteSpace(currentUserId)) { chatMessages.Clear(); return; }
        var msgs = await ProfileSvc.LoadChatMessagesAsync(currentUserId, Id);
        chatMessages = msgs.Select(m => new ProfileChatThread.ProfileChatMessageView { Body = m.Body, IsIncoming = m.IsIncoming, CreatedAt = m.CreatedAt }).ToList();
    }

    // Callback wrappers
    private async Task SaveOwnProfileCallback() => await SaveOwnProfileAsync();
    private async Task AvatarSelectedCallback(IBrowserFile file) => await OnAvatarSelected(file);
    private async Task SaveAvatarCallback() => await SaveAvatarAsync();
    private Task SetSportCallback(Sport sport) { SetSport(sport); return Task.CompletedTask; }
    private async Task SendMailboxMessageCallback(string body) => await SendMailboxMessageAsync(body);
    private async Task RefreshChatCallback() => await RefreshChatAsync();
}
