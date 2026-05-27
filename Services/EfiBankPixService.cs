using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Confirmai.Services;

/// <summary>
/// Creates and checks EfiBank (Gerencianet) Pix charges via the official Pix API.
/// Authentication uses OAuth2 (client_credentials) with mandatory mTLS certificate.
/// </summary>
public sealed class EfiBankPixService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EfiBankOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EfiBankPixService> _logger;

    public EfiBankPixService(
        IHttpClientFactory httpClientFactory,
        IOptions<EfiBankOptions> options,
        IMemoryCache cache,
        ILogger<EfiBankPixService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsEnabled;

    /// <summary>
    /// Creates a Pix charge (Cob) and returns (txId, pixCopiaECola).
    /// txId is stored in EventConfirmation.PixTxId so the webhook can resolve it.
    /// </summary>
    public async Task<(string TxId, string BrCode)> CreateChargeAsync(decimal amount, int confirmationId)
    {
        if (!_options.IsEnabled)
            throw new InvalidOperationException(
                "EfiBank não está configurado. Defina EfiBank:ClientId, ClientSecret, CertificatePath e PixKey.");

        var txId  = GenerateTxId();
        var token = await GetAccessTokenAsync();

        using var http = CreateHttpClient(token);

        var amountStr = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var body = new
        {
            calendario        = new { expiracao = _options.PixExpiresInSeconds },
            valor             = new { original  = amountStr },
            chave             = _options.PixKey,
            solicitacaoPagador = $"Confirmai #{confirmationId}"
        };

        var response = await http.PutAsJsonAsync($"/v2/cob/{txId}", body);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EfiBankCobResponse>();

        if (result?.PixCopiaECola is null)
            throw new InvalidOperationException("EfiBank não retornou pixCopiaECola na resposta.");

        _logger.LogInformation(
            "EfiBank Pix criado: txId={TxId}, confirmationId={ConfirmationId}, amount={Amount}",
            txId, confirmationId, amount);

        return (txId, result.PixCopiaECola);
    }

    /// <summary>Returns true when the charge status is CONCLUIDA (payment received).</summary>
    public async Task<bool> IsChargePaidAsync(string txId)
    {
        if (!_options.IsEnabled) return false;
        try
        {
            var token = await GetAccessTokenAsync();
            using var http = CreateHttpClient(token);

            var response = await http.GetAsync($"/v2/cob/{txId}");
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<EfiBankCobResponse>();
            return string.Equals(result?.Status, "CONCLUIDA", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EfiBank: erro ao verificar status txId={TxId}", txId);
            return false;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync()
    {
        const string cacheKey = "EfiBank_AccessToken";

        if (_cache.TryGetValue(cacheKey, out string? token) && token is not null)
            return token;

        using var http = CreateHttpClient(bearerToken: null);

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token")
        {
            Content = new StringContent(
                """{"grant_type":"client_credentials"}""",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EfiBankTokenResponse>();
        token = result!.AccessToken;

        _cache.Set(cacheKey, token, TimeSpan.FromSeconds(result.ExpiresIn - 60));
        return token;
    }

    /// <summary>Creates an HttpClient with the mTLS certificate loaded.</summary>
    private HttpClient CreateHttpClient(string? bearerToken)
    {
        var handler = new HttpClientHandler();

        if (!string.IsNullOrWhiteSpace(_options.CertificatePath) &&
            File.Exists(_options.CertificatePath))
        {
            var cert = string.IsNullOrWhiteSpace(_options.CertificatePassword)
                ? X509CertificateLoader.LoadPkcs12FromFile(_options.CertificatePath, null)
                : X509CertificateLoader.LoadPkcs12FromFile(_options.CertificatePath, _options.CertificatePassword);

            handler.ClientCertificates.Add(cert);
        }

        var http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout     = TimeSpan.FromSeconds(30)
        };

        if (!string.IsNullOrWhiteSpace(bearerToken))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

        return http;
    }

    /// <summary>Generates a 32-char uppercase hex txId (within EfiBank's 26–35 char requirement).</summary>
    private static string GenerateTxId() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}

// ── Internal DTOs ─────────────────────────────────────────────────────────────

internal sealed record EfiBankTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")]   int    ExpiresIn);

internal sealed record EfiBankCobResponse(
    [property: JsonPropertyName("txid")]          string? TxId,
    [property: JsonPropertyName("status")]        string? Status,
    [property: JsonPropertyName("pixCopiaECola")] string? PixCopiaECola);
