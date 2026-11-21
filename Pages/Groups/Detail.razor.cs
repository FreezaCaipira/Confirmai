using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Detail
{
    [Parameter] public int Id { get; set; }
    [Inject] private GroupDetailService GroupService { get; set; } = default!;
    [Inject] private GroupPaymentsService GroupPayments { get; set; } = default!;

    private Group? group;
    private List<GroupJoinRequest> pendingRequests = new();
    private GroupJoinRequest? userJoinRequest;
    private bool isLoading = true;
    private string? currentUserId;
    private bool requestingJoin;
    private bool cancellingJoin;
    private bool isBulkProcessing;
    private bool showAllMembers;
    private int myPendingPayments;
    private int upcomingEventsCount;
    private Event? nextEvent;
    private bool hasAnyEvent;

    private int? loadedId;

    // C46 (1.2): load on OnParametersSetAsync — Blazor reuses the component
    // instance when only the route Id changes (back/forward between groups),
    // so OnInitializedAsync alone would keep showing the previous group's data.
    protected override async Task OnParametersSetAsync()
    {
        if (loadedId == Id) return;
        loadedId = Id;

        group = null;
        pendingRequests = new();
        userJoinRequest = null;
        isLoading = true;
        requestingJoin = false;
        cancellingJoin = false;
        isBulkProcessing = false;
        showAllMembers = false;
        myPendingPayments = 0;
        upcomingEventsCount = 0;
        nextEvent = null;
        hasAnyEvent = false;

        currentUserId = await GroupService.GetCurrentUserIdAsync();
        await LoadGroup();
    }

    private async Task LoadGroup()
    {
        isLoading = true;
        var data = await GroupService.LoadAsync(Id, currentUserId);
        group = data.Group;
        pendingRequests = data.PendingRequests;
        userJoinRequest = data.UserJoinRequest;
        upcomingEventsCount = data.UpcomingEventsCount;
        nextEvent = data.NextEvent;
        hasAnyEvent = data.HasAnyEvent;
        myPendingPayments = data.Group is not null
            ? await GroupPayments.CountMyPendingAsync(Id, currentUserId)
            : 0;
        showAllMembers = false;
        isLoading = false;
    }

    private void ToggleMembersView() => showAllMembers = !showAllMembers;

    private bool showInviteModal;

    private async Task RequestToJoin()
    {
        if (currentUserId is null || group is null) return;
        requestingJoin = true;
        await GroupService.RequestToJoinAsync(group.Id, currentUserId);
        requestingJoin = false;
        await LoadGroup();
    }

    private async Task CancelJoinRequest()
    {
        if (currentUserId is null || group is null || userJoinRequest is null) return;
        cancellingJoin = true;
        await GroupService.CancelJoinRequestAsync(userJoinRequest.Id, currentUserId);
        cancellingJoin = false;
        await LoadGroup();
    }

    private async Task ApproveRequest(int requestId)
    {
        if (currentUserId is null || group is null) return;
        await GroupService.ApproveRequestAsync(requestId, currentUserId, group.Name);
        await LoadGroup();
    }

    private async Task RejectRequest(int requestId)
    {
        if (currentUserId is null) return;
        await GroupService.RejectRequestAsync(requestId, currentUserId);
        await LoadGroup();
    }

    private async Task ApproveAllRequests()
    {
        if (currentUserId is null || group is null) return;
        isBulkProcessing = true;
        try
        {
            await GroupService.ApproveAllPendingAsync(group.Id, currentUserId);
            await LoadGroup();
        }
        finally
        {
            isBulkProcessing = false;
        }
    }
}
