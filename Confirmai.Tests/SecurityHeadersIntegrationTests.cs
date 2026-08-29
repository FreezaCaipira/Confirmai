using System.Net;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for the security headers middleware added by the
/// CSP-nonce + hardening pass. Verifies that public GETs include the
/// expected hardening headers and a freshly-generated nonce on every
/// response.
/// </summary>
public class SecurityHeadersIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public SecurityHeadersIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicResponse_IncludesHardeningHeaders()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/about");

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var xcto));
        Assert.Equal("nosniff", Assert.Single(xcto));

        Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var xfo));
        Assert.Equal("DENY", Assert.Single(xfo));

        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var refPol));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(refPol));

        Assert.True(response.Headers.TryGetValues("Permissions-Policy", out var permPol));
        Assert.Contains("camera=()", Assert.Single(permPol));
    }

    [Fact]
    public async Task PublicResponse_IncludesContentSecurityPolicy_WithScriptNonce()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/about");

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var cspValues));
        var csp = Assert.Single(cspValues);

        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("base-uri 'self'", csp);
        Assert.Contains("form-action 'self'", csp);

        // The external-login redirect chain ends at the Google consent screen;
        // without it in form-action the browser blocks the submit silently.
        Assert.Contains("form-action 'self' https://accounts.google.com", csp);

        // Script-src must use a per-request nonce, not 'unsafe-inline'.
        Assert.Contains("script-src 'self' 'nonce-", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
    }

    [Fact]
    public async Task ContentSecurityPolicy_GeneratesUniqueNoncePerRequest()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var responseA = await client.GetAsync("/about");
        var responseB = await client.GetAsync("/about");

        var nonceA = ExtractNonce(responseA);
        var nonceB = ExtractNonce(responseB);

        Assert.False(string.IsNullOrEmpty(nonceA));
        Assert.False(string.IsNullOrEmpty(nonceB));
        Assert.NotEqual(nonceA, nonceB);
    }

    private static string? ExtractNonce(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Content-Security-Policy", out var values))
        {
            return null;
        }

        var csp = string.Join(' ', values);
        const string marker = "'nonce-";
        var start = csp.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }
        start += marker.Length;
        var end = csp.IndexOf('\'', start);
        return end < 0 ? null : csp.Substring(start, end - start);
    }
}
