using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Confirmai.Tests;

public class IdentityPageModelsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public IdentityPageModelsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Identity/Account/ForgotPassword")]
    [InlineData("/Identity/Account/ForgotPasswordConfirmation")]
    [InlineData("/Identity/Account/ResendEmailConfirmation")]
    [InlineData("/Identity/Account/Logout")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    public async Task IdentityPages_RenderForAnonymousUser(string route)
    {
        var response = await _factory.CreateClient().GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_RequiresAuthenticatedUser()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Identity/Account/ChangePassword");

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized
            || response.StatusCode == HttpStatusCode.Redirect,
            $"Expected Unauthorized or Redirect, got {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenCodeIsMissing()
    {
        var response = await _factory.CreateClient().GetAsync("/Identity/Account/ResetPassword");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterConfirmation_RedirectsToLogin_WhenEmailIsMissing()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Identity/Account/RegisterConfirmation");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Identity/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RegisterConfirmation_Renders_WhenEmailIsProvided()
    {
        var response = await _factory.CreateClient().GetAsync("/Identity/Account/RegisterConfirmation?email=test@test.local");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("test@test.local", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmail_RendersPage_WhenLinkIsInvalid()
    {
        var response = await _factory.CreateClient().GetAsync("/Identity/Account/ConfirmEmail");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(html));
    }

    [Fact]
    public async Task ResendEmailConfirmation_PostRedirects_WhenUnknownEmailIsSubmitted()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var getResponse = await client.GetAsync("/Identity/Account/ResendEmailConfirmation");
        getResponse.EnsureSuccessStatusCode();

        var pageHtml = await getResponse.Content.ReadAsStringAsync();
        var verificationToken = ExtractVerificationToken(pageHtml);

        var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("Input.Email", "unknown-user@test.local"),
            KeyValuePair.Create("__RequestVerificationToken", verificationToken)
        });

        var postResponse = await client.PostAsync("/Identity/Account/ResendEmailConfirmation", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal("/Identity/Account/ResendEmailConfirmation", postResponse.Headers.Location?.OriginalString);
    }

    private static string ExtractVerificationToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase);

        Assert.True(match.Success, "Could not extract antiforgery token from identity page.");
        return match.Groups["token"].Value;
    }
}


