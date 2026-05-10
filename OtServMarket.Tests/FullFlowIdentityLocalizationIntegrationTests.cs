using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Confirmai.Tests;

public class FullFlowIdentityLocalizationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public FullFlowIdentityLocalizationIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginPage_RendersEnglishLanguageAndTexts_WhenUiLanguageCookieIsEnglish()
    {
        using var client = await CreateClientWithLanguageAsync("en-US");

        var response = await client.GetAsync("/Identity/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"en-US\">", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirmai Login", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginPage_ReturnsEnglishInvalidLoginMessage_WhenUiLanguageCookieIsEnglish()
    {
        using var client = await CreateClientWithLanguageAsync("en-US");

        var getResponse = await client.GetAsync("/Identity/Account/Login");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var token = ExtractVerificationToken(getHtml);
        using var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("Input.Email", "unknown@test.local"),
            KeyValuePair.Create("Input.Password", "wrong"),
            KeyValuePair.Create("Input.RememberMe", "false"),
            KeyValuePair.Create("__RequestVerificationToken", token)
        });

        var postResponse = await client.PostAsync("/Identity/Account/Login", form);
        var postHtml = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Contains("Invalid login.", postHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginPage_ReturnsSpanishInvalidLoginMessage_WhenUiLanguageCookieIsSpanish()
    {
        using var client = await CreateClientWithLanguageAsync("es-ES");

        var getResponse = await client.GetAsync("/Identity/Account/Login");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var token = ExtractVerificationToken(getHtml);
        using var form = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("Input.Email", "unknown@test.local"),
            KeyValuePair.Create("Input.Password", "wrong"),
            KeyValuePair.Create("Input.RememberMe", "false"),
            KeyValuePair.Create("__RequestVerificationToken", token)
        });

        var postResponse = await client.PostAsync("/Identity/Account/Login", form);
        var postHtml = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Contains("Login invalido.", postHtml, StringComparison.Ordinal);
    }

    private async Task<HttpClient> CreateClientWithLanguageAsync(string languageCode)
    {
        using var bootstrapClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await bootstrapClient.GetAsync($"/set-language/{Uri.EscapeDataString(languageCode)}?returnUrl=%2FIdentity%2FAccount%2FLogin");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/Identity/Account/Login", response.Headers.Location?.ToString());

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


