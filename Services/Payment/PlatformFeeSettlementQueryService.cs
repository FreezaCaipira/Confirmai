using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Read-only queries for the platform fee settlement UI (Fase A/B do Ciclo 27).
/// Returns the per-match breakdown, group balance and settlement history
/// that the organizer and the system admin pages render.
/// </summary>
public class PlatformFeeSettlementQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IOptions<FeeOptions> _feeOptions;

    public PlatformFeeSettlementQueryService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
    }

    /// <summary>
    /// Returns the full fee overview for a group: per-match breakdown, balance
    /// (accrued/settled/due/in-analysis) and the settlement history.
    /// </summary>
    public async Task<PlatformFeeOverview> GetGroupFeeOverviewAsync(int groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var matchRows = await db.EventConfirmations
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount.HasValue)
            .Include(c => c.Event)
            .GroupBy(c => new { c.Event!.Id, c.Event.StartsAt, c.Event.Location })
            .Select(g => new PlatformFeeMatchRow
            {
                EventId = g.Key.Id,
                StartsAt = g.Key.StartsAt,
                Location = g.Key.Location,
                PaidPlayers = g.Count(),
                FeeAmount = g.Sum(c => c.PlatformFeeAmount!.Value)
            })
            .OrderBy(m => m.StartsAt)
            .ToListAsync();

        var accrued = matchRows.Sum(m => m.FeeAmount);

        var settlementRows = await db.PlatformFeeSettlements
            .Where(s => s.GroupId == groupId)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new
            {
                s.Id, s.Amount, s.Status, s.SubmittedAt, s.ReviewedAt, s.ReviewNote,
                HasProof = s.ProofImageData != null && s.ProofImageData.Length > 0
            })
            .ToListAsync();

        var settlements = settlementRows
            .Select(r => new PlatformFeeSettlementRow(
                r.Id, r.Amount, r.Status, r.SubmittedAt, r.ReviewedAt, r.ReviewNote, r.HasProof))
            .ToList();

        var settled = settlements.Where(s => s.Status == PlatformFeeSettlementStatus.Pago).Sum(s => s.Amount);
        var inAnalysis = settlements.Where(s => s.Status == PlatformFeeSettlementStatus.EmAnalise).Sum(s => s.Amount);

        // Status por partida: na Fase A todas com taxa stamped e due > 0 ficam Pendente.
        // A baixa FIFO (Fase D) refina este status por partida.
        var matches = matchRows
            .Select(m => new PlatformFeeMatch(
                m.EventId,
                m.StartsAt,
                m.Location,
                m.PaidPlayers,
                m.FeeAmount,
                PlatformFeeMatchStatus.Pendente))
            .ToList();

        var opts = _feeOptions.Value;

        return new PlatformFeeOverview(
            matches,
            accrued,
            settled,
            accrued - settled,
            inAnalysis,
            settlements,
            opts.PlatformPixKey ?? string.Empty,
            opts.PlatformPixCity);
    }

    private sealed class PlatformFeeMatchRow
    {
        public int EventId { get; set; }
        public DateTime StartsAt { get; set; }
        public string Location { get; set; } = string.Empty;
        public int PaidPlayers { get; set; }
        public decimal FeeAmount { get; set; }
    }
}

/// <summary>Snapshot of the platform fee state of a group for the UI.</summary>
public record PlatformFeeOverview(
    IReadOnlyList<PlatformFeeMatch> Matches,
    decimal Accrued,
    decimal Settled,
    decimal Due,
    decimal InAnalysis,
    IReadOnlyList<PlatformFeeSettlementRow> Settlements,
    string PlatformPixKey,
    string? PlatformPixCity);

/// <summary>One match (event) with its accrued platform fee.</summary>
public record PlatformFeeMatch(
    int EventId,
    DateTime StartsAt,
    string Location,
    int PaidPlayers,
    decimal FeeAmount,
    PlatformFeeMatchStatus Status);

public enum PlatformFeeMatchStatus
{
    Pendente = 0,
    Pago = 1
}

/// <summary>Settlement row for the history list.</summary>
public record PlatformFeeSettlementRow(
    int Id,
    decimal Amount,
    PlatformFeeSettlementStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? ReviewNote,
    bool HasProof);
