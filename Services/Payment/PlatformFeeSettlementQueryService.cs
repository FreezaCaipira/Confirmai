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
    private readonly PlatformFeeLedgerService _ledger;

    public PlatformFeeSettlementQueryService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions,
        PlatformFeeLedgerService ledger)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    /// <summary>
    /// Returns the full fee overview for a group: per-match breakdown, balance
    /// (accrued/settled/due/in-analysis) and the settlement history.
    /// </summary>
    public async Task<PlatformFeeOverview> GetGroupFeeOverviewAsync(int groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var matchRows = await db.EventConfirmations
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount > 0)
            .Include(c => c.Event)
                .ThenInclude(e => e!.Group)
            .GroupBy(c => new { c.Event!.Id, c.Event.StartsAt, c.Event.Location, GroupName = c.Event.Group!.Name })
            .Select(g => new PlatformFeeMatchRow
            {
                EventId = g.Key.Id,
                StartsAt = g.Key.StartsAt,
                Location = g.Key.Location,
                GroupName = g.Key.GroupName,
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

        // Status por partida a partir da selecao explicita de cada lote aprovado.
        var breakdown = await _ledger.GetGroupFeeBreakdownByMatchAsync(groupId);
        var statusByEvent = breakdown.ToDictionary(m => m.EventId, m => m.Status);

        var matches = matchRows
            .Select(m => new PlatformFeeMatch(
                m.EventId,
                m.StartsAt,
                m.Location,
                m.GroupName,
                m.PaidPlayers,
                m.FeeAmount,
                statusByEvent.TryGetValue(m.EventId, out var st) ? st : PlatformFeeMatchStatus.Pendente))
            .ToList();

        // Due = sum of fees from matches still Pendente (not covered by an approved settlement).
        var due = matches
            .Where(m => m.Status == PlatformFeeMatchStatus.Pendente)
            .Sum(m => m.FeeAmount);

        var opts = _feeOptions.Value;

        return new PlatformFeeOverview(
            matches,
            accrued,
            settled,
            due,
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
        public string GroupName { get; set; } = string.Empty;
        public int PaidPlayers { get; set; }
        public decimal FeeAmount { get; set; }
    }

    /// <summary>
    /// Returns the settlement review queue for the system admin:
    /// groups with accrued fee (pendente/em analise/quitado) and the
    /// settlements awaiting review (EmAnalise), newest first.
    /// </summary>
    public async Task<PlatformFeeReviewQueue> GetReviewQueueAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var groupRows = await db.PlatformFeeSettlements
            .GroupBy(s => s.GroupId)
            .Select(g => new
            {
                GroupId = g.Key,
                Settled = g.Where(s => s.Status == PlatformFeeSettlementStatus.Pago).Sum(s => s.Amount),
                InAnalysis = g.Where(s => s.Status == PlatformFeeSettlementStatus.EmAnalise).Sum(s => s.Amount),
                Rejected = g.Where(s => s.Status == PlatformFeeSettlementStatus.Rejeitado).Sum(s => s.Amount)
            })
            .ToListAsync();

        // Groups that owe fee but never submitted a settlement must show up too,
        // otherwise the admin cannot see who is not paying at all.
        var accruedByGroup = await db.EventConfirmations
            .Where(c => c.PlatformFeeAmount > 0)
            .GroupBy(c => c.Event!.GroupId)
            .Select(g => new { GroupId = g.Key, Accrued = g.Sum(c => c.PlatformFeeAmount!.Value) })
            .ToDictionaryAsync(g => g.GroupId, g => g.Accrued);

        var groupIds = groupRows.Select(g => g.GroupId)
            .Union(accruedByGroup.Keys)
            .ToList();

        var groupNames = await db.Groups
            .Where(gr => groupIds.Contains(gr.Id))
            .Select(gr => new { gr.Id, gr.Name })
            .ToDictionaryAsync(gr => gr.Id, gr => gr.Name);

        var settlementsByGroup = groupRows.ToDictionary(g => g.GroupId);

        var groups = groupIds
            .Select(groupId =>
            {
                accruedByGroup.TryGetValue(groupId, out var accrued);
                groupNames.TryGetValue(groupId, out var name);
                settlementsByGroup.TryGetValue(groupId, out var s);
                var settled = s?.Settled ?? 0m;
                var inAnalysis = s?.InAnalysis ?? 0m;
                // Due = accrued - settled, but never negative (overpayment is credit, not negative debt).
                var due = accrued - settled;
                if (due < 0) due = 0;
                return new PlatformFeeGroupReview(
                    groupId,
                    name ?? $"Grupo #{groupId}",
                    accrued,
                    settled,
                    due,
                    inAnalysis);
            })
            .OrderByDescending(g => g.InAnalysis)
            .ThenByDescending(g => g.Due)
            .ToList();

        var pendingSettlements = await db.PlatformFeeSettlements
            .Where(s => s.Status == PlatformFeeSettlementStatus.EmAnalise)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new PlatformFeeReviewSettlementRow
            {
                Id = s.Id,
                GroupId = s.GroupId,
                Amount = s.Amount,
                SubmittedAt = s.SubmittedAt,
                HasProof = s.ProofImageData != null && s.ProofImageData.Length > 0
            })
            .ToListAsync();

        var pendingGroupNames = await db.Groups
            .Where(gr => pendingSettlements.Select(s => s.GroupId).Distinct().Contains(gr.Id))
            .Select(gr => new { gr.Id, gr.Name })
            .ToDictionaryAsync(gr => gr.Id, gr => gr.Name);

        var pending = pendingSettlements
            .Select(s => new PlatformFeeReviewSettlement(
                s.Id,
                s.GroupId,
                pendingGroupNames.TryGetValue(s.GroupId, out var n) ? n : $"Grupo #{s.GroupId}",
                s.Amount,
                s.SubmittedAt,
                s.HasProof))
            .ToList();

        return new PlatformFeeReviewQueue(groups, pending);
    }

    private sealed class PlatformFeeReviewSettlementRow
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public decimal Amount { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool HasProof { get; set; }
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
    string GroupName,
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

/// <summary>Review queue for the system admin (Fase B).</summary>
public record PlatformFeeReviewQueue(
    IReadOnlyList<PlatformFeeGroupReview> Groups,
    IReadOnlyList<PlatformFeeReviewSettlement> PendingSettlements);

/// <summary>Group balance summary for the admin review queue.</summary>
public record PlatformFeeGroupReview(
    int GroupId,
    string GroupName,
    decimal Accrued,
    decimal Settled,
    decimal Due,
    decimal InAnalysis);

/// <summary>A settlement awaiting review, for the admin queue.</summary>
public record PlatformFeeReviewSettlement(
    int Id,
    int GroupId,
    string GroupName,
    decimal Amount,
    DateTime SubmittedAt,
    bool HasProof);
