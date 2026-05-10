using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services;

public class ServerRegistrationRequestService
{
    public sealed class ReviewResult
    {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
        public int? ServerId { get; init; }
    }

    public sealed class CreateServerRegistrationRequestInput
    {
        public string Name { get; init; } = string.Empty;
        public string TibiaVersion { get; init; } = string.Empty;
        public string? WebsiteUrl { get; init; }
        public string? Region { get; init; }
        public string? RequestNote { get; init; }
    }

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly LogService? _log;

    public ServerRegistrationRequestService(AppDbContext db, IWebHostEnvironment env, LogService? log = null)
    {
        _db = db;
        _env = env;
        _log = log;
    }

    public async Task<ServerRegistrationRequest> CreateAsync(CreateServerRegistrationRequestInput input, string requesterUserId, IBrowserFile? logoFile = null)
    {
        var request = new ServerRegistrationRequest
        {
            RequestedName = input.Name.Trim(),
            TibiaVersion = input.TibiaVersion.Trim(),
            WebsiteUrl = string.IsNullOrWhiteSpace(input.WebsiteUrl) ? null : input.WebsiteUrl.Trim(),
            RequestNote = BuildRequestNote(input.Region, input.RequestNote),
            RequesterUserId = requesterUserId,
            RequestedLogoPath = logoFile == null ? null : await SaveLogoAsync(logoFile),
            Status = ServerRegistrationRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServerRegistrationRequests.Add(request);
        await _db.SaveChangesAsync();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ServerRequested,
                AuditEntities.Server,
                request.Id.ToString(),
                $"Solicitação de registro de servidor recebida: '{request.RequestedName}' (versao {request.TibiaVersion}).",
                actorUserId: requesterUserId,
                source: AdminAuditSources.ServerRegistrations,
                metadata: new { request.Id, request.RequestedName, request.TibiaVersion, request.WebsiteUrl });
        }
        return request;
    }

    public async Task<List<ServerRegistrationRequest>> GetPendingAsync()
    {
        return await _db.ServerRegistrationRequests
            .AsNoTracking()
            .Include(r => r.RequesterUser)
            .Where(r => r.Status == ServerRegistrationRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ServerRegistrationRequest>> GetRecentReviewedAsync(int take = 20)
    {
        return await _db.ServerRegistrationRequests
            .AsNoTracking()
            .Include(r => r.RequesterUser)
            .Include(r => r.ReviewerUser)
            .Include(r => r.ApprovedServer)
            .Where(r => r.Status != ServerRegistrationRequestStatus.Pending)
            .OrderByDescending(r => r.ReviewedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountPendingAsync()
    {
        return await _db.ServerRegistrationRequests.CountAsync(r => r.Status == ServerRegistrationRequestStatus.Pending);
    }

    public async Task<ReviewResult> ApproveAsync(int requestId, string reviewerUserId, string? reviewNote)
    {
        var request = await _db.ServerRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Status != ServerRegistrationRequestStatus.Pending)
        {
            return new ReviewResult
            {
                Succeeded = false,
                Message = "Solicitacao nao encontrada ou ja revisada."
            };
        }

        var normalizedName = request.RequestedName.Trim();
        var normalizedVersion = request.TibiaVersion.Trim();

        var serverAlreadyExists = await _db.Servers
            .AsNoTracking()
            .AnyAsync(s =>
                s.Name.ToLower() == normalizedName.ToLower()
                && (s.TibiaVersion ?? string.Empty).ToLower() == normalizedVersion.ToLower());

        if (serverAlreadyExists)
        {
            return new ReviewResult
            {
                Succeeded = false,
                Message = "Ja existe um servidor com esse nome e versao."
            };
        }

        if (string.IsNullOrWhiteSpace(request.RequesterUserId))
        {
            return new ReviewResult
            {
                Succeeded = false,
                Message = "O usuario solicitante foi removido e nao e possivel aprovar esta solicitacao."
            };
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var server = new TibiaServer
        {
            Name = normalizedName,
            TibiaVersion = normalizedVersion,
            Region = ExtractRegionFromRequestNote(request.RequestNote),
            WebsiteUrl = request.WebsiteUrl,
            LogoPath = request.RequestedLogoPath,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.RequesterUserId,
            PrimaryGameMasterUserId = request.RequesterUserId
        };

        _db.Servers.Add(server);
        await _db.SaveChangesAsync();

        _db.ServerMembers.Add(new ServerMember
        {
            ServerId = server.Id,
            UserId = request.RequesterUserId,
            Role = ServerMemberRole.ServerAdmin,
            CreatedAt = DateTime.UtcNow
        });

        request.Status = ServerRegistrationRequestStatus.Approved;
        request.ReviewerUserId = reviewerUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        request.ApprovedServerId = server.Id;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ServerRequestApproved,
                AuditEntities.Server,
                server.Id.ToString(),
                $"Solicitação de servidor aprovada: '{server.Name}'.",
                actorUserId: reviewerUserId,
                source: AdminAuditSources.ServerRegistrations,
                metadata: new { RequestId = requestId, ServerId = server.Id, server.Name, server.TibiaVersion, RequesterUserId = request.RequesterUserId, ReviewNote = request.ReviewNote });
        }

        return new ReviewResult
        {
            Succeeded = true,
            Message = "Solicitacao aprovada e servidor criado.",
            ServerId = server.Id
        };
    }

    public async Task<ReviewResult> RejectAsync(int requestId, string reviewerUserId, string? reviewNote)
    {
        var request = await _db.ServerRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Status != ServerRegistrationRequestStatus.Pending)
        {
            return new ReviewResult
            {
                Succeeded = false,
                Message = "Solicitacao nao encontrada ou ja revisada."
            };
        }

        request.Status = ServerRegistrationRequestStatus.Rejected;
        request.ReviewerUserId = reviewerUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Rejeitado pela administracao." : reviewNote.Trim();

        await _db.SaveChangesAsync();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ServerRequestRejected,
                AuditEntities.Server,
                requestId.ToString(),
                $"Solicitação de servidor rejeitada: '{request.RequestedName}'.",
                actorUserId: reviewerUserId,
                source: AdminAuditSources.ServerRegistrations,
                level: "Warning",
                metadata: new { RequestId = requestId, request.RequestedName, request.TibiaVersion, RequesterUserId = request.RequesterUserId, ReviewNote = request.ReviewNote });
        }
        return new ReviewResult
        {
            Succeeded = true,
            Message = "Solicitacao rejeitada."
        };
    }

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private async Task<string> SaveLogoAsync(IBrowserFile logoFile)
    {
        var ext = Path.GetExtension(logoFile.Name);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
            throw new InvalidOperationException($"Tipo de arquivo não permitido: {ext}");

        if (!AllowedContentTypes.Contains(logoFile.ContentType))
            throw new InvalidOperationException($"Tipo de conteúdo não permitido: {logoFile.ContentType}");

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "server-requests");
        if (!Directory.Exists(uploads))
        {
            Directory.CreateDirectory(uploads);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploads, fileName);

        await using var stream = File.Create(filePath);
        await logoFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(stream);

        return $"/uploads/server-requests/{fileName}";
    }

    private static string? BuildRequestNote(string? region, string? note)
    {
        var cleanRegion = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        var cleanNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (cleanRegion == null && cleanNote == null)
        {
            return null;
        }

        if (cleanRegion == null)
        {
            return cleanNote;
        }

        if (cleanNote == null)
        {
            return $"Regiao: {cleanRegion}";
        }

        return $"Regiao: {cleanRegion}\n{cleanNote}";
    }

    private static string? ExtractRegionFromRequestNote(string? requestNote)
    {
        if (string.IsNullOrWhiteSpace(requestNote))
        {
            return null;
        }

        const string prefix = "Regiao:";
        var lines = requestNote.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var region = line[prefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(region) ? null : region;
            }
        }

        return null;
    }
}
