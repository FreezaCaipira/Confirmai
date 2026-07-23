using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
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

        IdentityResult result = isVenueManager
            ? await UserManager.RemoveFromRoleAsync(user, "venue_manager")
            : await UserManager.AddToRoleAsync(user, "venue_manager");

        if (result.Succeeded)
            isVenueManager = !isVenueManager;
        else
            roleToggleError = string.Join(" ", result.Errors.Select(e => e.Description));

        roleToggleBusy = false;
    }
}
