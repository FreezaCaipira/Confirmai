using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class DetailChipsRow
{
    [Parameter]
    public Event Event { get; set; } = null!;
}
