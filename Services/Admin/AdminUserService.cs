using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Identity;

namespace Confirmai.Services.Admin;

public enum AdminUserMutationResult
{
    Success,
    NotFound,
    Failed,
}

/// <summary>
/// Mutações administrativas de usuário (lockout, delete, papel venue_manager).
/// Extraídas das pages no C34 Fase 3b: o audit registra o <b>admin</b> como ator
/// (antes o log gravava o alvo — ou ninguém — como autor da ação).
/// </summary>
public class AdminUserService(
    UserManager<ApplicationUser> userManager,
    LogService log)
{
    private const string VenueManagerRole = "venue_manager";

    /// <summary>UC-A-08/09 — bloqueio/desbloqueio permanente de usuário.</summary>
    public async Task<AdminUserMutationResult> SetLockoutAsync(
        string targetUserId, bool locked, string? actorUserId)
    {
        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null) return AdminUserMutationResult.NotFound;

        var result = await userManager.SetLockoutEndDateAsync(
            user, locked ? DateTimeOffset.MaxValue : (DateTimeOffset?)null);
        if (!result.Succeeded) return AdminUserMutationResult.Failed;

        await log.AuditAsync(
            locked ? AuditEvents.UserLockedOut : AuditEvents.UserUnlocked,
            AuditEntities.User,
            targetUserId,
            locked ? $"Usuario {targetUserId} bloqueado pelo admin"
                   : $"Usuario {targetUserId} desbloqueado pelo admin",
            actorUserId,
            AdminAuditSources.Identity,
            locked ? "Warning" : "Info");
        return AdminUserMutationResult.Success;
    }

    /// <summary>UC-A-10 — exclusão de usuário.</summary>
    public async Task<(AdminUserMutationResult Result, string? Errors)> DeleteUserAsync(
        string targetUserId, string? actorUserId)
    {
        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null) return (AdminUserMutationResult.NotFound, null);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return (AdminUserMutationResult.Failed,
                    string.Join("; ", result.Errors.Select(e => e.Description)));

        await log.AuditAsync(
            AuditEvents.UserDeleted,
            AuditEntities.User,
            targetUserId,
            $"Usuario {targetUserId} excluido pelo admin",
            actorUserId,
            AdminAuditSources.Identity,
            "Warning");
        return (AdminUserMutationResult.Success, null);
    }

    /// <summary>UC-A-12 — concede/revoga o papel venue_manager.</summary>
    public async Task<(AdminUserMutationResult Result, bool IsManager, string? Errors)>
        ToggleVenueManagerAsync(string targetUserId, string? actorUserId)
    {
        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null) return (AdminUserMutationResult.NotFound, false, null);

        var isManager = await userManager.IsInRoleAsync(user, VenueManagerRole);
        var result = isManager
            ? await userManager.RemoveFromRoleAsync(user, VenueManagerRole)
            : await userManager.AddToRoleAsync(user, VenueManagerRole);
        if (!result.Succeeded)
            return (AdminUserMutationResult.Failed, isManager,
                    string.Join(" ", result.Errors.Select(e => e.Description)));

        await log.AuditAsync(
            isManager ? AuditEvents.UserRoleRemoved : AuditEvents.UserRoleAssigned,
            AuditEntities.User,
            targetUserId,
            $"Papel {VenueManagerRole} {(isManager ? "removido de" : "concedido a")} {user.UserName ?? targetUserId}",
            actorUserId,
            AdminAuditSources.Identity,
            metadata: new { role = VenueManagerRole });
        return (AdminUserMutationResult.Success, !isManager, null);
    }
}
