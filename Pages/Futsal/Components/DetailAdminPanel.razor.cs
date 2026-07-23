using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class DetailAdminPanel
{
    [Parameter]
    public int EventId { get; set; }

    [Parameter]
    public DateTime? LineupConfirmedAt { get; set; }

    [Parameter]
    public bool IsPastEvent { get; set; }

    [Parameter]
    public bool IsAdmin { get; set; }

    [Parameter]
    public bool IsActive { get; set; }

    [Parameter]
    public bool HasQuorum { get; set; }

    [Parameter]
    public int Lacking { get; set; }
}
