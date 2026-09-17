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
    /// Only the confirmation's owner may upload — the confirmation id is
    /// caller-supplied, so ownership must be enforced here, not in the UI.
    /// </summary>
    /// <param name="confirmationId">EventConfirmation ID</param>
    /// <param name="fileBytes">Image file bytes</param>
    /// <param name="mimeType">Content type (MIME type)</param>
    /// <param name="userId">Authenticated user performing the upload</param>
    /// <returns>Upload result with success status and error code</returns>
    public async Task<PixProofUploadResult> UploadProofAsync(int confirmationId, byte[] fileBytes, string mimeType, string userId)
    {
        if (string.IsNullOrWhiteSpace(mimeType) ||
            !AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase) ||
            fileBytes is null || fileBytes.Length == 0)
        {
            return new PixProofUploadResult { Success = false, Error = PixProofUploadError.InvalidImage };
        }

        if (fileBytes.Length > MaxFileSizeBytes)
        {
            return new PixProofUploadResult { Success = false, Error = PixProofUploadError.FileTooLarge };
        }

        // The declared MIME type is client-controlled — check the file signature.
        if (!ImageSignatureValidator.MatchesDeclaredType(fileBytes, mimeType))
        {
            return new PixProofUploadResult { Success = false, Error = PixProofUploadError.InvalidImage };
        }

        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var confirmation = await db.EventConfirmations.FindAsync(confirmationId);

            if (confirmation is null)
            {
                return new PixProofUploadResult { Success = false, Error = PixProofUploadError.NotFound };
            }

            if (confirmation.UserId != userId)
            {
                return new PixProofUploadResult { Success = false, Error = PixProofUploadError.Forbidden };
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
                Error = PixProofUploadError.None,
                UploadedAt = confirmation.PixProofUploadedAt
            };
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Erro de I/O ao processar upload de comprovante Pix para confirmação {ConfirmationId}", confirmationId);
            return new PixProofUploadResult { Success = false, Error = PixProofUploadError.IoError };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar upload de comprovante Pix para confirmação {ConfirmationId}", confirmationId);
            return new PixProofUploadResult { Success = false, Error = PixProofUploadError.Unexpected };
        }
    }
}

/// <summary>
/// Outcome code of a Pix proof upload — pages translate this, never the service.
/// </summary>
public enum PixProofUploadError
{
    None,
    /// <summary>Not a valid JPG/PNG/WebP image (bad type, empty, or signature mismatch).</summary>
    InvalidImage,
    FileTooLarge,
    NotFound,
    /// <summary>The caller does not own the confirmation.</summary>
    Forbidden,
    IoError,
    Unexpected
}

/// <summary>
/// Result of a Pix proof upload operation.
/// </summary>
public record PixProofUploadResult
{
    public required bool Success { get; init; }
    public required PixProofUploadError Error { get; init; }
    public DateTime? UploadedAt { get; init; }
}
