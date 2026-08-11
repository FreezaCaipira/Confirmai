using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Payment;

/// <summary>
/// Authorizes access to a platform fee settlement proof image.
/// Mirrors the authorization rule of /api/pix-proof/{id}: the submitter,
/// any admin of the group that owes the settlement, or a system admin.
/// Extracted from the minimal API endpoint so the rule is unit-testable
/// (the endpoint serves a financial document — the rule must be tested).
/// </summary>
public class PlatformFeeSettlementProofAuthorizer
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PlatformFeeSettlementProofAuthorizer(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    /// <summary>
    /// Returns the proof image bytes and content type when the user is authorized,
    /// or null when the settlement/proof does not exist.
    /// Throws <see cref="UnauthorizedAccessException"/> when the user is not authorized.
    /// </summary>
    public async Task<(byte[] Data, string ContentType)?> GetProofForUserAsync(
        int settlementId,
        string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();

        await using var db = await _dbFactory.CreateDbContextAsync();

        var settlement = await db.PlatformFeeSettlements
            .Include(s => s.Group)
                .ThenInclude(g => g!.Members)
            .FirstOrDefaultAsync(s => s.Id == settlementId);

        if (settlement is null || settlement.ProofImageData is null || settlement.ProofImageData.Length == 0)
            return null;

        bool isSubmitter = settlement.SubmittedByUserId == userId;
        bool isGroupAdmin = settlement.Group?.Members
            .Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin) == true;

        bool isSystemAdmin = false;
        if (!isSubmitter && !isGroupAdmin)
        {
            var adminRoleIds = await db.Roles.AsNoTracking()
                .Where(r => r.Name == "admin")
                .Select(r => r.Id)
                .ToListAsync();
            isSystemAdmin = adminRoleIds.Count > 0 && await db.UserRoles.AsNoTracking()
                .AnyAsync(ur => ur.UserId == userId && adminRoleIds.Contains(ur.RoleId));
        }

        if (!isSubmitter && !isGroupAdmin && !isSystemAdmin)
            throw new UnauthorizedAccessException();

        var contentType = settlement.ProofContentType ?? "image/jpeg";
        return (settlement.ProofImageData, contentType);
    }
}

public enum PlatformFeeProofResult
{
    Authorized = 0,
    NotFound = 1,
    Forbidden = 2,
    Unauthorized = 3
}
