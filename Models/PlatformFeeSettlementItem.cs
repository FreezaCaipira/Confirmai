namespace Confirmai.Models;

/// <summary>
/// One row per match (event) covered by a <see cref="PlatformFeeSettlement"/>.
/// Replaces the old <c>SelectedEventIds</c> CSV column: gives referential
/// integrity to <see cref="Event"/> and snapshots the fee amount of the match
/// at the moment the settlement was submitted (same logic as
/// <see cref="EventConfirmation.PlatformFeeAmount"/>).
/// </summary>
public class PlatformFeeSettlementItem
{
    public int Id { get; set; }

    public int SettlementId { get; set; }
    public PlatformFeeSettlement Settlement { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    /// <summary>
    /// Snapshot of the match's accrued platform fee at the moment the
    /// settlement was submitted. Stored so that later changes to the
    /// confirmation ledger do not rewrite history of an approved settlement.
    /// </summary>
    public decimal FeeAmount { get; set; }
}
