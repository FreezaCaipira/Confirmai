using System.Net;
using System.Text;
using Confirmai.Configuration;
using Confirmai.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

/// <summary>
/// Unit tests for <see cref="EfiBankPixService"/>.
/// No real network or certificate is required — an HTTP handler stub is injected.
/// </summary>
public class EfiBankPixServiceTests
{
    // ── Sandbox amount override ───────────────────────────────────────────────

    [Theory]
    [InlineData(true,  15.00, 1.00)]
    [InlineData(false, 15.00, 15.00)]
    [InlineData(false, 1.00,  1.00)]
    [InlineData(true,  0.01,  1.00)]
    public void GetEffectiveAmount_ReturnsExpected(bool sandbox, decimal input, decimal expected)
    {
        var actual = EfiBankPixService.GetEffectiveAmount(input, sandbox);
        Assert.Equal(expected, actual);
    }

    // ── CreateChargeAsync uses sandbox amount ─────────────────────────────────

    [Fact]
    public async Task CreateChargeAsync_SendsAmount1_WhenSandboxTrue()
    {
        string? capturedBody = null;

        var svc = BuildService(
            sandbox: true,
            responder: req =>
            {
                if (req.RequestUri!.PathAndQuery.Contains("/oauth/token"))
                    return TokenResponse();

                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return CobResponse("ATIVA");
            });

        await svc.CreateChargeAsync(amount: 50.00m, confirmationId: 1);

        Assert.NotNull(capturedBody);
        Assert.Contains("\"1.00\"", capturedBody);
    }

    [Fact]
    public async Task CreateChargeAsync_SendsOriginalAmount_WhenSandboxFalse()
    {
        string? capturedBody = null;

        var svc = BuildService(
            sandbox: false,
            responder: req =>
            {
                if (req.RequestUri!.PathAndQuery.Contains("/oauth/token"))
                    return TokenResponse();

                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return CobResponse("ATIVA");
            });

        await svc.CreateChargeAsync(amount: 25.50m, confirmationId: 2);

        Assert.NotNull(capturedBody);
        Assert.Contains("\"25.50\"", capturedBody);
    }

    // ── IsChargePaidAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task IsChargePaidAsync_ReturnsTrue_WhenStatusIsConcluida()
    {
        var svc = BuildService(responder: req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("/oauth/token"))
                return TokenResponse();
            return CobResponse("CONCLUIDA");
        });

        var result = await svc.IsChargePaidAsync("TXID_CONCLUIDA");

        Assert.True(result);
    }

    [Fact]
    public async Task IsChargePaidAsync_ReturnsFalse_WhenStatusIsAtiva()
    {
        var svc = BuildService(responder: req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("/oauth/token"))
                return TokenResponse();
            return CobResponse("ATIVA");
        });

        var result = await svc.IsChargePaidAsync("TXID_ATIVA");

        Assert.False(result);
    }

    [Fact]
    public async Task IsChargePaidAsync_ReturnsFalse_WhenDisabled()
    {
        var svc = BuildService(enabled: false, responder: _ => throw new InvalidOperationException("não deveria chamar HTTP"));

        var result = await svc.IsChargePaidAsync("any");

        Assert.False(result);
    }

    [Fact]
    public async Task IsChargePaidAsync_ReturnsFalse_WhenHttpReturnsError()
    {
        var svc = BuildService(responder: req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("/oauth/token"))
                return TokenResponse();
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var result = await svc.IsChargePaidAsync("TXID_ERR");

        Assert.False(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EfiBankPixService BuildService(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        bool sandbox = true,
        bool enabled = true)
    {
        var opts = new EfiBankOptions
        {
            ClientId            = "test-id",
            ClientSecret        = "test-secret",
            PixKey              = "+5511999990000",
            Sandbox             = sandbox,
            // Non-empty so IsEnabled returns true; the production handler is never used
            // because handlerFactory is injected in tests.
            CertificatePath     = "fake-cert.p12",
            PixExpiresInSeconds = 3600,
        };

        // Simulate disabled by clearing required fields.
        if (!enabled)
        {
            opts.ClientId        = string.Empty;
            opts.ClientSecret    = string.Empty;
            opts.PixKey          = string.Empty;
            opts.CertificatePath = string.Empty;
        }

        var options   = Options.Create(opts);
        var cache     = new MemoryCache(new MemoryCacheOptions());
        var logger    = NullLogger<EfiBankPixService>.Instance;
        var factory   = new StubHttpClientFactory(_ => responder(_));

        return new EfiBankPixService(factory, options, cache, logger,
            handlerFactory: () => new RoutingHandler(responder));
    }

    private static HttpResponseMessage TokenResponse() =>
        HttpTestResponses.Json("""{"access_token":"stub-token","expires_in":3600}""");

    private static HttpResponseMessage CobResponse(string status) =>
        HttpTestResponses.Json($$"""
            {
                "txid": "TXID123",
                "status": "{{status}}",
                "pixCopiaECola": "00020126580014br.gov.bcb.pix"
            }
            """);
}
