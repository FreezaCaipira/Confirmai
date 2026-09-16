using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

    /// <summary>
    /// Senior review C30-C ressalva 2: prove the real path — an unhandled
    /// exception must be re-executed to /error by UseExceptionHandler, not
    /// just that GET /error renders. Runs in a Production-environment factory
    /// (where the app registers UseExceptionHandler("/error") itself) and
    /// throws from a branch covered by the same handler configuration.
    /// </summary>
    [Fact]
    public async Task UnhandledException_ReExecutesToErrorPage_Returns500()
    {
        using var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter>(new ThrowEndpointStartupFilter()));
        });
        using var client = prodFactory.CreateClient();

        var response = await client.GetAsync("/__test-throw");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // The re-executed /error page is rendered — not Blazor, no leak.
        Assert.Contains("Erro", body);
        Assert.DoesNotContain("blazor.server.js", body);
        Assert.DoesNotContain("c33-test-exception", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    /// <summary>
    /// Registers the same UseExceptionHandler("/error") the app uses in
    /// production plus a throwing branch BEFORE next(app): the exception is
    /// caught and re-executed through the full pipeline, landing on the real
    /// /error Razor Page. (Appending after next(app) would be dead code —
    /// MapFallbackToPage("/_Host") would catch the path first.)
    /// </summary>
    private sealed class ThrowEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.UseExceptionHandler("/error");
                app.Map("/__test-throw", branch =>
                    branch.Run(_ => throw new InvalidOperationException("c33-test-exception")));
                next(app);
            };
    }
}
