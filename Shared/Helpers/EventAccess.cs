using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Shared.Helpers;

/// <summary>
/// Centralizes event-level authorization checks used across Blazor pages.
/// C46 (1.5): the management rule is "group admin OR system admin" —
/// being the creator alone no longer grants management rights.
/// <see cref="IsCreator"/> stays for display-only purposes.
/// </summary>
public static class EventAccess
{
    /// <summary>
    /// Returns true when <paramref name="userId"/> is the creator of the event.
    /// Display-only; does NOT grant management rights.
    /// </summary>
    public static bool IsCreator(Event? ev, string? userId)
        => ev is not null
           && !string.IsNullOrEmpty(userId)
           && ev.CreatedByUserId == userId;

    /// <summary>
    /// Single source of "who can manage this event": an admin member of the
    /// event's group OR a system admin. Requires <c>ev.Group.Members</c>
    /// to be loaded; use <see cref="CanManageAsync"/> otherwise.
    /// </summary>
    public static bool CanManage(Event? ev, string? userId, bool isSystemAdmin)
    {
        if (ev is null || string.IsNullOrEmpty(userId)) return false;
        if (isSystemAdmin) return true;
        return ev.Group?.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin) == true;
    }

    /// <summary>
    /// Async variant for services without the loaded Members graph:
    /// system admin via Identity roles, group admin via GroupAccess.
    /// </summary>
    public static async Task<bool> CanManageAsync(AppDbContext db, Event? ev, string? userId)
    {
        if (ev is null || string.IsNullOrEmpty(userId)) return false;

        var adminRoleIds = await db.Roles.AsNoTracking()
            .Where(r => r.Name == "admin")
            .Select(r => r.Id)
            .ToListAsync();
        if (adminRoleIds.Count > 0 &&
            await db.UserRoles.AsNoTracking()
                .AnyAsync(ur => ur.UserId == userId && adminRoleIds.Contains(ur.RoleId)))
            return true;

        return await Services.Groups.GroupAccess.IsGroupAdminAsync(db, ev.GroupId, userId);
    }

    /// <summary>
    /// Legacy "organizer" concept kept for display purposes (creator OR
    /// group admin). For management authorization use <see cref="CanManage"/>.
    /// </summary>
    public static bool IsAdmin(Event? ev, string? userId)
    {
        if (ev is null || string.IsNullOrEmpty(userId)) return false;
        if (ev.CreatedByUserId == userId) return true;
        return ev.Group?.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin) == true;
    }
}
