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
    private readonly IOptions<FeeOptions> _feeOptions;

    public PlatformFeeLedgerService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
    }

    /// <summary>
    /// Stamps the platform fee on a confirmation when it is marked as paid.
    /// Idempotent: only stamps if PlatformFeeAmount is still null.
    /// Only applies to futsal events in manual mode (no gateways) with a positive price.
    /// </summary>
    public async Task<bool> StampFeeOnPaidAsync(int confirmationId)
    {
        var fee = _feeOptions.Value.ManualPlatformFeeFixed;
        if (fee <= 0) return false;

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
        if (group.Sport != Sport.Futsal) return false;
        if (group.EnablePaymentGateways) return false;
        if (conf.Event!.Price.GetValueOrDefault() <= 0) return false;

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
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount.HasValue)
            .SumAsync(c => c.PlatformFeeAmount!.Value);

        var settled = await db.PlatformFeeSettlements
            .Where(s => s.GroupId == groupId && s.Status == PlatformFeeSettlementStatus.Pago)
            .SumAsync(s => s.Amount);

        return (accrued, settled, accrued - settled);
    }

    /// <summary>
    /// Projects the per-match fee status using FIFO settlement allocation.
    /// Paid settlements (in SubmittedAt order) cover the oldest matches first;
    /// a partially covered match stays Pendente; surplus becomes credit for
    /// the next match. Rejected/EmAnalise settlements do not abate anything.
    /// </summary>
    public async Task<IReadOnlyList<PlatformFeeMatchStatusProjection>> GetGroupFeeBreakdownByMatchAsync(int groupId)
    {
        await using var db = _dbFactory.CreateDbContext();

        // Matches (events) with accrued fee, oldest first.
        var matches = await db.EventConfirmations
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount.HasValue)
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

        // Collect all event IDs covered by approved settlements (explicit selection).
        var paidSettlementEventIds = await db.PlatformFeeSettlements
            .Where(s => s.GroupId == groupId
                && s.Status == PlatformFeeSettlementStatus.Pago
                && s.SelectedEventIds != null)
            .Select(s => s.SelectedEventIds!)
            .ToListAsync();

        var coveredEventIds = new HashSet<int>();
        foreach (var csv in paidSettlementEventIds)
        {
            foreach (var idStr in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(idStr.Trim(), out var id))
                    coveredEventIds.Add(id);
            }
        }

        var result = new List<PlatformFeeMatchStatusProjection>(matches.Count);
        foreach (var m in matches)
        {
            var status = coveredEventIds.Contains(m.EventId)
                ? PlatformFeeMatchStatus.Pago
                : PlatformFeeMatchStatus.Pendente;
            result.Add(new PlatformFeeMatchStatusProjection(
                m.EventId, m.StartsAt, m.Location, m.PaidPlayers, m.FeeAmount, status));
        }

        return result;
    }
}

/// <summary>Per-match fee status after FIFO settlement allocation (Fase D).</summary>
public record PlatformFeeMatchStatusProjection(
    int EventId,
    DateTime StartsAt,
    string Location,
    int PaidPlayers,
    decimal FeeAmount,
    PlatformFeeMatchStatus Status);
