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

    protected override async Task OnInitializedAsync()
    {
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
        myPendingPayments = data.Group is not null
            ? await GroupPayments.CountMyPendingAsync(Id, currentUserId)
            : 0;
        showAllMembers = false;
        isLoading = false;
    }

    private void ToggleMembersView() => showAllMembers = !showAllMembers;

    private async Task ShareInviteOnWhatsApp(string url)
    {
        var shareUrl = $"https://wa.me/?text={Uri.EscapeDataString(url)}";
        await JS.InvokeVoidAsync("open", shareUrl, "_blank", "noopener,noreferrer");
    }

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
        await GroupService.CancelJoinRequestAsync(userJoinRequest.Id);
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
