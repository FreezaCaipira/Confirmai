using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Interface for Pix payout services.
/// </summary>
public interface IPixPayoutService
{
    /// <summary>
    /// Sends a Pix payout to the specified Pix key.
    /// </summary>
    /// <param name="amount">Amount to send (base amount without fee)</param>
    /// <param name="pixKey">Recipient's Pix key</param>
    /// <param name="description">Payment description</param>
    /// <param name="idempotencyKey">Deterministic key to prevent duplicate payouts (derived from confirmationId + txId)</param>
    /// <returns>EndToEndId for tracking the payout</returns>
    Task<string> SendPayoutAsync(decimal amount, string pixKey, string description, string idempotencyKey);
}

/// <summary>
/// Sends Pix payouts via EfiBank "Envio e Pagamento Pix" API.
/// Used to automatically transfer the base amount to the organizer's Pix key
/// after receiving a payment (with fee retained by the platform).
/// </summary>
public sealed class EfiBankPixPayoutService : IPixPayoutService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EfiBankOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EfiBankPixPayoutService> _logger;
    private readonly Func<HttpMessageHandler>? _handlerFactory;

    public EfiBankPixPayoutService(
        IHttpClientFactory httpClientFactory,
        IOptions<EfiBankOptions> options,
        IMemoryCache cache,
        ILogger<EfiBankPixPayoutService> logger)
        : this(httpClientFactory, options, cache, logger, handlerFactory: null) { }

    /// <summary>Test-only constructor that injects a custom <see cref="HttpMessageHandler"/> factory.</summary>
    internal EfiBankPixPayoutService(
        IHttpClientFactory httpClientFactory,
        IOptions<EfiBankOptions> options,
        IMemoryCache cache,
        ILogger<EfiBankPixPayoutService> logger,
        Func<HttpMessageHandler>? handlerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _handlerFactory = handlerFactory;
    }

    public bool IsEnabled => _options.IsEnabled;

    /// <summary>
    /// Sends a Pix payout to the specified Pix key.
    /// </summary>
    /// <param name="amount">Amount to send (base amount without fee)</param>
    /// <param name="pixKey">Recipient's Pix key (organizer)</param>
    /// <param name="description">Payment description</param>
    /// <param name="idempotencyKey">Deterministic key to prevent duplicate payouts (derived from confirmationId + txId)</param>
    /// <returns>EndToEndId for tracking the payout</returns>
    public async Task<string> SendPayoutAsync(decimal amount, string pixKey, string description, string idempotencyKey)
    {
        if (!_options.IsEnabled)
            throw new InvalidOperationException(
                "EfiBank não está configurado. Defina EfiBank:ClientId, ClientSecret, CertificatePath e PixKey.");

        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (string.IsNullOrWhiteSpace(pixKey))
            throw new ArgumentException("Pix key is required", nameof(pixKey));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required", nameof(idempotencyKey));

        var token = await GetAccessTokenAsync();
        using var http = CreateHttpClient(token);

        var amountStr = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var txId = GenerateTxId(idempotencyKey);

        var body = new
        {
            valor = amountStr,
            pagador = new
            {
                chave = _options.PixKey,
                infoPagador = description ?? "Repasse Confirmai"
            },
            favorecido = new
            {
                chave = pixKey
            }
        };

        var response = await http.PutAsJsonAsync($"/v2/gn/pix/{txId}", body);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "EfiBank: falha ao enviar Pix. Status={Status}, Body={Body}",
                response.StatusCode, errorContent);
            throw new InvalidOperationException(
                $"Falha ao enviar Pix: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<EfiBankPayoutResponse>();

        var endToEndId = result?.EndToEndId;
        if (string.IsNullOrWhiteSpace(endToEndId))
            throw new InvalidOperationException("EfiBank não retornou e2eId na resposta.");

        _logger.LogInformation(
            "EfiBank Pix enviado: endToEndId={EndToEndId}, amount={Amount}, pixKey={PixKey}, txId={TxId}",
            endToEndId, amount, pixKey, txId);

        return endToEndId;
    }

    // ── Private helpers (reused from EfiBankPixService) ─────────────────────

    private async Task<string> GetAccessTokenAsync()
    {
        const string cacheKey = "EfiBank_AccessToken_Payout";

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

        try
        {
            var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EfiBankTokenResponse>();
            token = result!.AccessToken;

            _cache.Set(cacheKey, token, TimeSpan.FromSeconds(result.ExpiresIn - 60));
            return token;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "EfiBank: falha ao obter token OAuth. URL={Url}, InnerException={Inner}",
                _options.BaseUrl + "/oauth/token", ex.InnerException?.Message);
            throw;
        }
    }

    private HttpClient CreateHttpClient(string? bearerToken)
    {
        var handler = _handlerFactory?.Invoke() ?? CreateProductionHandler();
        return BuildHttpClient(handler, bearerToken);
    }

    private HttpMessageHandler CreateProductionHandler()
    {
        var handler = new HttpClientHandler();

        // Try loading from file first
        if (!string.IsNullOrWhiteSpace(_options.CertificatePath) &&
            File.Exists(_options.CertificatePath))
        {
            var password = _options.CertificatePassword ?? string.Empty;
            var cert = X509CertificateLoader.LoadPkcs12FromFile(
                _options.CertificatePath,
                password,
                X509KeyStorageFlags.UserKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);

            handler.ClientCertificates.Add(cert);
            _logger.LogDebug("EfiBank: certificado carregado (arquivo) — Subject={Subject}, HasPrivateKey={HasKey}",
                cert.Subject, cert.HasPrivateKey);
            return handler;
        }

        // Try loading from base64
        if (!string.IsNullOrWhiteSpace(_options.CertificateBase64))
        {
            try
            {
                var certBytes = Convert.FromBase64String(_options.CertificateBase64);
                var password = _options.CertificatePassword ?? string.Empty;
                var cert = X509CertificateLoader.LoadPkcs12(
                    certBytes,
                    password,
                    X509KeyStorageFlags.UserKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);

                handler.ClientCertificates.Add(cert);
                _logger.LogDebug("EfiBank: certificado carregado (base64) — Subject={Subject}, HasPrivateKey={HasKey}",
                    cert.Subject, cert.HasPrivateKey);
                return handler;
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                _logger.LogError(ex, "EfiBank: falha ao carregar certificado (base64) — Length={Length} chars",
                    _options.CertificateBase64.Length);
                throw;
            }
        }

        _logger.LogWarning("EfiBank: certificado não encontrado — Path={Path}, Base64Set={Base64Set}",
            _options.CertificatePath, !string.IsNullOrWhiteSpace(_options.CertificateBase64));

        return handler;
    }

    private HttpClient BuildHttpClient(HttpMessageHandler handler, string? bearerToken)
    {
        var http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (!string.IsNullOrWhiteSpace(bearerToken))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);

        return http;
    }

    /// <summary>
    /// Generates a deterministic 32-char hex txId from the idempotency key.
    /// This ensures the same payout is never sent twice (EfiBank recognizes duplicate idEnvio).
    /// </summary>
    private static string GenerateTxId(string idempotencyKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey));
        return Convert.ToHexString(hash[..16]); // 32 hex chars, within EfiBank's 26-35 char limit
    }
}

// ── Internal DTOs ─────────────────────────────────────────────────────────────

internal sealed record EfiBankPayoutResponse(
    [property: JsonPropertyName("e2eId")] string? E2eId,
    [property: JsonPropertyName("endToEndId")] string? EndToEndIdLegacy)
{
    public string? EndToEndId => E2eId ?? EndToEndIdLegacy;
}
