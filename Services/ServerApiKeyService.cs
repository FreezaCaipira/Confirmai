using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Models;

namespace Confirmai.Services;

public class ServerApiKeyService
{
    private const string KeyPrefix = "osm_";
    private const int RawKeyLength = 32;

    private readonly AppDbContext _db;
    private readonly LogService? _log;

    public ServerApiKeyService(AppDbContext db, LogService? log = null)
    {
        _db = db;
        _log = log;
    }

    public sealed class ApiKeyCreatedResult
    {
        public bool Success { get; init; }
        public string? RawKey { get; init; }
        public ServerApiKey? Entity { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public sealed class ApiKeyView
    {
        public int Id { get; init; }
        public int ServerId { get; init; }
        public string ServerName { get; init; } = string.Empty;
        public string KeyPrefix { get; init; } = string.Empty;
        public string? Label { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastUsedAt { get; init; }
        public DateTime? RevokedAt { get; init; }
    }

    public async Task<ApiKeyCreatedResult> CreateKeyAsync(int serverId, string? label)
    {
        var server = await _db.Servers.FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            return new ApiKeyCreatedResult { Message = "Servidor não encontrado." };
        }

        var rawKey = GenerateRawKey();
        var hash = HashKey(rawKey);
        var prefix = rawKey[..Math.Min(12, rawKey.Length)];

        var entity = new ServerApiKey
        {
            ServerId = serverId,
            KeyHash = hash,
            KeyPrefix = prefix,
            Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServerApiKeys.Add(entity);
        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.ServerApiKeyIssued,
                AuditEntities.ApiKey,
                entity.Id.ToString(),
                $"API key emitida para o servidor {serverId}. Prefixo: {prefix}." + (entity.Label != null ? $" Label: {entity.Label}." : ""),
                source: AdminAuditSources.ApiKeys,
                metadata: new { entity.Id, serverId, entity.KeyPrefix, entity.Label });

        return new ApiKeyCreatedResult
        {
            Success = true,
            RawKey = rawKey,
            Entity = entity,
            Message = "API key criada com sucesso."
        };
    }

    public async Task<List<ApiKeyView>> GetKeysForServerAsync(int serverId)
    {
        return await _db.ServerApiKeys
            .AsNoTracking()
            .Include(k => k.Server)
            .Where(k => k.ServerId == serverId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyView
            {
                Id = k.Id,
                ServerId = k.ServerId,
                ServerName = k.Server.Name,
                KeyPrefix = k.KeyPrefix,
                Label = k.Label,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt,
                RevokedAt = k.RevokedAt
            })
            .ToListAsync();
    }

    public async Task<List<ApiKeyView>> GetAllKeysAsync()
    {
        return await _db.ServerApiKeys
            .AsNoTracking()
            .Include(k => k.Server)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyView
            {
                Id = k.Id,
                ServerId = k.ServerId,
                ServerName = k.Server.Name,
                KeyPrefix = k.KeyPrefix,
                Label = k.Label,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt,
                RevokedAt = k.RevokedAt
            })
            .ToListAsync();
    }

    public async Task<bool> RevokeKeyAsync(int keyId)
    {
        var key = await _db.ServerApiKeys.FirstOrDefaultAsync(k => k.Id == keyId);
        if (key == null) return false;

        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.ServerApiKeyRevoked,
                AuditEntities.ApiKey,
                key.Id.ToString(),
                $"API key revogada. Servidor: {key.ServerId}. Prefixo: {key.KeyPrefix}." + (key.Label != null ? $" Label: {key.Label}." : ""),
                source: AdminAuditSources.ApiKeys,
                level: "Warning",
                metadata: new { key.Id, key.ServerId, key.KeyPrefix, key.Label });

        return true;
    }

    public async Task<(ServerApiKey? Key, TibiaServer? Server)> ValidateKeyAsync(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey)) return (null, null);

        var hash = HashKey(rawKey);

        var key = await _db.ServerApiKeys
            .Include(k => k.Server)
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);

        if (key == null) return (null, null);

        key.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (key, key.Server);
    }

    private static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(RawKeyLength);
        return KeyPrefix + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashKey(string rawKey)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
