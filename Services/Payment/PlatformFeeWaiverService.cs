using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Administra a isenção de taxa de plataforma por grupo (V1 manual).
/// Só o sysadmin concede/renova/revoga — o organizador nunca controla.
/// Toda isenção tem prazo e toda mudança gera auditoria
/// (<see cref="AuditEvents.GroupFeeWaiverChanged"/>).
/// </summary>
public sealed class PlatformFeeWaiverService
{
    public const int MaxReasonLength = 200;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IOptions<FeeOptions> _feeOptions;
    private readonly LogService _log;

    public PlatformFeeWaiverService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions,
        LogService log)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Concede ou renova a isenção do grupo até <paramref name="untilUtc"/>
    /// (exclusivo). Prazo futuro e motivo são obrigatórios.
    /// </summary>
    public async Task<PlatformFeeWaiverResult> SetWaiverAsync(
        int groupId, string adminUserId, DateTime untilUtc, string? reason)
    {
        var now = DateTime.UtcNow;

        if (untilUtc <= now)
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.ExpirationNotFuture);

        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.ReasonRequired);

        if (trimmed.Length > MaxReasonLength)
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.ReasonTooLong);

        await using var db = await _dbFactory.CreateDbContextAsync();

        if (!await IsSystemAdminAsync(db, adminUserId))
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.NotAuthorized);

        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.GroupNotFound);

        // Renovar uma isenção ativa estende o prazo sem reabrir a janela:
        // confirmações criadas na vigência anterior continuam dentro dela.
        var active = group.PlatformFeeWaivedUntil is DateTime cur && cur > now;
        if (!active || group.PlatformFeeWaivedFrom is null)
            group.PlatformFeeWaivedFrom = now;
        group.PlatformFeeWaivedUntil = untilUtc;
        group.PlatformFeeWaiverReason = trimmed;
        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.GroupFeeWaiverChanged,
            AuditEntities.Group,
            groupId.ToString(),
            $"Isenção de taxa concedida/renovada até {untilUtc:u}: {trimmed}",
            actorUserId: adminUserId,
            metadata: new { groupId, waivedFrom = group.PlatformFeeWaivedFrom, waivedUntil = untilUtc, reason = trimmed });

        return new PlatformFeeWaiverResult(true, PlatformFeeWaiverError.None);
    }

    /// <summary>Revoga a isenção do grupo (limpa prazo e motivo).</summary>
    public async Task<PlatformFeeWaiverResult> ClearWaiverAsync(
        int groupId, string adminUserId, string? reason = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (!await IsSystemAdminAsync(db, adminUserId))
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.NotAuthorized);

        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
            return new PlatformFeeWaiverResult(false, PlatformFeeWaiverError.GroupNotFound);

        if (group.PlatformFeeWaivedUntil is null)
            return new PlatformFeeWaiverResult(true, PlatformFeeWaiverError.None);

        var previousUntil = group.PlatformFeeWaivedUntil;
        group.PlatformFeeWaivedFrom = null;
        group.PlatformFeeWaivedUntil = null;
        group.PlatformFeeWaiverReason = null;
        await db.SaveChangesAsync();

        await _log.AuditAsync(
            AuditEvents.GroupFeeWaiverChanged,
            AuditEntities.Group,
            groupId.ToString(),
            $"Isenção de taxa revogada (era até {previousUntil:u})",
            actorUserId: adminUserId,
            metadata: new { groupId, previousUntil, reason = reason?.Trim() });

        return new PlatformFeeWaiverResult(true, PlatformFeeWaiverError.None);
    }

    /// <summary>All groups (id + name) for the admin waiver group picker.</summary>
    public async Task<IReadOnlyList<(int Id, string Name)>> GetGroupsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await db.Groups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync();
        return rows.Select(r => (r.Id, r.Name)).ToList();
    }

    /// <summary>Estado de isenção de todos os grupos (ativos e expirados), para a tela do admin.</summary>
    public async Task<IReadOnlyList<GroupFeeWaiverRow>> GetWaiverOverviewAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        var rows = await db.Groups
            .AsNoTracking()
            .Where(g => g.PlatformFeeWaivedUntil != null)
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name, g.PlatformFeeWaivedUntil, g.PlatformFeeWaiverReason })
            .ToListAsync();

        return rows
            .Select(r => new GroupFeeWaiverRow(
                r.Id,
                r.Name,
                r.PlatformFeeWaivedUntil,
                r.PlatformFeeWaiverReason,
                Active: r.PlatformFeeWaivedUntil > now))
            .ToList();
    }

    /// <summary>
    /// Quanto deixou de ser cobrado no período: confirmações carimbadas com taxa 0
    /// (só existem quando o grupo estava isento ao marcar pago) × a taxa fixa atual.
    /// </summary>
    public async Task<GroupFeeWaivedStats> GetWaivedStatsAsync(DateTime? startDate, DateTime? endDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PlatformFeeAmount == 0m && c.MarkedPaidAt != null);

        if (startDate.HasValue)
            query = query.Where(c => c.MarkedPaidAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(c => c.MarkedPaidAt <= endDate.Value.AddDays(1).AddTicks(-1));

        var count = await query.CountAsync();
        var amount = count * _feeOptions.Value.ManualPlatformFeeFixed;

        return new GroupFeeWaivedStats(count, amount);
    }

    private static async Task<bool> IsSystemAdminAsync(AppDbContext db, string userId)
    {
        var adminRoleIds = await db.Roles.AsNoTracking()
            .Where(r => r.Name == "admin")
            .Select(r => r.Id)
            .ToListAsync();

        if (adminRoleIds.Count == 0) return false;

        return await db.UserRoles.AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && adminRoleIds.Contains(ur.RoleId));
    }
}

public record PlatformFeeWaiverResult(bool Success, PlatformFeeWaiverError Error);

public enum PlatformFeeWaiverError
{
    None = 0,
    NotAuthorized,
    GroupNotFound,
    ExpirationNotFuture,
    ReasonRequired,
    ReasonTooLong,
}

/// <summary>Linha da tela do admin: grupo com isenção configurada (ativa ou expirada).</summary>
public record GroupFeeWaiverRow(
    int GroupId,
    string GroupName,
    DateTime? WaivedUntil,
    string? Reason,
    bool Active);

/// <summary>Métrica "isento no período" para o AdminRevenue.</summary>
public record GroupFeeWaivedStats(int WaivedConfirmations, decimal WaivedAmount);
