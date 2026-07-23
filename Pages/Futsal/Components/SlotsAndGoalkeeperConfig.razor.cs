using Confirmai.Pages.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class SlotsAndGoalkeeperConfig
{
    [Parameter]
    public Create.CreateMatchForm Form { get; set; } = new();

    [Parameter]
    public EventCallback OnRotateInGoalChanged { get; set; }

    private async Task HandleRotateInGoalChanged()
        => await OnRotateInGoalChanged.InvokeAsync();
}
