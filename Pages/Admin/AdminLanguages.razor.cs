using System.Security.Claims;
using Confirmai.Configuration;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace Confirmai.Pages.Admin;

public partial class AdminLanguages
{
    private SecurityPolicySnapshot securityPolicy = SecurityPolicyDefaults.Create(isDevelopment: false);
    private bool runtimeRequireConfirmedEmail;
    private int runtimeLockoutMaxAttempts;
    private int runtimeLockoutMinutes;
    private string? securityPolicyMessage;
    private bool securityPolicySaved;
    private ClaimsPrincipal? currentUser;

    [Inject] private AdminSecurityPolicyService AdminSecurityPolicyService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IWebHostEnvironment WebHostEnvironment { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUser = authState.User;

        securityPolicy = SecurityPolicyDefaults.Create(WebHostEnvironment.IsDevelopment());

        var runtimePolicy = await AdminSecurityPolicyService.GetRuntimePolicyForAdminAsync(currentUser);
        runtimeRequireConfirmedEmail = runtimePolicy.RequireConfirmedEmail;
        runtimeLockoutMaxAttempts = runtimePolicy.LockoutMaxFailedAccessAttempts;
        runtimeLockoutMinutes = runtimePolicy.LockoutMinutes;
    }

    private async Task SaveSecurityPolicyAsync()
    {
        try
        {
            var saved = await AdminSecurityPolicyService.SetRuntimePolicyForAdminAsync(
                currentUser,
                new RuntimeSecurityPolicy(
                    runtimeRequireConfirmedEmail,
                    runtimeLockoutMaxAttempts,
                    runtimeLockoutMinutes));

            securityPolicySaved = saved;

            if (saved)
            {
                securityPolicyMessage = T["AdminDashboard.SecurityPolicySaved"];
                var runtimePolicy = await AdminSecurityPolicyService.GetRuntimePolicyForAdminAsync(currentUser);
                runtimeRequireConfirmedEmail = runtimePolicy.RequireConfirmedEmail;
                runtimeLockoutMaxAttempts = runtimePolicy.LockoutMaxFailedAccessAttempts;
                runtimeLockoutMinutes = runtimePolicy.LockoutMinutes;
                return;
            }

            securityPolicyMessage = T["AdminDashboard.SecurityPolicyRangeError"];
        }
        catch (UnauthorizedAccessException)
        {
            securityPolicySaved = false;
            securityPolicyMessage = T["AdminDashboard.AccessDeniedSecurity"];
        }
    }
}
