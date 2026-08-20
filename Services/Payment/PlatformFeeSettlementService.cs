using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Payment;

/// <summary>
/// Handles the platform fee settlement flow (nivel 2):
/// organizador envia comprovante de repasse -> admin do sistema revisa (aprova/rejeita).
/// Espelha o fluxo do jogador (nivel 1): PixProofUploadService + AdminConfirmationService.
/// </summary>
public class PlatformFeeSettlementService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<PlatformFeeSettlementService> _logger;
    private readonly UiTextService _ui;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };

    public PlatformFeeSettlementService(
        IDbContextFactory<AppDbContext> factory,
        ILogger<PlatformFeeSettlementService> logger,
        UiTextService ui)
    {
        _factory = factory;
        _logger = logger;
        _ui = ui;
    }

    /// <summary>
    /// Creates a new settlement with proof image uploaded by the group organizer.
    /// </summary>
    public async Task<PlatformFeeSettlementResult> SubmitSettlementAsync(
        int groupId,
        string submittedByUserId,
        decimal amount,
        byte[] fileBytes,
        string mimeType,
        IReadOnlyCollection<int>? selectedEventIds = null)
    {
        if (string.IsNullOrWhiteSpace(mimeType) ||
            !AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.InvalidImageType"]
            };
        }

        if (fileBytes is null || fileBytes.Length == 0)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.EmptyFile"]
            };
        }

        if (fileBytes.Length > MaxFileSizeBytes)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui.Get("Payment.Settlement.FileTooLarge", MaxFileSizeBytes / (1024 * 1024))
            };
        }

        if (amount <= 0)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.InvalidAmount"]
            };
        }

        try
        {
            await using var db = _factory.CreateDbContext();

            // Only a group admin (the organizer who owes the fee) may declare a transfer.
            var isGroupAdmin = await db.GroupMembers.AsNoTracking().AnyAsync(m =>
                m.GroupId == groupId &&
                m.UserId == submittedByUserId &&
                m.Role == GroupMemberRole.Admin);

            if (!isGroupAdmin)
            {
                return new PlatformFeeSettlementResult
                {
                    Success = false,
                    Message = _ui["Payment.Settlement.NotGroupAdmin"]
                };
            }

            if (selectedEventIds is null || selectedEventIds.Count == 0)
            {
                return new PlatformFeeSettlementResult
                {
                    Success = false,
                    Message = _ui["Payment.Settlement.NoMatchesSelected"]
                };
            }

            var eventIds = selectedEventIds.Distinct().ToList();

            // The selected matches must belong to this group and have accrued fee.
            var accruedByEvent = await db.EventConfirmations
                .AsNoTracking()
                .Where(c => c.Event!.GroupId == groupId
                    && c.PlatformFeeAmount.HasValue
                    && eventIds.Contains(c.Event.Id))
                .GroupBy(c => c.Event!.Id)
                .Select(g => new { EventId = g.Key, Fee = g.Sum(c => c.PlatformFeeAmount!.Value) })
                .ToDictionaryAsync(g => g.EventId, g => g.Fee);

            if (accruedByEvent.Count != eventIds.Count)
            {
                return new PlatformFeeSettlementResult
                {
                    Success = false,
                    Message = _ui["Payment.Settlement.InvalidMatchSelection"]
                };
            }

            // A match awaiting review cannot be selected again — the organizer
            // would pay it twice.
            var inReview = (await db.PlatformFeeSettlementItems
                .AsNoTracking()
                .Where(i => i.Settlement.GroupId == groupId
                    && i.Settlement.Status == PlatformFeeSettlementStatus.EmAnalise)
                .Select(i => i.EventId)
                .ToListAsync())
                .ToHashSet();

            if (eventIds.Any(inReview.Contains))
            {
                return new PlatformFeeSettlementResult
                {
                    Success = false,
                    Message = _ui["Payment.Settlement.MatchAlreadyCovered"]
                };
            }

            // Fee already transferred per match. A match may be selected again only
            // for the residual left by late payers after an approved settlement.
            var coveredByEvent = await PlatformFeeCoverage.GetCoveredByEventAsync(db, groupId);

            var residualByEvent = eventIds.ToDictionary(
                eid => eid,
                eid => accruedByEvent[eid] - coveredByEvent.GetValueOrDefault(eid));

            if (residualByEvent.Values.Any(r => r <= 0))
            {
                return new PlatformFeeSettlementResult
                {
                    Success = false,
                    Message = _ui["Payment.Settlement.MatchAlreadyCovered"]
                };
            }

            // The amount must match the residual fee of the selected matches:
            // approving the settlement marks all of them as paid, so a smaller
            // amount would silently write off the difference.
            var expected = residualByEvent.Values.Sum();
            if (amount != expected)
            {
                return new PlatformFeeSettlementResult
                {
                    Success = false,
                    Message = _ui.Get("Payment.Settlement.AmountMismatch", expected.ToString("F2"))
                };
            }

            var settlement = new PlatformFeeSettlement
            {
                GroupId = groupId,
                Amount = amount,
                SubmittedByUserId = submittedByUserId,
                SubmittedAt = DateTime.UtcNow,
                ProofImageData = fileBytes,
                ProofContentType = mimeType,
                Status = PlatformFeeSettlementStatus.EmAnalise,
                Items = eventIds
                    .Select(eid => new PlatformFeeSettlementItem
                    {
                        EventId = eid,
                        FeeAmount = residualByEvent[eid]
                    })
                    .ToList()
            };

            db.PlatformFeeSettlements.Add(settlement);
            await db.SaveChangesAsync();

            return new PlatformFeeSettlementResult
            {
                Success = true,
                Message = _ui["Payment.Settlement.SubmitSuccess"],
                SettlementId = settlement.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar envio de repasse para grupo {GroupId}", groupId);
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.SubmitError"]
            };
        }
    }

    /// <summary>
    /// Admin reviews a settlement: approves (Pago) or rejects (Rejeitado).
    /// </summary>
    public async Task<PlatformFeeSettlementResult> ReviewSettlementAsync(
        int settlementId,
        string reviewerUserId,
        bool approved,
        string? note = null)
    {
        await using var db = _factory.CreateDbContext();

        // Only a system admin may settle the debt — never the organizer who owes it.
        if (!await IsSystemAdminAsync(db, reviewerUserId))
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.NotSystemAdmin"]
            };
        }

        var settlement = await db.PlatformFeeSettlements
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == settlementId);

        if (settlement is null)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.NotFound"]
            };
        }

        if (settlement.Status != PlatformFeeSettlementStatus.EmAnalise)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.AlreadyReviewed"]
            };
        }

        // A rejection must carry a reason so the organizer knows what to fix.
        if (!approved && string.IsNullOrWhiteSpace(note))
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = _ui["Payment.Settlement.RejectReasonRequired"]
            };
        }

        settlement.Status = approved
            ? PlatformFeeSettlementStatus.Pago
            : PlatformFeeSettlementStatus.Rejeitado;
        settlement.ReviewedByUserId = reviewerUserId;
        settlement.ReviewedAt = DateTime.UtcNow;
        settlement.ReviewNote = note;

        await db.SaveChangesAsync();

        return new PlatformFeeSettlementResult
        {
            Success = true,
            Message = approved
                ? _ui["Payment.Settlement.Approved"]
                : _ui["Payment.Settlement.Rejected"],
            SettlementId = settlement.Id
        };
    }

    private static async Task<bool> IsSystemAdminAsync(AppDbContext db, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var adminRoleIds = await db.Roles.AsNoTracking()
            .Where(r => r.Name == "admin")
            .Select(r => r.Id)
            .ToListAsync();

        if (adminRoleIds.Count == 0) return false;

        return await db.UserRoles.AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && adminRoleIds.Contains(ur.RoleId));
    }
}

public record PlatformFeeSettlementResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public int? SettlementId { get; init; }
}
