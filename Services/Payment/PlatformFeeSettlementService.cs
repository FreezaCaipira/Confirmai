using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
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
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };

    public PlatformFeeSettlementService(
        IDbContextFactory<AppDbContext> factory,
        ILogger<PlatformFeeSettlementService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new settlement with proof image uploaded by the group organizer.
    /// </summary>
    public async Task<PlatformFeeSettlementResult> SubmitSettlementAsync(
        int groupId,
        string submittedByUserId,
        decimal amount,
        byte[] fileBytes,
        string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType) ||
            !AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = "Apenas imagens JPG, PNG ou WebP são aceitas."
            };
        }

        if (fileBytes is null || fileBytes.Length == 0)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = "Arquivo vazio."
            };
        }

        if (fileBytes.Length > MaxFileSizeBytes)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = $"Arquivo muito grande. Máximo {MaxFileSizeBytes / (1024 * 1024)} MB."
            };
        }

        if (amount <= 0)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = "Valor do repasse deve ser maior que zero."
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
                    Message = "Apenas um administrador do grupo pode enviar o repasse."
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
                Status = PlatformFeeSettlementStatus.EmAnalise
            };

            db.PlatformFeeSettlements.Add(settlement);
            await db.SaveChangesAsync();

            return new PlatformFeeSettlementResult
            {
                Success = true,
                Message = "Repasse enviado com sucesso. Aguardando revisão.",
                SettlementId = settlement.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar envio de repasse para grupo {GroupId}", groupId);
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = "Erro ao enviar repasse. Tente novamente."
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
                Message = "Apenas o administrador do sistema pode confirmar o recebimento do repasse."
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
                Message = "Repasse não encontrado."
            };
        }

        if (settlement.Status != PlatformFeeSettlementStatus.EmAnalise)
        {
            return new PlatformFeeSettlementResult
            {
                Success = false,
                Message = "Este repasse já foi revisado."
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
            Message = approved ? "Repasse aprovado." : "Repasse rejeitado.",
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
