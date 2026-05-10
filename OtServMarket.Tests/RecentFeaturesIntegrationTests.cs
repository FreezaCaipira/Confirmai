using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Confirmai.Tests;

public class RecentFeaturesIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public RecentFeaturesIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SetLanguageRoute_SetsCookie_AndRedirectsToReturnUrl()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/set-language/en-US?returnUrl=%2Fdashboard");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/dashboard", response.Headers.Location?.ToString());
        Assert.Contains(response.Headers, h =>
            h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
            && h.Value.Any(v => v.Contains("Confirmai.uiLanguage=en-US", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SetLanguageRoute_UsesDefaultLanguage_WhenLanguageIsInvalid()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/set-language/fr-FR?returnUrl=%2Fdashboard");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains(response.Headers, h =>
            h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
            && h.Value.Any(v => v.Contains("Confirmai.uiLanguage=pt-BR", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SetLanguageRoute_SanitizesUnsafeReturnUrl()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/set-language/es-ES?returnUrl=%2F%2Fevil.example");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task HomePage_RendersCookieConsentMarkup_AndCookieScriptReference()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("cookie-consent-banner", html, StringComparison.Ordinal);
        Assert.Contains("cookie-consent-modal", html, StringComparison.Ordinal);
        Assert.Contains("/js/cookie-consent.js", html, StringComparison.Ordinal);
    }
}


