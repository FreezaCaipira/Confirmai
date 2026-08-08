using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileContactsDisplay
{
    [Parameter]
    public ApplicationUser? User { get; set; }

    [Parameter]
    public bool IsOwnProfile { get; set; }
}
