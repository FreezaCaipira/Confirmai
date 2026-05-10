using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Confirmai.Models;

namespace Confirmai;

/// <summary>
/// Periodically revalidates the security stamp of the logged-in user within a Blazor Server
/// circuit. When the stamp changes (e.g. after UpdateSecurityStampAsync is called in the
/// game-login flow), the circuit is treated as unauthenticated on the next revalidation tick
/// — no page refresh required.
/// </summary>
internal sealed class RevalidatingIdentityAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdentityOptions _options;

    public RevalidatingIdentityAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> optionsAccessor)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _options = optionsAccessor.Value;
    }

    // Match the SecurityStampValidatorOptions interval set in Program.cs (30 s).
    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        // Use a fresh scope so we always get up-to-date DB data.
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            return await ValidateSecurityStampAsync(
                userManager,
                authenticationState.User)
                .ConfigureAwait(false);
        }
    }

    private async Task<bool> ValidateSecurityStampAsync(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return false;

        if (!userManager.SupportsUserSecurityStamp)
            return true;

        var principalStamp = principal.FindFirstValue(
            _options.ClaimsIdentity.SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user);

        return principalStamp == userStamp;
    }
}
