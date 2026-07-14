using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Groups;

public partial class Features
{
    [Parameter] public int Id { get; set; }

    private Group?               group         = null;
    private List<GroupMember>    members       = new();
    private List<EventPaymentGatewayOption> availableGatewayOptions = new();
    private string?              currentUserId = null;
    private bool                 isLoading     = true;
    private bool                 isAdmin       = false;
    private bool                 isCreator     = false;

    // Feature toggles
    private bool    isSaving    = false;
    private string  saveMessage = string.Empty;
    private bool    saveError   = false;

    // Role management
    private bool    isSavingRole = false;
    private string  roleMessage  = string.Empty;
    private bool    roleError    = false;
    private int?    confirmPromoteMemberId = null;

    // Pix receiver
    private bool    isSavingPix        = false;
    private string  pixMessage         = string.Empty;
    private bool    pixError           = false;
    private string  selectedPixReceiverId = string.Empty;

    // Payout account
    private bool    isSavingPayout     = false;
    private string  payoutMessage      = string.Empty;
    private bool    payoutError        = false;
    private GroupPayoutAccount? payoutAccount = null;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        await using var db = await DbFactory.CreateDbContextAsync();
        group = await db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == Id);

        isAdmin = currentUserId is not null && group is not null &&
                  group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        isCreator = currentUserId is not null && group is not null && group.CreatedByUserId == currentUserId;

        members = group?.Members.ToList() ?? new();
        availableGatewayOptions = (await EventGatewayFactory.GetAvailableAsync()).ToList();
        selectedPixReceiverId = group?.PixReceiverUserId ?? string.Empty;
        confirmPromoteMemberId = null;
        
        // Load payout account
        payoutAccount = await db.GroupPayoutAccounts
            .FirstOrDefaultAsync(gpa => gpa.GroupId == Id && gpa.IsActive);
        
        isLoading = false;
    }

    private async Task TogglePostMatchRanking()
    {
        if (group is null) return;
        isSaving    = true;
        saveMessage = string.Empty;
        saveError   = false;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var dbGroup = await db.Groups.FindAsync(group.Id);
            if (dbGroup is null) return;
            var previous = dbGroup.EnablePostMatchRanking;
            dbGroup.EnablePostMatchRanking = !group.EnablePostMatchRanking;
            await db.SaveChangesAsync();
            group.EnablePostMatchRanking = dbGroup.EnablePostMatchRanking;

            await LogService.AuditAsync(
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

            saveMessage = dbGroup.EnablePostMatchRanking
                ? "Ranking pós-partida ativado."
                : "Ranking pós-partida desativado.";
        }
        catch
        {
            saveMessage = "Erro ao salvar. Tente novamente.";
            saveError   = true;
        }
        finally { isSaving = false; }
    }

    private async Task TogglePaymentGateways()
    {
        if (group is null) return;
        isSaving    = true;
        saveMessage = string.Empty;
        saveError   = false;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var dbGroup = await db.Groups.FindAsync(group.Id);
            if (dbGroup is null) return;

            var previous = dbGroup.EnablePaymentGateways;
            dbGroup.EnablePaymentGateways = !group.EnablePaymentGateways;
            await db.SaveChangesAsync();
            group.EnablePaymentGateways = dbGroup.EnablePaymentGateways;

            await LogService.AuditAsync(
                AuditEvents.GroupFeatureToggled,
                AuditEntities.Group,
                dbGroup.Id.ToString(),
                $"Configuração do grupo alterada: gateways de pagamento {(dbGroup.EnablePaymentGateways ? "ativado" : "desativado")}",
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

            saveMessage = dbGroup.EnablePaymentGateways
                ? "Gateways de pagamento ativados para este grupo."
                : "Gateways de pagamento desativados. Fluxo padrão com Pix direto mantido.";
        }
        catch
        {
            saveMessage = "Erro ao salvar. Tente novamente.";
            saveError   = true;
        }
        finally { isSaving = false; }
    }

    private async Task SetMemberRole(int memberId, GroupMemberRole newRole)
    {
        if (group is null) return;
        isSavingRole = true;
        roleMessage  = string.Empty;
        roleError    = false;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();

            var dbGroup = await db.Groups.FindAsync(group.Id);
            if (dbGroup is null) return;

            if (string.IsNullOrWhiteSpace(currentUserId) || dbGroup.CreatedByUserId != currentUserId)
            {
                roleMessage = "Somente o criador do grupo pode adicionar ou remover admins.";
                roleError   = true;

                await LogService.AuditAsync(
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
                return;
            }

            // Safety: cannot remove last admin
            if (newRole == GroupMemberRole.Member)
            {
                var adminCount = await db.GroupMembers
                    .CountAsync(m => m.GroupId == group.Id && m.Role == GroupMemberRole.Admin);
                if (adminCount <= 1)
                {
                    roleMessage = "O grupo deve ter pelo menos um admin.";
                    roleError   = true;
                    return;
                }
            }

            var dbMember = await db.GroupMembers.FindAsync(memberId);
            if (dbMember is null || dbMember.GroupId != group.Id) return;
            var oldRole = dbMember.Role;
            if (oldRole == newRole)
            {
                roleMessage = "Nenhuma alteração de permissão foi necessária.";
                confirmPromoteMemberId = null;
                return;
            }
            dbMember.Role = newRole;
            await db.SaveChangesAsync();

            await LogService.AuditAsync(
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

            roleMessage = newRole == GroupMemberRole.Admin
                ? "Membro promovido a admin."
                : "Permissão de admin removida.";
            confirmPromoteMemberId = null;
            await LoadAsync();
        }
        catch
        {
            roleMessage = "Erro ao salvar. Tente novamente.";
            roleError   = true;
        }
        finally
        {
            isSavingRole = false;
            if (roleError)
                confirmPromoteMemberId = null;
        }
    }

    private async Task SavePixReceiver()
    {
        if (group is null) return;
        isSavingPix = true;
        pixMessage  = string.Empty;
        pixError    = false;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var dbGroup = await db.Groups.FindAsync(group.Id);
            if (dbGroup is null) return;

            var previousReceiverId = dbGroup.PixReceiverUserId;
            var requestedReceiverId = string.IsNullOrEmpty(selectedPixReceiverId)
                ? null
                : selectedPixReceiverId;

            if (!string.IsNullOrWhiteSpace(requestedReceiverId))
            {
                var isEligible = await db.GroupMembers
                    .Include(m => m.User)
                    .AnyAsync(m =>
                        m.GroupId == group.Id &&
                        m.UserId == requestedReceiverId &&
                        m.Role == GroupMemberRole.Admin &&
                        !string.IsNullOrWhiteSpace(m.User!.PixKey));
                if (!isEligible)
                {
                    pixMessage = "Escolha inválida: selecione um admin com chave Pix cadastrada.";
                    pixError = true;
                    return;
                }
            }

            dbGroup.PixReceiverUserId = requestedReceiverId;
            await db.SaveChangesAsync();
            group.PixReceiverUserId = dbGroup.PixReceiverUserId;

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

                await LogService.AuditAsync(
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

            pixMessage = "Destino Pix salvo com sucesso.";
        }
        catch
        {
            pixMessage = "Erro ao salvar. Tente novamente.";
            pixError   = true;
        }
        finally { isSavingPix = false; }
    }

    // Callback wrappers for child components
    private async Task TogglePostMatchRankingCallback()
        => await TogglePostMatchRanking();

    private async Task TogglePaymentGatewaysCallback()
        => await TogglePaymentGateways();

    private async Task SetMemberRoleCallback((int memberId, GroupMemberRole newRole) args)
        => await SetMemberRole(args.memberId, args.newRole);

    private Task InitiatePromoteCallback(int memberId)
    { confirmPromoteMemberId = memberId; return Task.CompletedTask; }

    private Task CancelPromoteCallback()
    { confirmPromoteMemberId = null; return Task.CompletedTask; }

    private async Task SavePixReceiverCallback(string selectedId)
    {
        selectedPixReceiverId = selectedId;
        await SavePixReceiver();
    }

    private async Task SavePayoutAccount(GroupPayoutAccount formData)
    {
        if (group is null) return;
        isSavingPayout = true;
        payoutMessage = string.Empty;
        payoutError = false;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            
            if (payoutAccount is null)
            {
                // Create new payout account
                var newAccount = new GroupPayoutAccount
                {
                    GroupId = group.Id,
                    PixKeyType = formData.PixKeyType,
                    PixKeyValue = formData.PixKeyValue,
                    BeneficiaryName = formData.BeneficiaryName,
                    CreatedByUserId = currentUserId,
                    IsActive = true
                };
                db.GroupPayoutAccounts.Add(newAccount);
                await db.SaveChangesAsync();
                payoutAccount = newAccount;
                
                await LogService.AuditAsync(
                    AuditEvents.GroupPayoutAccountCreated,
                    AuditEntities.Group,
                    group.Id.ToString(),
                    $"Conta de repasse criada para o grupo: chave {formData.PixKeyType} - {formData.PixKeyValue}",
                    currentUserId,
                    source: "GroupFeatures",
                    metadata: new
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        PixKeyType = formData.PixKeyType.ToString(),
                        PixKeyValue = formData.PixKeyValue,
                        BeneficiaryName = formData.BeneficiaryName,
                        CreatedByUserId = currentUserId,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
            }
            else
            {
                // Update existing payout account
                var dbAccount = await db.GroupPayoutAccounts.FindAsync(payoutAccount.Id);
                if (dbAccount is null) return;
                
                var previousKey = dbAccount.PixKeyValue;
                dbAccount.PixKeyType = formData.PixKeyType;
                dbAccount.PixKeyValue = formData.PixKeyValue;
                dbAccount.BeneficiaryName = formData.BeneficiaryName;
                await db.SaveChangesAsync();
                payoutAccount = dbAccount;
                
                await LogService.AuditAsync(
                    AuditEvents.GroupPayoutAccountUpdated,
                    AuditEntities.Group,
                    group.Id.ToString(),
                    $"Conta de repasse atualizada: chave alterada de {previousKey} para {formData.PixKeyValue}",
                    currentUserId,
                    source: "GroupFeatures",
                    metadata: new
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        PreviousPixKey = previousKey,
                        CurrentPixKey = formData.PixKeyValue,
                        PixKeyType = formData.PixKeyType.ToString(),
                        BeneficiaryName = formData.BeneficiaryName,
                        UpdatedByUserId = currentUserId,
                        UpdatedAtUtc = DateTime.UtcNow,
                    });
            }
            
            payoutMessage = "Chave PIX de repasse salva com sucesso.";
        }
        catch
        {
            payoutMessage = "Erro ao salvar. Tente novamente.";
            payoutError = true;
        }
        finally { isSavingPayout = false; }
    }

    private async Task SavePayoutAccountCallback(GroupPayoutAccount formData)
        => await SavePayoutAccount(formData);
}
