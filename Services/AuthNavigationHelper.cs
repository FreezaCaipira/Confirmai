using Microsoft.AspNetCore.Components;

namespace Confirmai.Services;

public static class AuthNavigationHelper
{
    private const string LoginPath = "/Identity/Account/Login";

    public static string BuildLoginUrl(string returnUrl)
    {
        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? "/"
            : returnUrl.StartsWith('/') ? returnUrl : "/" + returnUrl;

        return $"{LoginPath}?returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
    }

    public static void NavigateToLogin(NavigationManager navigation, string returnUrl, bool replace = false)
    {
        navigation.NavigateTo(BuildLoginUrl(returnUrl), forceLoad: true, replace: replace);
    }

    public static void NavigateToLoginForCurrentUrl(NavigationManager navigation, bool replace = true)
    {
        var baseRelativePath = navigation.ToBaseRelativePath(navigation.Uri);
        var returnUrl = string.IsNullOrWhiteSpace(baseRelativePath)
            ? "/"
            : "/" + baseRelativePath.TrimStart('/');

        NavigateToLogin(navigation, returnUrl, replace);
    }
}
