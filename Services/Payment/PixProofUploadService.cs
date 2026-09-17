using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Confirmai.Data;
using Confirmai.Services.Core;

namespace Confirmai.Services.Payment;

/// <summary>
/// Service for validating and persisting Pix proof of payment uploads.
/// </summary>
public class PixProofUploadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<PixProofUploadService> _logger;
    private readonly LogService _log;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };

    public PixProofUploadService(IDbContextFactory<AppDbContext> factory, ILogger<PixProofUploadService> logger, LogService log)
    {
        _factory = factory;
        _logger = logger;
        _log = log;
    }

    /// <summary>
    /// Validates and uploads a Pix proof image, persisting to the database.
    /// </summary>
    /// <param name="confirmationId">EventConfirmation ID</param>
    /// <param name="fileBytes">Image file bytes</param>
    /// <param name="mimeType">Content type (MIME type)</param>
    /// <returns>Upload result with success status and message</returns>
    public async Task<PixProofUploadResult> UploadProofAsync(int confirmationId, byte[] fileBytes, string mimeType)
    {
        // Validate MIME type
        if (string.IsNullOrWhiteSpace(mimeType) || 
            !AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            return new PixProofUploadResult
            {
                Success = false,
                Message = "Apenas imagens JPG, PNG ou WebP são aceitas."
            };
        }

        // Validate file size
        if (fileBytes is null || fileBytes.Length == 0)
        {
            return new PixProofUploadResult
            {
                Success = false,
                Message = "Arquivo vazio."
            };
        }

        if (fileBytes.Length > MaxFileSizeBytes)
        {
            return new PixProofUploadResult
            {
                Success = false,
                Message = $"Arquivo muito grande. Máximo {MaxFileSizeBytes / (1024 * 1024)} MB."
            };
        }

        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var confirmation = await db.EventConfirmations.FindAsync(confirmationId);
            
            if (confirmation is null)
            {
                return new PixProofUploadResult
                {
                    Success = false,
                    Message = "Confirmação de presença não encontrada."
                };
            }

            var replaced = confirmation.PixProofImageData is { Length: > 0 };

            confirmation.PixProofImageData = fileBytes;
            confirmation.PixProofContentType = mimeType;
            confirmation.PixProofUploadedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await _log.AuditAsync(
                replaced ? AuditEvents.ProofReplaced : AuditEvents.ProofUploaded,
                AuditEntities.EventConfirmation,
                confirmationId.ToString(),
                replaced ? "Comprovante Pix substituído" : "Comprovante Pix enviado",
                actorUserId: confirmation.UserId,
                metadata: new { confirmationId, eventId = confirmation.EventId, mimeType, sizeBytes = fileBytes.Length });

            return new PixProofUploadResult
            {
                Success = true,
                Message = "Comprovante enviado com sucesso.",
                UploadedAt = confirmation.PixProofUploadedAt
            };
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Erro de I/O ao processar upload de comprovante Pix para confirmação {ConfirmationId}", confirmationId);
            return new PixProofUploadResult
            {
                Success = false,
                Message = "Erro ao processar arquivo. Tente novamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar upload de comprovante Pix para confirmação {ConfirmationId}", confirmationId);
            return new PixProofUploadResult
            {
                Success = false,
                Message = "Erro ao enviar comprovante. Tente novamente."
            };
        }
    }
}

/// <summary>
/// Result of a Pix proof upload operation.
/// </summary>
public record PixProofUploadResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public DateTime? UploadedAt { get; init; }
}
