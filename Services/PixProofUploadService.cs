using Microsoft.EntityFrameworkCore;
using Confirmai.Data;

namespace Confirmai.Services;

/// <summary>
/// Service for validating and persisting Pix proof of payment uploads.
/// </summary>
public class PixProofUploadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };

    public PixProofUploadService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
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

            confirmation.PixProofImageData = fileBytes;
            confirmation.PixProofContentType = mimeType;
            confirmation.PixProofUploadedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return new PixProofUploadResult
            {
                Success = true,
                Message = "Comprovante enviado com sucesso.",
                UploadedAt = confirmation.PixProofUploadedAt
            };
        }
        catch (IOException)
        {
            return new PixProofUploadResult
            {
                Success = false,
                Message = "Erro ao processar arquivo. Tente novamente."
            };
        }
        catch (Exception)
        {
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
