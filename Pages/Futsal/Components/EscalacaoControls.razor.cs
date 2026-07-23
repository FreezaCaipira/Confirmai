using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class EscalacaoControls
{
    [Parameter]
    public bool TeamsAreBalanced { get; set; }

    [Parameter]
    public bool ShowConfirmModal { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public EventCallback OnRandomize { get; set; }

    [Parameter]
    public EventCallback<bool> OnShowConfirmModalChanged { get; set; }

    [Parameter]
    public EventCallback OnConfirmEscalacao { get; set; }

    private async Task HandleRandomize()
        => await OnRandomize.InvokeAsync();

    private async Task HandleShowConfirmModal(bool value)
        => await OnShowConfirmModalChanged.InvokeAsync(value);

    private async Task HandleConfirmar()
        => await OnConfirmEscalacao.InvokeAsync();
}
