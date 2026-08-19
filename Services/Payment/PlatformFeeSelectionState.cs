using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services.Payment;

/// <summary>
/// Pure, side-effect-free helper that encapsulates the platform fee match
/// selection logic used by the organizer's Payments page. Extracted from
/// <c>Payments.razor.cs</c> so the rules are testable without a Blazor
/// render tree (the project has no bUnit).
/// </summary>
public static class PlatformFeeSelectionState
{
    /// <summary>
    /// A match is selectable only while its fee status is <c>Pendente</c>.
    /// Matches already covered by an approved settlement (<c>Pago</c>) are
    /// shown for context but cannot be picked again.
    /// </summary>
    public static bool IsSelectable(PlatformFeeMatch match)
        => match.Status == PlatformFeeMatchStatus.Pendente;

    /// <summary>
    /// Sum of the fees of the matches currently selected by the organizer.
    /// Used to display the amount that will be sent in the settlement and
    /// to validate it against the declared <c>amount</c>.
    /// </summary>
    public static decimal SelectedAmount(
        IReadOnlyList<PlatformFeeMatch> matches,
        IReadOnlySet<int> selectedEventIds)
        => matches
            .Where(m => selectedEventIds.Contains(m.EventId))
            .Sum(m => m.FeeAmount);

    /// <summary>
    /// The organizer can submit a settlement only when at least one pending
    /// match is selected. An empty selection disables the submit button.
    /// </summary>
    public static bool CanSubmit(IReadOnlySet<int> selectedEventIds)
        => selectedEventIds.Count > 0;
}
