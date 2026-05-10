using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using System.Security.Claims;

namespace Confirmai.Services;

/// <summary>
/// Adds application-level claims to the user principal at login time:
///   - "server_admin" = "true"  — user is GM/admin of at least one server
/// Claims are baked into the auth cookie and available without DB queries.
/// NOTE: if a user's GM membership changes they must re-login to receive updated claims.
/// </summary>
public class CustomClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var isServerAdmin = await db.ServerMembers
            .AsNoTracking()
            .AnyAsync(m => m.UserId == user.Id && m.Role == ServerMemberRole.ServerAdmin);

        if (isServerAdmin)
        {
            identity.AddClaim(new Claim("server_admin", "true"));
        }

        return identity;
    }
}
