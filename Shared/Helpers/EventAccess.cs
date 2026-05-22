using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Shared.Helpers;

/// <summary>
/// Centralizes event-level authorization checks used across Blazor pages.
/// </summary>
public static class EventAccess
{
    /// <summary>
    /// Returns true when <paramref name="userId"/> is the creator of the event.
    /// Both parameters must be non-null and non-empty, otherwise returns false.
    /// </summary>
    public static bool IsCreator(Event? ev, string? userId)
        => ev is not null
           && !string.IsNullOrEmpty(userId)
           && ev.CreatedByUserId == userId;

    /// <summary>
    /// Returns true when <paramref name="userId"/> is the event creator
    /// OR has <see cref="GroupMemberRole.Admin"/> in the event's group.
    /// Requires <c>ev.Group.Members</c> to be loaded.
    /// </summary>
    public static bool IsAdmin(Event? ev, string? userId)
    {
        if (ev is null || string.IsNullOrEmpty(userId)) return false;
        if (ev.CreatedByUserId == userId) return true;
        return ev.Group?.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin) == true;
    }
}
