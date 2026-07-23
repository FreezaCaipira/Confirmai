using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Confirmai.Pages.Admin.Components;

public partial class AdminPaymentsAdvancedToolsModal
{
    private string reconcileChargeId = string.Empty;
    private int? timelineConfirmationId;
    private string statusTransitionTarget = string.Empty;
    private string statusTransitionReason = string.Empty;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public bool IsReconciling { get; set; }
    [Parameter] public bool IsStatusTransitioning { get; set; }
    [Parameter] public string ReconcileResultMessage { get; set; } = string.Empty;
    [Parameter] public bool ReconcileResultIsError { get; set; }
    [Parameter] public int? ReconcileResultConfirmationId { get; set; }
    [Parameter] public string StatusTransitionResultMessage { get; set; } = string.Empty;
    [Parameter] public bool StatusTransitionResultIsError { get; set; }
    [Parameter] public int? StatusTransitionConfirmationId { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<string> OnReconcileCharge { get; set; }
    [Parameter] public EventCallback OnDismissReconcileMessage { get; set; }
    [Parameter] public EventCallback<int> OnOpenConfirmationTimeline { get; set; }
    [Parameter] public EventCallback<(int ConfirmationId, string TargetStatus, string Reason)> OnApplyStatusTransition { get; set; }
    [Parameter] public EventCallback OnDismissStatusTransitionMessage { get; set; }

    private async Task ReconcileCharge()
    {
        await OnReconcileCharge.InvokeAsync(reconcileChargeId);
    }

    private async Task OpenConfirmationTimeline()
    {
        if (timelineConfirmationId.HasValue && timelineConfirmationId.Value > 0)
        {
            await OnOpenConfirmationTimeline.InvokeAsync(timelineConfirmationId.Value);
        }
    }

    private async Task ApplyStatusTransition()
    {
        if (StatusTransitionConfirmationId.HasValue && StatusTransitionConfirmationId.Value > 0 && !string.IsNullOrWhiteSpace(statusTransitionTarget))
        {
            await OnApplyStatusTransition.InvokeAsync((StatusTransitionConfirmationId.Value, statusTransitionTarget, statusTransitionReason));
        }
    }

    private async Task DismissReconcileMessage()
    {
        await OnDismissReconcileMessage.InvokeAsync();
    }

    private async Task DismissStatusTransitionMessage()
    {
        await OnDismissStatusTransitionMessage.InvokeAsync();
    }

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            OnClose.InvokeAsync();
        }
    }

    private string BuildConfirmationTimelineHref(int confirmationId)
    {
        return $"/payments/view/{confirmationId}";
    }
}
