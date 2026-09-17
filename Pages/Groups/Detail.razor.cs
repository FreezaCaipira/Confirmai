using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Detail : IAsyncDisposable
{
    [Parameter] public int Id { get; set; }
    [Inject] private GroupDetailService GroupService { get; set; } = default!;

    private Group? group;
    private List<GroupJoinRequest> pendingRequests = new();
    private GroupJoinRequest? userJoinRequest;
    private bool isLoading = true;
    private string? currentUserId;
    private bool copiedInvite;
    private bool copiedCode;
    private bool requestingJoin;
    private bool cancellingJoin;
    private string enteredCode = string.Empty;
    private string codeError = string.Empty;
    private string requestSortOrder = "newest";
    private HashSet<int> selectedRequestIds = new();
    private bool isBulkProcessing;
    private bool showAllMembers;

    private CancellationTokenSource? _copyInviteCts;
    private CancellationTokenSource? _copyCodeCts;

    private IEnumerable<GroupJoinRequest> sortedPendingRequests => requestSortOrder switch
    {
        "newest" => pendingRequests.OrderByDescending(r => r.RequestedAt),
        "oldest" => pendingRequests.OrderBy(r => r.RequestedAt),
        "name" => pendingRequests.OrderBy(r => r.User?.FullName ?? r.User?.UserName ?? ""),
        _ => pendingRequests
    };

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
        showAllMembers = false;
        isLoading = false;
    }

    private async Task CopyInviteLink(string url)
    {
        try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", url); }
        catch { }
        _copyInviteCts?.Cancel();
        _copyInviteCts = new CancellationTokenSource();
        var ct = _copyInviteCts.Token;
        try
        {
            copiedInvite = true;
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested) copiedInvite = false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copiedInvite = false;
        }
    }

    private void ToggleMembersView() => showAllMembers = !showAllMembers;

    private async Task CopyCode(string code)
    {
        try { await JS.InvokeVoidAsync("navigator.clipboard.writeText", code); }
        catch { }
        _copyCodeCts?.Cancel();
        _copyCodeCts = new CancellationTokenSource();
        var ct = _copyCodeCts.Token;
        try
        {
            copiedCode = true;
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested) copiedCode = false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copiedCode = false;
        }
    }

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

    private async Task JoinWithCode()
    {
        if (currentUserId is null || group is null) return;
        codeError = string.Empty;
        var typed = enteredCode.Trim().ToUpperInvariant();
        var result = await GroupService.JoinWithCodeAsync(group.Id, currentUserId, typed);
        if (result == Confirmai.Services.Groups.JoinWithCodeResult.InvalidCode)
        {
            codeError = Ui["GroupEntry.InvalidCode"];
            return;
        }
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

    private async Task ApproveSelected()
    {
        if (currentUserId is null || group is null || selectedRequestIds.Count == 0) return;
        isBulkProcessing = true;
        try
        {
            await GroupService.ApproveSelectedAsync(selectedRequestIds, currentUserId, group.Name);
            selectedRequestIds.Clear();
            await LoadGroup();
        }
        finally
        {
            isBulkProcessing = false;
        }
    }

    private async Task RejectSelected()
    {
        if (currentUserId is null || selectedRequestIds.Count == 0) return;
        isBulkProcessing = true;
        try
        {
            await GroupService.RejectSelectedAsync(selectedRequestIds, currentUserId);
            selectedRequestIds.Clear();
            await LoadGroup();
        }
        finally
        {
            isBulkProcessing = false;
        }
    }

    private void ClearSelection() => selectedRequestIds.Clear();

    private void ToggleRequestSelection(int requestId, bool isSelected)
    {
        if (isSelected) selectedRequestIds.Add(requestId);
        else selectedRequestIds.Remove(requestId);
    }

    public ValueTask DisposeAsync()
    {
        _copyInviteCts?.Cancel();
        _copyCodeCts?.Cancel();
        _copyInviteCts?.Dispose();
        _copyCodeCts?.Dispose();
        return ValueTask.CompletedTask;
    }
}
