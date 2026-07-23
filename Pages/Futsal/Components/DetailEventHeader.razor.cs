using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class DetailEventHeader
{
    [Parameter]
    public Event Event { get; set; } = null!;

    [Parameter]
    public int EventNumber { get; set; }

    [Parameter]
    public ApplicationUser? CreatorUser { get; set; }
}
