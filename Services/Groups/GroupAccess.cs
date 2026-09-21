using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

/// <summary>
/// Service-level authorization checks for group-scoped mutations.
/// UI guards alone are not an authorization boundary: any code path that
/// reaches these services must have its role verified here.
/// The sync helpers evaluate a Group already loaded with Members (pages);
/// the async ones hit the database (services).
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

    /// <summary>True when the user is any member of the group.</summary>
    public static async Task<bool> IsMemberAsync(AppDbContext db, int groupId, string? userId)
    {
        if (userId is null) return false;
        return await db.GroupMembers.AsNoTracking().AnyAsync(m =>
            m.GroupId == groupId && m.UserId == userId);
    }

    /// <summary>Group ids (within <paramref name="groupIds"/>) where the user is an admin member.</summary>
    public static async Task<HashSet<int>> AdminGroupIdsAsync(
        AppDbContext db, IEnumerable<int> groupIds, string? userId)
    {
        if (userId is null) return new HashSet<int>();
        var ids = groupIds.Distinct().ToList();
        var rows = await db.GroupMembers.AsNoTracking()
            .Where(m => ids.Contains(m.GroupId) && m.UserId == userId && m.Role == GroupMemberRole.Admin)
            .Select(m => m.GroupId)
            .ToListAsync();
        return rows.ToHashSet();
    }

    /// <summary>True when the user is a member of the already-loaded group.</summary>
    public static bool IsMember(Group? group, string? userId)
        => userId is not null && group is not null &&
           group.Members.Any(m => m.UserId == userId);

    /// <summary>True when the user is an admin member of the already-loaded group.</summary>
    public static bool IsAdmin(Group? group, string? userId)
        => userId is not null && group is not null &&
           group.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin);
}
