using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Payment;

/// <summary>
/// Calculates the platform fee coverage per match from approved settlements.
/// Extracted from PlatformFeeLedgerService (Ciclo 29 Fase B) so that both
/// the ledger and the settlement service depend on the same source of truth
/// for the residual rule, without the ledger exposing internal statics.
/// </summary>
public static class PlatformFeeCoverage
{
    /// <summary>
    /// Fee already transferred per match, from the items of approved settlements.
    /// Key = EventId, Value = sum of FeeAmount across all Pago settlements.
    /// </summary>
    public static async Task<Dictionary<int, decimal>> GetCoveredByEventAsync(
        AppDbContext db, int groupId)
        => await db.PlatformFeeSettlementItems
            .AsNoTracking()
            .Where(i => i.Settlement.GroupId == groupId
                && i.Settlement.Status == PlatformFeeSettlementStatus.Pago)
            .GroupBy(i => i.EventId)
            .Select(g => new { EventId = g.Key, Covered = g.Sum(i => i.FeeAmount) })
            .ToDictionaryAsync(g => g.EventId, g => g.Covered);
}
