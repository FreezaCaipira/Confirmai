using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Index
{
    private string?      currentUserId = null;
    private List<Group>  groups        = new();
    private List<GroupJoinRequest> myPendingRequests = new();
    private bool         isLoading     = true;
    private Dictionary<int, int> pendingJoinRequestsByGroup = new();

    private int?    openMenuGroupId;
    private int?    confirmLeaveGroupId;
    private int?    copiedGroupId;
    private int?    cancellingRequestId;
    private bool    isLeaving;
    private string? leaveError;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IConfiguration Config { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private GroupDetailService GroupDetailService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LoadGroups();
    }

    private async Task LoadGroups()
    {
        if (currentUserId is null) return;
        isLoading = true;
        leaveError = null;
        await using var db = await DbFactory.CreateDbContextAsync();

        var userGroupIds = await db.GroupMembers
            .Where(m => m.UserId == currentUserId)
            .Select(m => m.GroupId)
            .Distinct()
            .ToListAsync();

        groups = userGroupIds.Count == 0
            ? new List<Group>()
            : await db.Groups
                .Where(g => userGroupIds.Contains(g.Id))
                .OrderByDescending(g => g.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin))
                .ThenBy(g => g.Name)
                .ToListAsync();

        foreach (var group in groups)
        {
            await db.Entry(group).Collection(g => g.Members).LoadAsync();
            foreach (var member in group.Members)
            {
                await db.Entry(member).Reference(m => m.User).LoadAsync();
            }
        }

        var adminGroupIds = groups
            .Where(IsCurrentUserAdmin)
            .Select(g => g.Id)
            .ToList();

        pendingJoinRequestsByGroup = adminGroupIds.Count == 0
            ? new Dictionary<int, int>()
            : await db.GroupJoinRequests
                .Where(r => adminGroupIds.Contains(r.GroupId) && r.Status == JoinRequestStatus.Pending)
                .GroupBy(r => r.GroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GroupId, x => x.Count);

        myPendingRequests = await GroupDetailService.GetMyPendingRequestsAsync(currentUserId);

        groups = groups
            .OrderByDescending(g => GetPendingRequestsCount(g.Id) > 0)
            .ThenByDescending(IsCurrentUserAdmin)
            .ThenBy(g => g.Name)
            .ToList();

        isLoading = false;
    }

    private bool IsCurrentUserAdmin(Group group)
        => currentUserId is not null && group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);

    private int GetPendingRequestsCount(int groupId)
        => pendingJoinRequestsByGroup.TryGetValue(groupId, out var pendingCount) ? pendingCount : 0;

    private bool GroupNeedsPixSetup(Group group)
        => IsCurrentUserAdmin(group)
        && !group.Members.Any(m => m.Role == GroupMemberRole.Admin && !string.IsNullOrWhiteSpace(m.User?.PixKey));

    private void ToggleMenu(int groupId)
    {
        openMenuGroupId = openMenuGroupId == groupId ? null : groupId;
        confirmLeaveGroupId = null;
    }

    private async Task CopyInvite(Group group)
    {
        if (string.IsNullOrWhiteSpace(group.InviteCode)) return;
        var baseUrl = (Config["App:BaseUrl"]?.TrimEnd('/')) ?? NavigationManager.BaseUri.TrimEnd('/');
        var inviteUrl = $"{baseUrl}/convite/{group.InviteCode}";
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", inviteUrl);
            copiedGroupId = group.Id;
            _ = ResetCopiedAfterDelay(group.Id);
        }
        catch (JSDisconnectedException) { }
    }

    private async Task ResetCopiedAfterDelay(int groupId)
    {
        await Task.Delay(2000);
        if (copiedGroupId == groupId)
        {
            copiedGroupId = null;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LeaveGroup(Group group)
    {
        if (currentUserId is null || isLeaving) return;
        isLeaving = true;
        leaveError = null;
        try
        {
            var result = await GroupDetailService.LeaveGroupAsync(group.Id, currentUserId);
            if (result == LeaveGroupResult.SoleAdmin)
            {
                leaveError = Ui["Group.SoleAdminCantLeave"];
            }
            else if (result == LeaveGroupResult.PendingPayment)
            {
                leaveError = Ui["Group.PendingPaymentCantLeave"];
            }
            else
            {
                openMenuGroupId = null;
                confirmLeaveGroupId = null;
                await LoadGroups();
            }
        }
        finally
        {
            isLeaving = false;
            confirmLeaveGroupId = null;
        }
    }

    private async Task CancelMyRequest(int requestId)
    {
        if (currentUserId is null) return;
        cancellingRequestId = requestId;
        try
        {
            await GroupDetailService.CancelJoinRequestAsync(requestId, currentUserId);
            await LoadGroups();
        }
        finally
        {
            cancellingRequestId = null;
        }
    }
}
