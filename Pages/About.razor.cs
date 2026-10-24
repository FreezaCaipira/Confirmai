using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages;

public partial class About
{
    [Inject] private UiTextService T { get; set; } = default!;
}
