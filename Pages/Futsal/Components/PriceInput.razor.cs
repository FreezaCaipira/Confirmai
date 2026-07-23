using Confirmai.Pages.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class PriceInput
{
    [Parameter]
    public Create.CreateMatchForm Form { get; set; } = new();
}
