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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class FullFlowAuthenticationIdentityScenariosIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public FullFlowAuthenticationIdentityScenariosIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_RejectsPlainUsernameIdentifier_BecauseInputRequiresEmailFormat()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "username-login-flow",
            Email = "username-login-flow@test.local",
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, "Username123!");
        Assert.True(createResult.Succeeded);

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await PostLoginAsync(client, loginIdentifier: "username-login-flow", password: "Username123!");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("Input.Email", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_UsesEnglishConfirmEmailMessage_WhenPolicyRequiresConfirmation_AndLanguageIsEnglish()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SetRuntimeSecuritySettingAsync(db, AdminSecurityPolicyService.RequireConfirmedEmailKey, "true");

        var user = new ApplicationUser
        {
            UserName = "unconfirmed-english-user",
            Email = "unconfirmed-english@test.local",
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, "Unconfirmed123!");
        Assert.True(createResult.Succeeded);

        using var client = await CreateClientWithLanguageAsync("en-US");

        var response = await PostLoginAsync(client, loginIdentifier: "unconfirmed-english@test.local", password: "Unconfirmed123!");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Login not allowed. Confirm your email before signing in.", html, StringComparison.Ordinal);
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

    private async Task<HttpClient> CreateClientWithLanguageAsync(string languageCode)
    {
        using var bootstrapClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await bootstrapClient.GetAsync($"/set-language/{Uri.EscapeDataString(languageCode)}?returnUrl=%2FIdentity%2FAccount%2FLogin");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var cookieHeader = response.Headers
            .GetValues("Set-Cookie")
            .First(v => v.Contains("Confirmai.uiLanguage=", StringComparison.Ordinal))
            .Split(';', 2)[0];

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        return client;
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string loginIdentifier, string password)
    {
        var getResponse = await client.GetAsync("/Identity/Account/Login");
        getResponse.EnsureSuccessStatusCode();

        var pageHtml = await getResponse.Content.ReadAsStringAsync();
        var verificationToken = ExtractVerificationToken(pageHtml);

        using var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("Input.Email", loginIdentifier),
            KeyValuePair.Create("Input.Password", password),
            KeyValuePair.Create("Input.RememberMe", "false"),
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


