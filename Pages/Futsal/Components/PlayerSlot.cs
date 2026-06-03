namespace Confirmai.Pages.Futsal.Components;

/// <summary>
/// Represents a player slot assignment in an escalacao (team lineup).
/// Used for both draft team building and display.
/// </summary>
public record PlayerSlot(string UserId, string Name, bool IsGoalkeeper);
