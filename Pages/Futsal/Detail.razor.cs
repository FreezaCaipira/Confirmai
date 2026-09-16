using System.Security.Claims;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Helpers;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Futsal;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Futsal;

public partial class Detail
{
    [Inject] private EventDetailService EventDetailSvc { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private Event? ev;
    private bool isLoading = true;
    private string? currentUserId;
    private ApplicationUser? creatorUser;
    private FutsalPosition chosenPos = FutsalPosition.Outfield;
    private string actionError = string.Empty;
    private bool isSystemAdmin;
    private int? confirmRemoveId;
    private int? confirmPayId;
    private int eventNumber;
    private bool confirmCancel;
    private WaitingList? userWaitlistEntry;
    private bool confirmLeaveWaitlist;
    private GroupJoinRequest? userJoinRequest;
    private bool requestingJoin;
    private bool cancellingJoin;
    private string joinRequestError = string.Empty;
    private List<PostMatchVote> detailMvpVotes = new();

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        isSystemAdmin = auth.User.IsInRole("admin");
        await LoadEvent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || ev is null) return;
        var mapsKey = Config["Google:MapsApiKey"];
        if (string.IsNullOrWhiteSpace(mapsKey) || mapsKey.Contains("SET_VIA")) return;
        try { await JS.InvokeVoidAsync("ConfirmaiSetupMapFallback", $"map-wrap-{ev.Id}", $"map-fallback-{ev.Id}"); }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }

    private async Task LoadEvent()
    {
        isLoading = true;
        var result = await EventDetailSvc.LoadAsync(Id, currentUserId);
        ev = result.Event;
        creatorUser = result.CreatorUser;
        userWaitlistEntry = result.UserWaitlistEntry;
        userJoinRequest = result.UserJoinRequest;
        detailMvpVotes = result.MvpVotes;
        eventNumber = result.EventNumber;
        isLoading = false;
    }

    private async Task RequestToJoinAsync()
    {
        if (currentUserId is null || ev is null) return;
        requestingJoin = true;
        joinRequestError = string.Empty;
        try { await EventDetailSvc.RequestToJoinAsync(ev.GroupId, currentUserId); await LoadEvent(); }
        catch { joinRequestError = Ui["Futsal.JoinRequestSendError"]; }
        finally { requestingJoin = false; }
    }

    private async Task CancelJoinRequestAsync()
    {
        if (currentUserId is null || ev is null || userJoinRequest is null) return;
        cancellingJoin = true;
        joinRequestError = string.Empty;
        try { await EventDetailSvc.CancelJoinRequestAsync(userJoinRequest.Id); await LoadEvent(); }
        catch { joinRequestError = Ui["Futsal.JoinRequestCancelError"]; }
        finally { cancellingJoin = false; }
    }

    private bool IsGroupAdmin() => EventAccess.IsAdmin(ev, currentUserId);

    private async Task ConfirmAs(FutsalPosition pos) { chosenPos = pos; await ConfirmPresence(); }

    private async Task ConfirmPresence()
    {
        if (currentUserId is null || ev is null) return;
        actionError = string.Empty;
        var result = await EventDetailSvc.ConfirmPresenceAsync(Id, currentUserId, chosenPos);
        if (!result.Success) { actionError = result.Error ?? Ui["Futsal.ConfirmPresenceError"]; return; }
        await LoadEvent();
    }

    private async Task CancelConfirmation()
    {
        if (currentUserId is null) return;
        actionError = string.Empty;
        await EventDetailSvc.CancelConfirmationAsync(Id, currentUserId);
        confirmCancel = false;
        await LoadEvent();
    }

    private async Task LeaveWaitlist()
    {
        if (currentUserId is null) return;
        await EventDetailSvc.LeaveWaitlistAsync(Id, currentUserId);
        confirmLeaveWaitlist = false;
        await LoadEvent();
    }

    private async Task AdminTogglePaid(int confirmationId)
    {
        if (!IsGroupAdmin()) return;
        var result = await AdminConfirmationService.TogglePaidAsync(confirmationId, currentUserId!);
        if (result.Found && result.Updated) { confirmPayId = null; await LoadEvent(); }
    }

    private async Task AdminRemoveConfirmation(int confirmationId)
    {
        if (!IsGroupAdmin()) return;
        await EventDetailSvc.AdminRemoveConfirmationAsync(confirmationId, currentUserId, ev?.Id);
        confirmRemoveId = null;
        await LoadEvent();
    }

    private async Task AdminRemoveFromWaitlist(int waitlistId)
    {
        if (!IsGroupAdmin()) return;
        await EventDetailSvc.AdminRemoveFromWaitlistAsync(waitlistId, currentUserId, ev?.Id);
        await LoadEvent();
    }

    private async Task AdminAddOutfieldSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        await EventDetailSvc.AdminAddOutfieldSlotAsync(ev.Id);
        await LoadEvent();
    }

    private async Task AdminRemoveOutfieldSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        var minInfo = EventMinimums.Get(ev.Sport);
        var currentMaxOut = ev.MaxGoalkeepers.HasValue ? ev.MaxPlayers - ev.MaxGoalkeepers.Value : ev.MaxPlayers;
        var currentConfOut = ev.Confirmations.Count(c => c.Position != FutsalPosition.Goalkeeper);
        if (currentMaxOut <= minInfo.MinOutfield || currentMaxOut <= currentConfOut) return;
        await EventDetailSvc.AdminRemoveOutfieldSlotAsync(ev.Id);
        await LoadEvent();
    }

    private async Task AdminAddGoalkeeperSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        await EventDetailSvc.AdminAddGoalkeeperSlotAsync(ev.Id);
        await LoadEvent();
    }

    private async Task AdminRemoveGoalkeeperSlot()
    {
        if (!IsGroupAdmin() || ev is null) return;
        var minInfo = EventMinimums.Get(ev.Sport);
        var currentGk = ev.MaxGoalkeepers ?? 0;
        var currentConfGk = ev.Confirmations.Count(c => c.Position == FutsalPosition.Goalkeeper);
        if (currentGk <= minInfo.MinGoalkeepers || currentGk <= currentConfGk) return;
        await EventDetailSvc.AdminRemoveGoalkeeperSlotAsync(ev.Id, currentGk - 1);
        await LoadEvent();
    }

    private void NavigateToPaymentsModal(int confirmationId) =>
        NavigationManager.NavigateTo($"/pagamento/evento/{confirmationId}");

    // Callback wrappers for child components
    private async Task ConfirmAsCallback(FutsalPosition pos) { chosenPos = pos; await ConfirmPresence(); }
    private Task ConfirmPayIdChangedCallback(int? value) { confirmPayId = value; return Task.CompletedTask; }
    private Task ConfirmRemoveIdChangedCallback(int? value) { confirmRemoveId = value; return Task.CompletedTask; }
    private Task ConfirmCancelChangedCallback(bool value) { confirmCancel = value; return Task.CompletedTask; }
    private async Task CancelConfirmationCallback() => await CancelConfirmation();
    private async Task AdminTogglePaidCallback(int confirmationId) => await AdminTogglePaid(confirmationId);
    private async Task AdminRemoveConfirmationCallback(int confirmationId) => await AdminRemoveConfirmation(confirmationId);
    private async Task AdminAddOutfieldSlotCallback() => await AdminAddOutfieldSlot();
    private async Task AdminRemoveOutfieldSlotCallback() => await AdminRemoveOutfieldSlot();
    private async Task AdminAddGoalkeeperSlotCallback() => await AdminAddGoalkeeperSlot();
    private async Task AdminRemoveGoalkeeperSlotCallback() => await AdminRemoveGoalkeeperSlot();
    private Task NavigateToPaymentsModalCallback(int confirmationId) { NavigateToPaymentsModal(confirmationId); return Task.CompletedTask; }
}
