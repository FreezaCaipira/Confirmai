using System.Security.Claims;
using Confirmai.Models;
using Microsoft.AspNetCore.Hosting;

namespace Confirmai.Services.User;

/// <summary>
/// Extracts profile data (FullName, AvatarPath) from external login claims.
/// Extracted from ExternalLogin.cshtml.cs in C30-B Fase 1 so the logic is
/// testable without spinning up the Identity pipeline.
/// </summary>
public sealed class ExternalLoginClaimsExtractor
{
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http;

    public ExternalLoginClaimsExtractor(IWebHostEnvironment env, HttpClient http)
    {
        _env = env;
        _http = http;
    }

    /// <summary>
    /// Extracts FullName from claims: prefers ClaimTypes.Name, falls back to
    /// given_name + family_name. Returns null if nothing is available.
    /// </summary>
    public static string? ExtractFullName(ClaimsPrincipal principal)
    {
        var name = principal.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        var given = principal.FindFirstValue(ClaimTypes.GivenName);
        var family = principal.FindFirstValue(ClaimTypes.Surname);
        if (!string.IsNullOrWhiteSpace(given) || !string.IsNullOrWhiteSpace(family))
            return $"{given} {family}".Trim();

        return null;
    }

    /// <summary>
    /// Extracts the profile picture URL from the "picture" claim (Google).
    /// </summary>
    public static string? ExtractPictureUrl(ClaimsPrincipal principal)
        => principal.FindFirstValue("picture");

    /// <summary>
    /// Downloads the picture from the URL and saves it to /uploads/avatars/
    /// in the convention that ProfileService uses. Returns the relative path
    /// (e.g. "/uploads/avatars/abc.png") or null on failure.
    /// Best-effort: never throws — a failed download must not block login.
    /// </summary>
    public async Task<string?> DownloadAvatarAsync(string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl))
            return null;

        try
        {
            var response = await _http.GetAsync(pictureUrl);
            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var ext = contentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                _ => ".jpg"
            };

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0 || bytes.Length > 2 * 1024 * 1024)
                return null;

            var avatarsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(avatarsDir);
            var filename = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(avatarsDir, filename);

            await File.WriteAllBytesAsync(fullPath, bytes);
            return $"/uploads/avatars/{filename}";
        }
        catch
        {
            // Best-effort: avatar download failure must not block login.
            return null;
        }
    }

    /// <summary>
    /// Applies claims to a user, filling only empty fields (never overwriting
    /// existing data — the user may have edited their profile already).
    /// isExistingAccount=true means we're linking to an account that already
    /// exists (by email match); in that case we never download the avatar
    /// (the user may have uploaded one already, and even if they haven't,
    /// overwriting on link is surprising).
    /// </summary>
    public async Task ApplyClaimsAsync(
        ApplicationUser user,
        ClaimsPrincipal principal,
        bool isExistingAccount)
    {
        if (string.IsNullOrWhiteSpace(user.FullName))
            user.FullName = ExtractFullName(principal);

        // Only download avatar for new accounts. For existing accounts being
        // linked, never overwrite — even an empty avatar is the user's choice
        // until they explicitly set one.
        if (!isExistingAccount && string.IsNullOrWhiteSpace(user.AvatarPath))
        {
            var pictureUrl = ExtractPictureUrl(principal);
            var downloaded = await DownloadAvatarAsync(pictureUrl);
            if (downloaded is not null)
                user.AvatarPath = downloaded;
        }
    }
}
