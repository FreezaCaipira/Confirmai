using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Confirmai.Data;

namespace Confirmai.Configuration;

public static class ApiKeyAuthDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

public class ApiKeyAuthOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceScopeFactory scopeFactory)
        : base(options, logger, encoder)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthDefaults.HeaderName, out var headerValues))
            return AuthenticateResult.NoResult();

        var apiKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.Fail("Empty API key.");

        var keyHash = ComputeSha256Hex(apiKey);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var dbContext = await db.CreateDbContextAsync();

        var serverApiKey = await dbContext.ServerApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive && k.RevokedAt == null);

        if (serverApiKey is null)
            return AuthenticateResult.Fail("Invalid or revoked API key.");

        // Update LastUsedAt (fire-and-forget, non-blocking)
        _ = UpdateLastUsedAsync(serverApiKey.Id);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, $"server:{serverApiKey.ServerId}"),
            new Claim("server_id", serverApiKey.ServerId.ToString()),
            new Claim("apikey_id", serverApiKey.Id.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task UpdateLastUsedAsync(int apiKeyId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var dbContext = await db.CreateDbContextAsync();

            var key = await dbContext.ServerApiKeys.FindAsync(apiKeyId);
            if (key is not null)
            {
                key.LastUsedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }
        catch
        {
            // Non-critical: don't let LastUsedAt tracking break authentication
        }
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
