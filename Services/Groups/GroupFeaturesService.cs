using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Groups;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

public sealed class GroupFeaturesLoadResult
{
    public Group? Group { get; init; }
    public List<GroupMember> Members { get; init; } = new();
    public List<EventPaymentGatewayOption> AvailableGatewayOptions { get; init; } = new();
    public GroupPayoutAccount? PayoutAccount { get; init; }
    public bool IsAdmin { get; init; }
    public bool IsCreator { get; init; }
}

public sealed class GroupFeaturesService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LogService _logService;
    private readonly EventPaymentGatewayFactory _gatewayFactory;

    public GroupFeaturesService(
        IDbContextFactory<AppDbContext> dbFactory,
        LogService logService,
        EventPaymentGatewayFactory gatewayFactory)
    {
        _dbFactory = dbFactory;
        _logService = logService;
        _gatewayFactory = gatewayFactory;
    }

    public async Task<GroupFeaturesLoadResult> LoadAsync(int groupId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var group = await db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        var isAdmin = currentUserId is not null && group is not null &&
                      group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        var isCreator = currentUserId is not null && group is not null && group.CreatedByUserId == currentUserId;

        var payoutAccount = await db.GroupPayoutAccounts
            .FirstOrDefaultAsync(gpa => gpa.GroupId == groupId && gpa.IsActive);

        var gatewayOptions = (await _gatewayFactory.GetAvailableAsync()).ToList();

        return new GroupFeaturesLoadResult
        {
            Group = group,
            Members = group?.Members.ToList() ?? new(),
            AvailableGatewayOptions = gatewayOptions,
            PayoutAccount = payoutAccount,
            IsAdmin = isAdmin,
            IsCreator = isCreator
        };
    }

    public async Task<bool> TogglePostMatchRankingAsync(int groupId, bool currentValue, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbGroup = await db.Groups.FindAsync(groupId);
        if (dbGroup is null) return false;

        var previous = dbGroup.EnablePostMatchRanking;
        dbGroup.EnablePostMatchRanking = !currentValue;
        await db.SaveChangesAsync();

        await _logService.AuditAsync(
            AuditEvents.GroupFeatureToggled,
            AuditEntities.Group,
            dbGroup.Id.ToString(),
            $"Configuração do grupo alterada: ranking pós-partida {(dbGroup.EnablePostMatchRanking ? "ativado" : "desativado")}",
            currentUserId,
            source: "GroupFeatures",
            metadata: new
            {
                Feature = "EnablePostMatchRanking",
                Previous = previous,
                Current = dbGroup.EnablePostMatchRanking,
                ChangedByUserId = currentUserId,
                ChangedAtUtc = DateTime.UtcNow,
            });

        return dbGroup.EnablePostMatchRanking;
    }

    public async Task<bool> ToggleBestPlayerVotingAsync(int groupId, bool currentValue, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbGroup = await db.Groups.FindAsync(groupId);
        if (dbGroup is null) return false;

        var previous = dbGroup.EnableBestPlayerVoting;
        dbGroup.EnableBestPlayerVoting = !currentValue;
        await db.SaveChangesAsync();

        await _logService.AuditAsync(
            AuditEvents.GroupFeatureToggled,
            AuditEntities.Group,
            dbGroup.Id.ToString(),
            $"Configuração do grupo alterada: votação melhor da partida {(dbGroup.EnableBestPlayerVoting ? "ativada" : "desativada")}",
            currentUserId,
            source: "GroupFeatures",
            metadata: new
            {
                Feature = "EnableBestPlayerVoting",
                Previous = previous,
                Current = dbGroup.EnableBestPlayerVoting,
                ChangedByUserId = currentUserId,
                ChangedAtUtc = DateTime.UtcNow,
            });

        return dbGroup.EnableBestPlayerVoting;
    }

    public async Task<(bool Success, bool NewValue, string Message)> TogglePaymentGatewaysAsync(
        int groupId, bool currentValue, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbGroup = await db.Groups.FindAsync(groupId);
        if (dbGroup is null) return (false, currentValue, "Grupo não encontrado.");

        var previous = dbGroup.EnablePaymentGateways;
        GroupFeatureRules.ApplyPaymentGatewaysToggle(dbGroup, !currentValue);
        await db.SaveChangesAsync();

        await _logService.AuditAsync(
            AuditEvents.GroupFeatureToggled,
            AuditEntities.Group,
            dbGroup.Id.ToString(),
            $"Configuração do grupo alterada: gateways de pagamento {(dbGroup.EnablePaymentGateways ? "ativado" : "desativado")}{(!dbGroup.EnablePaymentGateways ? " (funcionalidades adicionais desativadas automaticamente)" : "")}",
            currentUserId,
            source: "GroupFeatures",
            metadata: new
            {
                Feature = "EnablePaymentGateways",
                Previous = previous,
                Current = dbGroup.EnablePaymentGateways,
                ChangedByUserId = currentUserId,
                ChangedAtUtc = DateTime.UtcNow,
            });

        var message = dbGroup.EnablePaymentGateways
            ? "Gateways de pagamento ativados para este grupo."
            : "Gateways de pagamento desativados. Pagamento via Pix direto do organizador. Funcionalidades adicionais (ranking e votação) desativadas.";

        return (true, dbGroup.EnablePaymentGateways, message);
    }

    public async Task<(bool Success, string Message)> SetMemberRoleAsync(
        int groupId, int memberId, GroupMemberRole newRole, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbGroup = await db.Groups.FindAsync(groupId);
        if (dbGroup is null) return (false, "Grupo não encontrado.");

        if (string.IsNullOrWhiteSpace(currentUserId) || dbGroup.CreatedByUserId != currentUserId)
        {
            await _logService.AuditAsync(
                AuditEvents.GroupMemberRoleChangeDenied,
                AuditEntities.Group,
                dbGroup.Id.ToString(),
                "Tentativa negada de alterar papel de membro (somente criador pode gerir admins).",
                currentUserId,
                source: "GroupFeatures",
                level: "Warning",
                metadata: new
                {
                    GroupId = dbGroup.Id,
                    MemberId = memberId,
                    RequestedRole = newRole.ToString(),
                    ActorUserId = currentUserId,
                    OccurredAtUtc = DateTime.UtcNow,
                });

            return (false, "Somente o criador do grupo pode adicionar ou remover admins.");
        }

        if (newRole == GroupMemberRole.Member)
        {
            var adminCount = await db.GroupMembers
                .CountAsync(m => m.GroupId == groupId && m.Role == GroupMemberRole.Admin);
            if (adminCount <= 1)
                return (false, "O grupo deve ter pelo menos um admin.");
        }

        var dbMember = await db.GroupMembers.FindAsync(memberId);
        if (dbMember is null || dbMember.GroupId != groupId) return (false, "Membro não encontrado.");

        var oldRole = dbMember.Role;
        if (oldRole == newRole)
            return (true, "Nenhuma alteração de permissão foi necessária.");

        dbMember.Role = newRole;
        await db.SaveChangesAsync();

        await _logService.AuditAsync(
            AuditEvents.GroupMemberRoleChanged,
            AuditEntities.Group,
            dbGroup.Id.ToString(),
            $"Permissão de membro alterada no grupo: usuário {dbMember.UserId} de {oldRole} para {newRole}.",
            currentUserId,
            source: "GroupFeatures",
            metadata: new
            {
                GroupId = dbGroup.Id,
                GroupName = dbGroup.Name,
                TargetUserId = dbMember.UserId,
                PreviousRole = oldRole.ToString(),
                CurrentRole = newRole.ToString(),
                ChangedByUserId = currentUserId,
                ChangedAtUtc = DateTime.UtcNow,
            });

        var msg = newRole == GroupMemberRole.Admin
            ? "Membro promovido a admin."
            : "Permissão de admin removida.";

        return (true, msg);
    }

    public async Task<(bool Success, string Message)> SavePixReceiverAsync(
        int groupId, string selectedPixReceiverId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbGroup = await db.Groups.FindAsync(groupId);
        if (dbGroup is null) return (false, "Grupo não encontrado.");

        var previousReceiverId = dbGroup.PixReceiverUserId;
        var requestedReceiverId = string.IsNullOrEmpty(selectedPixReceiverId)
            ? null
            : selectedPixReceiverId;

        if (!string.IsNullOrWhiteSpace(requestedReceiverId))
        {
            var isEligible = await db.GroupMembers
                .Include(m => m.User)
                .AnyAsync(m =>
                    m.GroupId == groupId &&
                    m.UserId == requestedReceiverId &&
                    m.Role == GroupMemberRole.Admin &&
                    !string.IsNullOrWhiteSpace(m.User!.PixKey));
            if (!isEligible)
                return (false, "Escolha inválida: selecione um admin com chave Pix cadastrada.");
        }

        dbGroup.PixReceiverUserId = requestedReceiverId;
        await db.SaveChangesAsync();

        if (!string.Equals(previousReceiverId, dbGroup.PixReceiverUserId, StringComparison.Ordinal))
        {
            var ids = new[] { previousReceiverId, dbGroup.PixReceiverUserId }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Cast<string>()
                .ToList();
            var names = await db.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? u.Email ?? u.Id })
                .ToListAsync();
            var nameMap = names.ToDictionary(x => x.Id, x => x.Name);

            await _logService.AuditAsync(
                AuditEvents.GroupPixReceiverChanged,
                AuditEntities.Group,
                dbGroup.Id.ToString(),
                "Destino Pix do grupo alterado.",
                currentUserId,
                source: "GroupFeatures",
                metadata: new
                {
                    GroupId = dbGroup.Id,
                    GroupName = dbGroup.Name,
                    PreviousReceiverUserId = previousReceiverId,
                    PreviousReceiverName = previousReceiverId is not null && nameMap.TryGetValue(previousReceiverId, out var previousName) ? previousName : null,
                    CurrentReceiverUserId = dbGroup.PixReceiverUserId,
                    CurrentReceiverName = dbGroup.PixReceiverUserId is not null && nameMap.TryGetValue(dbGroup.PixReceiverUserId, out var currentName) ? currentName : null,
                    ChangedByUserId = currentUserId,
                    ChangedAtUtc = DateTime.UtcNow,
                });
        }

        return (true, "Destino Pix salvo com sucesso.");
    }

    public async Task<(bool Success, string Message)> SavePayoutAccountAsync(
        int groupId, GroupPayoutAccount formData, int? existingAccountId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (existingAccountId is null)
        {
            var newAccount = new GroupPayoutAccount
            {
                GroupId = groupId,
                PixKeyType = formData.PixKeyType,
                PixKeyValue = formData.PixKeyValue,
                BeneficiaryName = formData.BeneficiaryName,
                BeneficiaryCpf = formData.BeneficiaryCpf,
                BankAccountNumber = formData.BankAccountNumber,
                CreatedByUserId = currentUserId,
                IsActive = true
            };
            db.GroupPayoutAccounts.Add(newAccount);
            await db.SaveChangesAsync();

            await _logService.AuditAsync(
                AuditEvents.GroupPayoutAccountCreated,
                AuditEntities.Group,
                groupId.ToString(),
                $"Conta de repasse criada para o grupo: chave {formData.PixKeyType} - {formData.PixKeyValue}",
                currentUserId,
                source: "GroupFeatures",
                metadata: new
                {
                    GroupId = groupId,
                    PixKeyType = formData.PixKeyType.ToString(),
                    PixKeyValue = formData.PixKeyValue,
                    BeneficiaryName = formData.BeneficiaryName,
                    CreatedByUserId = currentUserId,
                    CreatedAtUtc = DateTime.UtcNow,
                });

            return (true, "Chave PIX de repasse salva com sucesso.");
        }
        else
        {
            var dbAccount = await db.GroupPayoutAccounts.FindAsync(existingAccountId.Value);
            if (dbAccount is null) return (false, "Conta de repasse não encontrada.");

            var previousKey = dbAccount.PixKeyValue;
            dbAccount.PixKeyType = formData.PixKeyType;
            dbAccount.PixKeyValue = formData.PixKeyValue;
            dbAccount.BeneficiaryName = formData.BeneficiaryName;
            dbAccount.BeneficiaryCpf = formData.BeneficiaryCpf;
            dbAccount.BankAccountNumber = formData.BankAccountNumber;
            await db.SaveChangesAsync();

            await _logService.AuditAsync(
                AuditEvents.GroupPayoutAccountUpdated,
                AuditEntities.Group,
                groupId.ToString(),
                $"Conta de repasse atualizada: chave alterada de {previousKey} para {formData.PixKeyValue}",
                currentUserId,
                source: "GroupFeatures",
                metadata: new
                {
                    GroupId = groupId,
                    PreviousPixKey = previousKey,
                    CurrentPixKey = formData.PixKeyValue,
                    PixKeyType = formData.PixKeyType.ToString(),
                    BeneficiaryName = formData.BeneficiaryName,
                    UpdatedByUserId = currentUserId,
                    UpdatedAtUtc = DateTime.UtcNow,
                });

            return (true, "Chave PIX de repasse salva com sucesso.");
        }
    }
}
