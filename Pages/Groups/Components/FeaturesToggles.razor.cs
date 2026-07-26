using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Factories;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Groups.Components;

public partial class FeaturesToggles
{
    [Parameter]
    public Group? Group { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public string SaveMessage { get; set; } = string.Empty;

    [Parameter]
    public bool SaveError { get; set; }

    [Parameter]
    public List<EventPaymentGatewayOption> AvailableGatewayOptions { get; set; } = new();

    [Parameter]
    public EventCallback OnTogglePostMatchRanking { get; set; }

    [Parameter]
    public EventCallback OnToggleBestPlayerVoting { get; set; }

    [Parameter]
    public EventCallback OnTogglePaymentGateways { get; set; }

    private async Task HandleTogglePostMatchRanking()
        => await OnTogglePostMatchRanking.InvokeAsync();

    private async Task HandleToggleBestPlayerVoting()
        => await OnToggleBestPlayerVoting.InvokeAsync();

    private async Task HandleTogglePaymentGateways()
        => await OnTogglePaymentGateways.InvokeAsync();
}
