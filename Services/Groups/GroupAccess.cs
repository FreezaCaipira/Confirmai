using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

/// <summary>
/// Service-level authorization checks for group-scoped mutations.
/// UI guards alone are not an authorization boundary: any code path that
/// reaches these services must have its role verified here.
/// </summary>
public static class GroupAccess
{
    /// <summary>True when the user is an admin member of the group.</summary>
    public static async Task<bool> IsGroupAdminAsync(AppDbContext db, int groupId, string? userId)
    {
        if (userId is null) return false;
        return await db.GroupMembers.AsNoTracking().AnyAsync(m =>
            m.GroupId == groupId &&
            m.UserId == userId &&
            m.Role == GroupMemberRole.Admin);
    }
}
