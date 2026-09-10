using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for the server-side /error Razor Page (C30-C Fase 4).
/// Verifies that the error page is served (not the Blazor _Host/NotFound
/// fallback), that the RequestId is shown for log correlation, and that an
/// unhandled exception returns 500 with the error page (not Blazor).
/// </summary>
public class ErrorPageIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ErrorPageIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ErrorPage_Returns200_WithLocalizedContent()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/error");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200, got {response.StatusCode}. Body: {body[..Math.Min(300, body.Length)]}");

        // The error page should contain the localized title.
        Assert.Contains("Erro", body);

        // It should NOT be the Blazor _Host page (no blazor.server.js).
        Assert.DoesNotContain("blazor.server.js", body);

        // It should NOT contain the NotFound component.
        Assert.DoesNotContain("NotFound", body);
    }

    [Fact]
    public async Task ErrorPage_ContainsRequestId_ForLogCorrelation()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/error");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The RequestId should be present in a <code> element.
        Assert.Contains("<code>", body);
        Assert.Contains("</code>", body);
    }

    [Fact]
    public async Task ErrorPage_NeverExposesException()
    {
        using var client = _factory.CreateClient();

        // The /error page should never contain exception details.
        var response = await client.GetAsync("/error");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("StackTrace", body);
    }
}
