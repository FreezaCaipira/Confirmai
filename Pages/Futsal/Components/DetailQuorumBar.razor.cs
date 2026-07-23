using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class DetailQuorumBar
{
    [Parameter]
    public bool HasQuorum { get; set; }

    [Parameter]
    public bool IsPastEvent { get; set; }

    [Parameter]
    public int MinOutfield { get; set; } = 6;

    [Parameter]
    public int MinGoalkeepers { get; set; } = 1;
}
