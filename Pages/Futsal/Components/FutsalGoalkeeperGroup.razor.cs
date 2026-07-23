using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class FutsalGoalkeeperGroup
{
    [Parameter] public Event? Event { get; set; }
    [Parameter] public string? CurrentUserId { get; set; }
    [Parameter] public bool IsAdmin { get; set; }
    [Parameter] public bool IsPastEvent { get; set; }
    [Parameter] public bool CanConfirm { get; set; }
    [Parameter] public List<EventConfirmation> GoalkeeperPlayers { get; set; } = new();
    [Parameter] public int ConfirmedGk { get; set; }
    [Parameter] public int? ConfirmRemoveId { get; set; }
    [Parameter] public bool ConfirmCancel { get; set; }
    [Parameter] public bool DetailMvpRevealed { get; set; }
    [Parameter] public string? DetailMvpUserId { get; set; }
    [Parameter] public (int MinOutfield, int MinGoalkeepers) MinInfo { get; set; }
    [Parameter] public bool IsFullGk { get; set; }

    [Parameter] public EventCallback<FutsalPosition> OnConfirmAs { get; set; }
    [Parameter] public EventCallback<int?> OnConfirmRemoveIdChange { get; set; }
    [Parameter] public EventCallback<bool> OnConfirmCancelChange { get; set; }
    [Parameter] public EventCallback OnCancelConfirmation { get; set; }
    [Parameter] public EventCallback<int> OnAdminRemoveConfirmation { get; set; }
    [Parameter] public EventCallback OnAdminAddGoalkeeperSlot { get; set; }
    [Parameter] public EventCallback OnAdminRemoveGoalkeeperSlot { get; set; }

    private async Task HandleConfirmAs(FutsalPosition pos)
        => await OnConfirmAs.InvokeAsync(pos);

    private async Task HandleConfirmRemoveIdChange(int? value)
        => await OnConfirmRemoveIdChange.InvokeAsync(value);

    private async Task HandleConfirmCancelChange(bool value)
        => await OnConfirmCancelChange.InvokeAsync(value);

    private async Task HandleCancelConfirmation()
        => await OnCancelConfirmation.InvokeAsync();

    private async Task HandleAdminRemoveConfirmation(int confirmationId)
        => await OnAdminRemoveConfirmation.InvokeAsync(confirmationId);

    private async Task HandleAdminAddGoalkeeperSlot()
        => await OnAdminAddGoalkeeperSlot.InvokeAsync();

    private async Task HandleAdminRemoveGoalkeeperSlot()
        => await OnAdminRemoveGoalkeeperSlot.InvokeAsync();
}
