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
}
