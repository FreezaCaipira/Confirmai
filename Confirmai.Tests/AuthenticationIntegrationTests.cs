using Confirmai.Services.Admin;
using System.Net;
using System.Text.RegularExpressions;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class AuthenticationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AuthenticationIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Lockout_AfterFiveFailedLoginAttempts()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.LockoutMaxAttemptsKey, "5");
        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.LockoutMinutesKey, "15");

        const string lockoutIdentity = "lockout@test.local";
        var user = new ApplicationUser
        {
            UserName = lockoutIdentity,
            Email = lockoutIdentity,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, "Lockout123!");
        Assert.True(createResult.Succeeded);

        for (int i = 0; i < 5; i++)
        {
            var failedResponse = await PostLoginFormAsync(client, lockoutIdentity, "WrongPassword!", rememberMe: false);
            Assert.Equal(HttpStatusCode.OK, failedResponse.StatusCode);
        }

        var lockedResponse = await PostLoginFormAsync(client, lockoutIdentity, "Lockout123!", rememberMe: false);
        var lockedHtml = await lockedResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, lockedResponse.StatusCode);
        Assert.Contains("conta temporariamente bloqueada", lockedHtml, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lockedUser = await verifyDb.Users.AsNoTracking().SingleAsync(u => u.UserName == lockoutIdentity);
        Assert.NotNull(lockedUser.LockoutEnd);
        Assert.True(lockedUser.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Lockout_UsesRuntimeConfiguredMaxAttempts()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.LockoutMaxAttemptsKey, "2");
        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.LockoutMinutesKey, "10");

        const string identity = "runtime-lockout@test.local";
        var user = new ApplicationUser
        {
            UserName = identity,
            Email = identity,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, "Runtime123!");
        Assert.True(createResult.Succeeded);

        var firstFail = await PostLoginFormAsync(client, identity, "WrongPassword!", rememberMe: false);
        Assert.Equal(HttpStatusCode.OK, firstFail.StatusCode);

        var secondFail = await PostLoginFormAsync(client, identity, "WrongPassword!", rememberMe: false);
        var secondHtml = await secondFail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, secondFail.StatusCode);
        Assert.Contains("conta temporariamente bloqueada", secondHtml, StringComparison.OrdinalIgnoreCase);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lockedUser = await verifyDb.Users.AsNoTracking().SingleAsync(u => u.UserName == identity);
        Assert.NotNull(lockedUser.LockoutEnd);
        Assert.True(lockedUser.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task IsNotAllowed_WhenEmailNotConfirmed_ProductionConfig()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.RequireConfirmedEmailKey, "true");

        const string unconfirmedIdentity = "unconfirmed@test.local";
        var user = new ApplicationUser { UserName = unconfirmedIdentity, Email = unconfirmedIdentity, EmailConfirmed = false };
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.CreateAsync(user, "Unconfirmed123!");

        var response = await PostLoginFormAsync(client, unconfirmedIdentity, "Unconfirmed123!", rememberMe: false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("confirme seu email", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AllowsLoginWithoutConfirmedEmail_WhenRuntimePolicyDisablesRequirement()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.RequireConfirmedEmailKey, "false");

        const string identity = "runtime-unconfirmed@test.local";
        var user = new ApplicationUser
        {
            UserName = identity,
            Email = identity,
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, "Runtime123!");
        Assert.True(createResult.Succeeded);

        var response = await PostLoginFormAsync(client, identity, "Runtime123!", rememberMe: false);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    private static async Task SetRuntimeSecuritySettingAsync(AppDbContext db, string key, string value)
    {
        var setting = await db.AppSettings.FindAsync(key);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PostLoginFormAsync(HttpClient client, string email, string password, bool rememberMe)
    {
        var getResponse = await client.GetAsync("/Identity/Account/Login");
        getResponse.EnsureSuccessStatusCode();

        var pageHtml = await getResponse.Content.ReadAsStringAsync();
        var verificationToken = ExtractVerificationToken(pageHtml);

        var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("Input.Email", email),
            KeyValuePair.Create("Input.Password", password),
            KeyValuePair.Create("Input.RememberMe", rememberMe ? "true" : "false"),
            KeyValuePair.Create("__RequestVerificationToken", verificationToken)
        });

        return await client.PostAsync("/Identity/Account/Login", form);
    }

    private static string ExtractVerificationToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase);

        Assert.True(match.Success, "Could not extract antiforgery token from login page.");
        return match.Groups["token"].Value;
    }
}


