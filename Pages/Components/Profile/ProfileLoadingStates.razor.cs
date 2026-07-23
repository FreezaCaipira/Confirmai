using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Components.Profile;

public partial class ProfileLoadingStates
{
    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool UserNotFound { get; set; }
}
