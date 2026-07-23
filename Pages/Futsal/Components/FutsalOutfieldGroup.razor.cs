using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class FutsalOutfieldGroup
{
    [Parameter] public Event? Event { get; set; }
    [Parameter] public string? CurrentUserId { get; set; }
    [Parameter] public bool IsAdmin { get; set; }
    [Parameter] public bool IsPastEvent { get; set; }
    [Parameter] public bool CanConfirm { get; set; }
    [Parameter] public List<EventConfirmation> OutFieldPlayers { get; set; } = new();
    [Parameter] public int OutFieldSlots { get; set; }
    [Parameter] public int BaseOut { get; set; }
    [Parameter] public int ConfirmedOut { get; set; }
    [Parameter] public int MaxOut { get; set; }
    [Parameter] public bool IsFullOut { get; set; }
    [Parameter] public int? ConfirmRemoveId { get; set; }
    [Parameter] public int? ConfirmPayId { get; set; }
    [Parameter] public bool ConfirmCancel { get; set; }
    [Parameter] public bool DetailMvpRevealed { get; set; }
    [Parameter] public string? DetailMvpUserId { get; set; }
    [Parameter] public (int MinOutfield, int MinGoalkeepers) MinInfo { get; set; }

    [Parameter] public EventCallback<FutsalPosition> OnConfirmAs { get; set; }
    [Parameter] public EventCallback<int?> OnConfirmPayIdChange { get; set; }
    [Parameter] public EventCallback<int?> OnConfirmRemoveIdChange { get; set; }
    [Parameter] public EventCallback<bool> OnConfirmCancelChange { get; set; }
    [Parameter] public EventCallback OnCancelConfirmation { get; set; }
    [Parameter] public EventCallback<int> OnAdminTogglePaid { get; set; }
    [Parameter] public EventCallback<int> OnAdminRemoveConfirmation { get; set; }
    [Parameter] public EventCallback OnAdminAddOutfieldSlot { get; set; }
    [Parameter] public EventCallback OnAdminRemoveOutfieldSlot { get; set; }
    [Parameter] public EventCallback<int> OnNavigateToPaymentsModal { get; set; }

    private async Task HandleConfirmAs(FutsalPosition pos)
        => await OnConfirmAs.InvokeAsync(pos);

    private async Task HandleConfirmPayIdChange(int? value)
        => await OnConfirmPayIdChange.InvokeAsync(value);

    private async Task HandleConfirmRemoveIdChange(int? value)
        => await OnConfirmRemoveIdChange.InvokeAsync(value);

    private async Task HandleConfirmCancelChange(bool value)
        => await OnConfirmCancelChange.InvokeAsync(value);

    private async Task HandleCancelConfirmation()
        => await OnCancelConfirmation.InvokeAsync();

    private async Task HandleAdminTogglePaid(int confirmationId)
        => await OnAdminTogglePaid.InvokeAsync(confirmationId);

    private async Task HandleAdminRemoveConfirmation(int confirmationId)
        => await OnAdminRemoveConfirmation.InvokeAsync(confirmationId);

    private async Task HandleAdminAddOutfieldSlot()
        => await OnAdminAddOutfieldSlot.InvokeAsync();

    private async Task HandleAdminRemoveOutfieldSlot()
        => await OnAdminRemoveOutfieldSlot.InvokeAsync();

    private async Task HandleNavigateToPaymentsModal(int confirmationId)
        => await OnNavigateToPaymentsModal.InvokeAsync(confirmationId);
}
