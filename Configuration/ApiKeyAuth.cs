using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Confirmai.Services;

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
        if (!Request.Headers.TryGetValue(ApiKeyAuthDefaults.HeaderName, out var headerValue))
        {
            return AuthenticateResult.NoResult();
        }

        var rawKey = headerValue.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return AuthenticateResult.Fail("Empty API key.");
        }

        using var scope = _scopeFactory.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

        var (key, server) = await apiKeyService.ValidateKeyAsync(rawKey);
        if (key == null || server == null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!server.IsActive)
        {
            return AuthenticateResult.Fail("Server is inactive.");
        }

        var claims = new[]
        {
            new Claim("ServerId", server.Id.ToString()),
            new Claim("ServerName", server.Name),
            new Claim("ApiKeyId", key.Id.ToString()),
            new Claim(ClaimTypes.AuthenticationMethod, ApiKeyAuthDefaults.AuthenticationScheme)
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
}
