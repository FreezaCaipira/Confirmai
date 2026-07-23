using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Pages.Components.Profile;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

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

    // Sport stats
    private Sport activeSport = Sport.Futsal;
    private int futsalTotal;
    private int futsalGoalkeeperTotal;
    private int futsalOutfieldTotal;
    private int pokerTotal;

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;
    [Inject] private IWebHostEnvironment Env { get; set; } = default!;

    private string DisplayName => user?.FullName?.Trim() is { Length: > 0 } name
        ? name
        : user?.UserName ?? "—";

    private string AvatarInitial => DisplayName.Length > 0
        ? DisplayName[0].ToString().ToUpperInvariant()
        : "?";

    // Using ProfileEditModel from ProfileEditForm component
    private ProfileEditForm.ProfileEditModel profileEditModel = new();

    protected override async Task OnParametersSetAsync()
    {
        loading = true;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? authState.User.FindFirstValue("sub")
            ?? string.Empty;
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

            if (!isOwnProfile && isAuthenticated)
                await LoadChatMessagesAsync();

            await LoadSportStatsAsync();
        }

        loading = false;
    }

    private async Task LoadSportStatsAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();

        var confirmations = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.UserId == Id)
            .Select(c => new { c.Event.Sport, c.Position })
            .ToListAsync();

        futsalTotal = confirmations.Count(c => c.Sport == Sport.Futsal);
        futsalGoalkeeperTotal = confirmations.Count(c => c.Sport == Sport.Futsal && c.Position == FutsalPosition.Goalkeeper);
        futsalOutfieldTotal = confirmations.Count(c => c.Sport == Sport.Futsal && c.Position != FutsalPosition.Goalkeeper);
        pokerTotal = confirmations.Count(c => c.Sport == Sport.Poker);
    }

    private void SetSport(Sport sport) => activeSport = sport;

    private void PrefillIntentMessage()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        // intent=message acknowledged — chat always shown for other profiles
        _ = query.TryGetValue("intent", out _);
    }

    private async Task SendMailboxMessageAsync(string body)
    {
        if (!isAuthenticated || string.IsNullOrWhiteSpace(currentUserId) || user == null) return;
        if (isOwnProfile) { messageFeedback = "Não é possível enviar mensagem para o próprio perfil."; return; }
        if (string.IsNullOrWhiteSpace(body)) { messageFeedback = "Escreva uma mensagem antes de enviar."; return; }

        isSendingMessage = true;
        messageFeedback = null;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var senderUser = await UserManager.FindByIdAsync(currentUserId);
            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId = currentUserId,
                SenderDisplayName = senderUser?.UserName,
                RecipientUserId = Id,
                RecipientDisplayName = user.UserName,
                Subject = "Chat pelo perfil",
                Body = body.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            messageBody = string.Empty;
            messageFeedback = "Mensagem enviada.";
            await LoadChatMessagesAsync();
        }
        finally
        {
            isSendingMessage = false;
        }
    }

    private async Task SaveOwnProfileAsync()
    {
        if (user == null || !isOwnProfile || !string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
        {
            profileSaveFeedback = "Não foi possível salvar.";
            return;
        }

        isSavingProfile = true;
        profileSaveFeedback = null;

        user.InstagramHandle = NormalizeOptional(profileEditModel.InstagramHandle);
        user.DiscordHandle = NormalizeOptional(profileEditModel.DiscordHandle);
        user.PixKey = NormalizeOptional(profileEditModel.PixKey);

        var result = await UserManager.UpdateAsync(user);
        isSavingProfile = false;
        profileSaveFeedback = result.Succeeded ? "Dados atualizados." : "Erro ao salvar.";
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Task OnAvatarSelected(IBrowserFile file)
    {
        avatarFile = file;
        avatarFeedback = null;
        return Task.CompletedTask;
    }

    private async Task SaveAvatarAsync()
    {
        if (user == null || !isOwnProfile || avatarFile == null) return;

        const long maxBytes = 2 * 1024 * 1024;
        var allowedTypes = new[] { "image/jpeg", "image/png" };
        var allowedExts = new[] { ".jpg", ".jpeg", ".png" };

        var ext = Path.GetExtension(avatarFile.Name).ToLowerInvariant();
        if (!allowedExts.Contains(ext) || !allowedTypes.Contains(avatarFile.ContentType))
        {
            avatarFeedback = "Formato inválido. Use PNG ou JPG.";
            return;
        }

        if (avatarFile.Size > maxBytes)
        {
            avatarFeedback = "Arquivo muito grande. Máximo 2 MB.";
            return;
        }

        isSavingAvatar = true;
        avatarFeedback = null;

        try
        {
            var avatarsDir = Path.Combine(Env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(avatarsDir);

            var filename = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(avatarsDir, filename);

            await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await using (var readStream = avatarFile.OpenReadStream(maxBytes))
            {
                await readStream.CopyToAsync(stream);
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarPath) && user.AvatarPath.StartsWith("/uploads/"))
            {
                var oldPath = Path.Combine(Env.WebRootPath, user.AvatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            user.AvatarPath = $"/uploads/avatars/{filename}";
            var result = await UserManager.UpdateAsync(user);
            avatarFeedback = result.Succeeded ? "Avatar atualizado." : "Erro ao salvar avatar.";
            if (result.Succeeded) avatarFile = null;
        }
        catch
        {
            avatarFeedback = "Erro ao processar a imagem.";
        }
        finally
        {
            isSavingAvatar = false;
        }
    }

    private async Task RefreshChatAsync() => await LoadChatMessagesAsync();

    private async Task LoadChatMessagesAsync()
    {
        if (!isAuthenticated || isOwnProfile || string.IsNullOrWhiteSpace(currentUserId))
        {
            chatMessages.Clear();
            return;
        }

        await using var db = await DbFactory.CreateDbContextAsync();
        var recent = await db.UserMailboxMessages
            .AsNoTracking()
            .Where(m => (m.SenderUserId == currentUserId && m.RecipientUserId == Id)
                     || (m.SenderUserId == Id && m.RecipientUserId == currentUserId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(80)
            .Select(m => new ProfileChatThread.ProfileChatMessageView
            {
                Body = m.Body,
                IsIncoming = m.SenderUserId != currentUserId,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        chatMessages = recent.OrderBy(m => m.CreatedAt).ToList();
    }

    // Callback wrappers for child components
    private async Task SaveOwnProfileCallback()
        => await SaveOwnProfileAsync();

    private async Task AvatarSelectedCallback(IBrowserFile file)
        => await OnAvatarSelected(file);

    private async Task SaveAvatarCallback()
        => await SaveAvatarAsync();

    private Task SetSportCallback(Sport sport)
    { SetSport(sport); return Task.CompletedTask; }

    private async Task SendMailboxMessageCallback(string body)
        => await SendMailboxMessageAsync(body);

    private async Task RefreshChatCallback()
        => await RefreshChatAsync();
}
