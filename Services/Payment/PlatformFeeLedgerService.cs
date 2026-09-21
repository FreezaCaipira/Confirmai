using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Manages the platform fee ledger for the V1 manual flow.
/// Stamps the fee amount on confirmations when they are marked as paid,
/// and tracks the group balance (accrued, settled, due).
/// </summary>
public class PlatformFeeLedgerService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly PlatformFeePolicy _feePolicy;

    public PlatformFeeLedgerService(
        IDbContextFactory<AppDbContext> dbFactory,
        PlatformFeePolicy feePolicy)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feePolicy = feePolicy ?? throw new ArgumentNullException(nameof(feePolicy));
    }

    /// <summary>
    /// Stamps the platform fee on a confirmation when it is marked as paid.
    /// Idempotent: only stamps if PlatformFeeAmount is still null.
    /// Only applies to futsal events in manual mode (no gateways) with a positive price.
    /// A waived group is still stamped — with an explicit 0 — so the snapshot and
    /// the ledger record the fee as zero instead of silently skipping it.
    /// </summary>
    public async Task<bool> StampFeeOnPaidAsync(int confirmationId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var conf = await db.EventConfirmations
            .AsTracking()
            .Include(c => c.Event)
                .ThenInclude(e => e!.Group)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null) return false;
        if (conf.PlatformFeeAmount is not null) return false; // idempotent
        if (conf.PaymentStatus != EventConfirmationPaymentStatus.Paid) return false;

        var group = conf.Event?.Group;
        if (group is null) return false;
        if (!PlatformFeePolicy.AppliesTo(group, conf.Event!.Price)) return false;

        // Legado sem carimbo (criada antes do C36-C): resolve pelo instante da
        // confirmacao, nao do stamp — a isencao concedida depois nao retroage.
        var asOf = conf.ConfirmedAt;
        var waived = _feePolicy.IsWaived(group, asOf);
        var fee = _feePolicy.ResolveManualFee(group, asOf);
        if (!waived && fee <= 0) return false; // configured fee off and not waived: nothing to stamp

        conf.PlatformFeeAmount = fee;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Returns the group's fee balance: accrued, settled, and due.
    /// Due = Accrued - Settled (only counting settlements with status Pago).
    /// </summary>
    public async Task<(decimal Accrued, decimal Settled, decimal Due)> GetGroupBalanceAsync(int groupId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var accrued = await db.EventConfirmations
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount.HasValue && c.PaymentStatus == EventConfirmationPaymentStatus.Paid)
            .SumAsync(c => c.PlatformFeeAmount!.Value);

        var settled = await db.PlatformFeeSettlements
            .Where(s => s.GroupId == groupId && s.Status == PlatformFeeSettlementStatus.Pago)
            .SumAsync(s => s.Amount);

        return (accrued, settled, accrued - settled);
    }

    /// <summary>
    /// Projects the per-match fee status from the explicit match selection of
    /// each settlement, comparing amounts: a match is Pago only when approved
    /// settlements cover its whole accrued fee. If a late player pays the same
    /// match after a settlement was approved, the match stays Pendente for the
    /// residual. Rejected/EmAnalise settlements do not abate anything.
    /// </summary>
    public async Task<IReadOnlyList<PlatformFeeMatchStatusProjection>> GetGroupFeeBreakdownByMatchAsync(int groupId)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Matches (events) with accrued fee, oldest first.
        // Zero-stamped (waived) lots are excluded — nothing to settle for them.
        var matches = await db.EventConfirmations
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount > 0 && c.PaymentStatus == EventConfirmationPaymentStatus.Paid)
            .Include(c => c.Event)
            .GroupBy(c => new { c.Event!.Id, c.Event.StartsAt, c.Event.Location })
            .Select(g => new
            {
                EventId = g.Key.Id,
                StartsAt = g.Key.StartsAt,
                Location = g.Key.Location,
                FeeAmount = g.Sum(c => c.PlatformFeeAmount!.Value),
                PaidPlayers = g.Count()
            })
            .OrderBy(m => m.StartsAt)
            .ToListAsync();

        if (matches.Count == 0)
            return Array.Empty<PlatformFeeMatchStatusProjection>();

        var coveredByEvent = await PlatformFeeCoverage.GetCoveredByEventAsync(db, groupId);

        var result = new List<PlatformFeeMatchStatusProjection>(matches.Count);
        foreach (var m in matches)
        {
            var covered = coveredByEvent.GetValueOrDefault(m.EventId);
            var residual = m.FeeAmount - covered;
            var status = residual <= 0
                ? PlatformFeeMatchStatus.Pago
                : PlatformFeeMatchStatus.Pendente;
            var displayedFee = status == PlatformFeeMatchStatus.Pago ? m.FeeAmount : residual;
            result.Add(new PlatformFeeMatchStatusProjection(
                m.EventId, m.StartsAt, m.Location, m.PaidPlayers, displayedFee, status));
        }

        return result;
    }
}

/// <summary>Per-match fee status after explicit settlement selection (Fase D).</summary>
public record PlatformFeeMatchStatusProjection(
    int EventId,
    DateTime StartsAt,
    string Location,
    int PaidPlayers,
    decimal FeeAmount,
    PlatformFeeMatchStatus Status);
