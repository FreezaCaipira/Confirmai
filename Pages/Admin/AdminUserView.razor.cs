using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Confirmai.Pages.Admin;

public partial class AdminUserView
{
    [Parameter] public string UserId { get; set; } = "";
    private ApplicationUser? user;
    private bool isLoading = true;
    private bool showAdminExtensions;
    private bool isVenueManager;
    private bool roleToggleBusy;
    private string roleToggleError = string.Empty;

    private string OutfitImageUrl => "https://outfit-images.ots.me/outfit.php?id=128&addons=3&head=114&body=114&legs=114&feet=114&mount=0&direction=3";

    private string DisplayName => user?.FullName?.Trim() is { Length: > 0 } name
        ? name
        : user?.UserName ?? T["Layout.UserFallback"];

    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;
    [Inject] private AdminUserService AdminUserService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        user = await UserManager.FindByIdAsync(UserId);

        if (user != null)
        {
            isVenueManager = await UserManager.IsInRoleAsync(user, "venue_manager");

            showAdminExtensions = false;
        }

        isLoading = false;
    }

    private async Task ToggleVenueManager()
    {
        if (user is null) return;
        roleToggleBusy  = true;
        roleToggleError = string.Empty;

        var auth    = await AuthStateProvider.GetAuthenticationStateAsync();
        var actorId = auth.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var (result, isManager, errors) =
            await AdminUserService.ToggleVenueManagerAsync(user.Id, actorId);

        if (result == AdminUserMutationResult.Success)
            isVenueManager = isManager;
        else
            roleToggleError = errors ?? T["AdminUserView.RoleToggleError"];

        roleToggleBusy = false;
    }
}
