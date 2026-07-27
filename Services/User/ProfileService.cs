using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.User;

public sealed class ProfileService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ProfileService(IDbContextFactory<AppDbContext> dbFactory, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _dbFactory = dbFactory;
        _userManager = userManager;
        _env = env;
    }

    public async Task<(int FutsalTotal, int FutsalGk, int FutsalOut, int PokerTotal)> LoadSportStatsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var confirmations = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Event.Sport, c.Position })
            .ToListAsync();

        return (
            confirmations.Count(c => c.Sport == Sport.Futsal),
            confirmations.Count(c => c.Sport == Sport.Futsal && c.Position == FutsalPosition.Goalkeeper),
            confirmations.Count(c => c.Sport == Sport.Futsal && c.Position != FutsalPosition.Goalkeeper),
            confirmations.Count(c => c.Sport == Sport.Poker));
    }

    public async Task SendMessageAsync(string senderUserId, string recipientUserId, string body)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var senderUser = await _userManager.FindByIdAsync(senderUserId);
        var recipientUser = await _userManager.FindByIdAsync(recipientUserId);
        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId = senderUserId,
            SenderDisplayName = senderUser?.UserName,
            RecipientUserId = recipientUserId,
            RecipientDisplayName = recipientUser?.UserName,
            Subject = "Chat pelo perfil",
            Body = body.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ProfileChatMessage>> LoadChatMessagesAsync(string currentUserId, string profileId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var recent = await db.UserMailboxMessages
            .AsNoTracking()
            .Where(m => (m.SenderUserId == currentUserId && m.RecipientUserId == profileId)
                     || (m.SenderUserId == profileId && m.RecipientUserId == currentUserId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(80)
            .Select(m => new ProfileChatMessage(m.Body, m.SenderUserId != currentUserId, m.CreatedAt))
            .ToListAsync();

        return recent.OrderBy(m => m.CreatedAt).ToList();
    }

    public async Task<(bool Success, string Message)> SaveAvatarAsync(ApplicationUser user, IBrowserFile file)
    {
        const long maxBytes = 2 * 1024 * 1024;
        var allowedTypes = new[] { "image/jpeg", "image/png" };
        var allowedExts = new[] { ".jpg", ".jpeg", ".png" };

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!allowedExts.Contains(ext) || !allowedTypes.Contains(file.ContentType))
            return (false, "Formato invalido. Use PNG ou JPG.");

        if (file.Size > maxBytes)
            return (false, "Arquivo muito grande. Maximo 2 MB.");

        try
        {
            var avatarsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(avatarsDir);
            var filename = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(avatarsDir, filename);

            await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await using (var readStream = file.OpenReadStream(maxBytes))
            {
                await readStream.CopyToAsync(stream);
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarPath) && user.AvatarPath.StartsWith("/uploads/"))
            {
                var oldPath = Path.Combine(_env.WebRootPath, user.AvatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            user.AvatarPath = $"/uploads/avatars/{filename}";
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? (true, "Avatar atualizado.") : (false, "Erro ao salvar avatar.");
        }
        catch
        {
            return (false, "Erro ao processar a imagem.");
        }
    }
}

public sealed record ProfileChatMessage(string Body, bool IsIncoming, DateTime CreatedAt);
