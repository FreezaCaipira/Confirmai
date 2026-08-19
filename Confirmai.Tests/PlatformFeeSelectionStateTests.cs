using Confirmai.Enums;
using Confirmai.Services.Payment;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Tests for the pure selection logic extracted from Payments.razor.cs
/// (Fase D do Ciclo 28). Covers the rules the plan lists:
///  - nothing selected disables submit
///  - displayed amount == sum of selected fees
///  - paid match is not selectable
/// </summary>
public class PlatformFeeSelectionStateTests
{
    private static PlatformFeeMatch Match(int id, decimal fee, PlatformFeeMatchStatus status)
        => new(id, new DateTime(2026, 8, 10), "Quadra", "Racha", 1, fee, status);

    private static HashSet<int> Set(params int[] ids) => new(ids);

    [Fact]
    public void CanSubmit_EmptySelection_ReturnsFalse()
    {
        Assert.False(PlatformFeeSelectionState.CanSubmit(Set()));
    }

    [Fact]
    public void CanSubmit_NonEmptySelection_ReturnsTrue()
    {
        Assert.True(PlatformFeeSelectionState.CanSubmit(Set(1)));
    }

    [Fact]
    public void SelectedAmount_NoSelection_ReturnsZero()
    {
        var matches = new List<PlatformFeeMatch>
        {
            Match(1, 0.75m, PlatformFeeMatchStatus.Pendente),
            Match(2, 0.75m, PlatformFeeMatchStatus.Pendente)
        };

        Assert.Equal(0m, PlatformFeeSelectionState.SelectedAmount(matches, Set()));
    }

    [Fact]
    public void SelectedAmount_SingleSelection_ReturnsThatFee()
    {
        var matches = new List<PlatformFeeMatch>
        {
            Match(1, 0.75m, PlatformFeeMatchStatus.Pendente),
            Match(2, 1.50m, PlatformFeeMatchStatus.Pendente)
        };

        Assert.Equal(0.75m, PlatformFeeSelectionState.SelectedAmount(matches, Set(1)));
    }

    [Fact]
    public void SelectedAmount_MultipleSelections_ReturnsSumOfFees()
    {
        var matches = new List<PlatformFeeMatch>
        {
            Match(1, 0.75m, PlatformFeeMatchStatus.Pendente),
            Match(2, 1.50m, PlatformFeeMatchStatus.Pendente),
            Match(3, 2.25m, PlatformFeeMatchStatus.Pendente)
        };

        Assert.Equal(4.50m, PlatformFeeSelectionState.SelectedAmount(matches, Set(1, 2, 3)));
    }

    [Fact]
    public void SelectedAmount_SelectionIncludesPaidMatch_StillSumsByEventId()
    {
        // The helper sums by EventId (matching the original UI behavior).
        // The UI prevents paid matches from entering the selection set via
        // IsSelectable — tested separately. This test documents that the
        // helper itself does not filter by status.
        var matches = new List<PlatformFeeMatch>
        {
            Match(1, 0.75m, PlatformFeeMatchStatus.Pendente),
            Match(2, 1.50m, PlatformFeeMatchStatus.Pago)
        };

        Assert.Equal(2.25m, PlatformFeeSelectionState.SelectedAmount(matches, Set(1, 2)));
    }

    [Fact]
    public void IsSelectable_PendingMatch_ReturnsTrue()
    {
        Assert.True(PlatformFeeSelectionState.IsSelectable(Match(1, 0.75m, PlatformFeeMatchStatus.Pendente)));
    }

    [Fact]
    public void IsSelectable_PaidMatch_ReturnsFalse()
    {
        Assert.False(PlatformFeeSelectionState.IsSelectable(Match(1, 0.75m, PlatformFeeMatchStatus.Pago)));
    }

    [Fact]
    public void SelectedAmount_EmptyMatchList_ReturnsZero()
    {
        Assert.Equal(0m, PlatformFeeSelectionState.SelectedAmount(
            new List<PlatformFeeMatch>(), Set(1, 2)));
    }
}
