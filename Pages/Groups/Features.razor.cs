using System.Security.Claims;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Confirmai.Pages.Groups;

public partial class Features
{
    [Inject] private GroupFeaturesService FeaturesService { get; set; } = default!;

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
        var result = await FeaturesService.LoadAsync(Id, currentUserId);

        group = result.Group;
        members = result.Members;
        availableGatewayOptions = result.AvailableGatewayOptions;
        payoutAccount = result.PayoutAccount;
        isAdmin = result.IsAdmin;
        isCreator = result.IsCreator;
        selectedPixReceiverId = group?.PixReceiverUserId ?? string.Empty;
        confirmPromoteMemberId = null;
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
            var newValue = await FeaturesService.TogglePostMatchRankingAsync(group.Id, group.EnablePostMatchRanking, currentUserId);
            group.EnablePostMatchRanking = newValue;
            saveMessage = newValue
                ? Ui["Group.RankingPostMatchEnabled"]
                : Ui["Group.RankingPostMatchDisabled"];
        }
        catch
        {
            saveMessage = Ui["Group.SaveError"];
            saveError   = true;
        }
        finally { isSaving = false; }
    }

    private async Task ToggleBestPlayerVoting()
    {
        if (group is null) return;
        isSaving    = true;
        saveMessage = string.Empty;
        saveError   = false;
        try
        {
            var newValue = await FeaturesService.ToggleBestPlayerVotingAsync(group.Id, group.EnableBestPlayerVoting, currentUserId);
            group.EnableBestPlayerVoting = newValue;
            saveMessage = newValue
                ? Ui["Group.BestPlayerVotingEnabled"]
                : Ui["Group.BestPlayerVotingDisabled"];
        }
        catch
        {
            saveMessage = Ui["Group.SaveError"];
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
            var (success, newValue, message) = await FeaturesService.TogglePaymentGatewaysAsync(group.Id, group.EnablePaymentGateways, currentUserId);
            if (success)
            {
                group.EnablePaymentGateways = newValue;
                if (!newValue)
                {
                    group.EnablePostMatchRanking = false;
                    group.EnableBestPlayerVoting = false;
                }
            }
            saveMessage = message;
            saveError = !success;
        }
        catch
        {
            saveMessage = Ui["Group.SaveError"];
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
            var (success, message) = await FeaturesService.SetMemberRoleAsync(group.Id, memberId, newRole, currentUserId);
            roleMessage = message;
            roleError = !success;
            confirmPromoteMemberId = null;
            if (success)
                await LoadAsync();
        }
        catch
        {
            roleMessage = Ui["Group.SaveError"];
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
            var (success, message) = await FeaturesService.SavePixReceiverAsync(group.Id, selectedPixReceiverId, currentUserId);
            pixMessage = message;
            pixError = !success;
            if (success)
                await LoadAsync();
        }
        catch
        {
            pixMessage = Ui["Group.SaveError"];
            pixError   = true;
        }
        finally { isSavingPix = false; }
    }

    // Callback wrappers for child components
    private async Task TogglePostMatchRankingCallback()
        => await TogglePostMatchRanking();

    private async Task ToggleBestPlayerVotingCallback()
        => await ToggleBestPlayerVoting();

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
            var (success, message) = await FeaturesService.SavePayoutAccountAsync(
                group.Id, formData, payoutAccount?.Id, currentUserId);
            payoutMessage = message;
            payoutError = !success;
            if (success)
                await LoadAsync();
        }
        catch
        {
            payoutMessage = Ui["Group.SaveError"];
            payoutError = true;
        }
        finally { isSavingPayout = false; }
    }

    private async Task SavePayoutAccountCallback(GroupPayoutAccount formData)
        => await SavePayoutAccount(formData);
}
