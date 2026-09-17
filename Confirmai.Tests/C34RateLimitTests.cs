using System.Net;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C34 Fase 3 — proves the fixed-window rate limiters actually reject excess
/// traffic on auth pages (20/min) and payment webhooks (30/min).
/// Each test class gets its own app instance, so the buckets are not shared
/// with other tests.
/// </summary>
public class C34RateLimitTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public C34RateLimitTests(IntegrationTestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthPage_RateLimited_AfterPermitLimit()
    {
        var client = _factory.CreateClient();
        HttpStatusCode? lastStatus = null;

        // "auth" policy: 20 requests/minute — the 21st+ must be rejected
        for (var i = 0; i < 25; i++)
        {
            using var resp = await client.GetAsync("/Identity/Account/ExternalLogin");
            lastStatus = resp.StatusCode;
            if (lastStatus == HttpStatusCode.TooManyRequests) break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task Webhook_RateLimited_AfterPermitLimit()
    {
        var client = _factory.CreateClient();
        HttpStatusCode? lastStatus = null;

        // "webhook" policy: 30 requests/minute
        for (var i = 0; i < 35; i++)
        {
            using var resp = await client.PostAsync("/api/webhooks/efibank/pix",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            lastStatus = resp.StatusCode;
            if (lastStatus == HttpStatusCode.TooManyRequests) break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }
}
