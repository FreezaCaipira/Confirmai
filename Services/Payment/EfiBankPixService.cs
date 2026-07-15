using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Confirmai.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

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
    // Overrides the HttpMessageHandler factory — used only in tests.
    private readonly Func<HttpMessageHandler>? _handlerFactory;

    public EfiBankPixService(
        IHttpClientFactory httpClientFactory,
        IOptions<EfiBankOptions> options,
        IMemoryCache cache,
        ILogger<EfiBankPixService> logger)
        : this(httpClientFactory, options, cache, logger, handlerFactory: null) { }

    /// <summary>Test-only constructor that injects a custom <see cref="HttpMessageHandler"/> factory.</summary>
    internal EfiBankPixService(
        IHttpClientFactory httpClientFactory,
        IOptions<EfiBankOptions> options,
        IMemoryCache cache,
        ILogger<EfiBankPixService> logger,
        Func<HttpMessageHandler>? handlerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _handlerFactory = handlerFactory;
    }

    /// <summary>Returns 1.00 in sandbox (EfiBank auto-confirms only when amount ≤ R$ 10).</summary>
    internal static decimal GetEffectiveAmount(decimal requested, bool sandbox) =>
        sandbox ? 1.00m : requested;

    public bool IsEnabled => _options.IsEnabled;

    /// <summary>
    /// Creates a Pix payment split configuration for automatic payout to organizer.
    /// NOTE: Split Pix requires EfiBank accounts for all participants. 
    /// This method is currently disabled as most organizers don't have EfiBank accounts.
    /// Manual payout via PayoutService is used instead.
    /// </summary>
    public async Task<string> CreateSplitAsync(GroupPayoutAccount payoutAccount, decimal serviceFeePercentage)
    {
        // Split Pix requires EfiBank accounts - not available for most organizers
        // Using manual payout via PayoutService instead
        throw new NotImplementedException("Split Pix requires EfiBank accounts. Use PayoutService for manual payout.");
    }

    /// <summary>
    /// Links a Pix charge to a payment split configuration.
    /// NOTE: Split Pix requires EfiBank accounts for all participants.
    /// This method is currently disabled as most organizers don't have EfiBank accounts.
    /// Manual payout via PayoutService is used instead.
    /// </summary>
    public async Task LinkChargeToSplitAsync(string txId, string splitId)
    {
        // Split Pix requires EfiBank accounts - not available for most organizers
        // Using manual payout via PayoutService instead
        throw new NotImplementedException("Split Pix requires EfiBank accounts. Use PayoutService for manual payout.");
    }

    /// <summary>
    /// Creates a Pix charge (Cob) and returns (txId, pixCopiaECola).
    /// txId is stored in EventConfirmation.PixTxId so the webhook can resolve it.
    /// NOTE: Split Pix is disabled as it requires EfiBank accounts for all participants.
    /// Manual payout via PayoutService is used instead.
    /// </summary>
    public async Task<(string TxId, string BrCode)> CreateChargeAsync(decimal amount, int confirmationId, GroupPayoutAccount? payoutAccount = null, decimal serviceFeePercentage = 0)
    {
        if (!_options.IsEnabled)
            throw new InvalidOperationException(
                "EfiBank não está configurado. Defina EfiBank:ClientId, ClientSecret, CertificatePath e PixKey.");

        var txId  = GenerateTxId();
        var token = await GetAccessTokenAsync();

        using var http = CreateHttpClient(token);

        // Em sandbox, cobrar sempre R$ 1,00 para acionar a confirmação automática do EfiBank
        // (cobranças > R$ 10,00 ficam ATIVAS para sempre em homologação).
        amount = GetEffectiveAmount(amount, _options.Sandbox);

        var amountStr = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        
        var body = new
        {
            calendario        = new { expiracao = _options.PixExpiresInSeconds },
            valor             = new { original  = amountStr },
            chave             = _options.PixKey,
            solicitacaoPagador = $"Confirmai #{confirmationId}"
        };

        var response = await http.PutAsJsonAsync($"/v2/cob/{txId}", body);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("EfiBank: falha ao criar cobrança. Status={Status}, Body={Body}",
                response.StatusCode, errorBody);
        }

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

    /// <summary>
    /// Registers (or updates) the webhook URL with EfiBank for the configured Pix key.
    /// EfiBank API: PUT /v2/webhook/{chave}  → 204 No Content on success.
    /// Should be called once at application startup when <see cref="EfiBankOptions.WebhookUrl"/> is set.
    /// </summary>
    public async Task RegisterWebhookAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogDebug("EfiBank: registro de webhook ignorado — serviço desabilitado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogDebug("EfiBank: EfiBank:WebhookUrl não configurado — registro ignorado.");
            return;
        }

        try
        {
            var token = await GetAccessTokenAsync();
            using var http = CreateHttpClient(token);

            var encodedKey = Uri.EscapeDataString(_options.PixKey!);
            var body = new { webhookUrl = _options.WebhookUrl };

            var response = await http.PutAsJsonAsync($"/v2/webhook/{encodedKey}", body, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "EfiBank: webhook registrado com sucesso. URL={WebhookUrl}, PixKey={PixKey}",
                    _options.WebhookUrl, _options.PixKey);
            }
            else
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "EfiBank: falha ao registrar webhook. Status={Status}, Body={Body}",
                    response.StatusCode, detail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EfiBank: erro ao registrar webhook.");
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

        _logger.LogInformation("EfiBank: tentando obter token OAuth. URL={Url}, ClientId={ClientId}",
            _options.BaseUrl + "/oauth/token", _options.ClientId);

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
            
            _logger.LogInformation("EfiBank: resposta OAuth. Status={Status}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("EfiBank: falha OAuth. Status={Status}, Body={Body}", 
                    response.StatusCode, errorBody);
            }

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

    /// <summary>Creates an HttpClient with the mTLS certificate loaded.</summary>
    private HttpClient CreateHttpClient(string? bearerToken)
    {
        var handler = _handlerFactory?.Invoke() ?? CreateProductionHandler();
        return BuildHttpClient(handler, bearerToken);
    }

    private HttpMessageHandler CreateProductionHandler()
    {
        var handler = new HttpClientHandler();

        if (!string.IsNullOrWhiteSpace(_options.CertificatePath) &&
            File.Exists(_options.CertificatePath))
        {
            // UserKeySet + PersistKeySet + Exportable: persists the private key in the current
            // user's key store so Windows SChannel can access it during mTLS handshake.
            // MachineKeySet would require admin rights and fails with SEC_E_UNKNOWN_CREDENTIALS.
            var password = _options.CertificatePassword ?? string.Empty;
            var cert = X509CertificateLoader.LoadPkcs12FromFile(
                _options.CertificatePath,
                password,
                X509KeyStorageFlags.UserKeySet  |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);

            handler.ClientCertificates.Add(cert);
            _logger.LogInformation("EfiBank: certificado carregado — Subject={Subject}, HasPrivateKey={HasKey}, NotBefore={NotBefore}, NotAfter={NotAfter}",
                cert.Subject, cert.HasPrivateKey, cert.NotBefore, cert.NotAfter);
        }
        else
        {
            _logger.LogWarning("EfiBank: certificado não encontrado em '{Path}'", _options.CertificatePath);
        }

        return handler;
    }

    private HttpClient BuildHttpClient(HttpMessageHandler handler, string? bearerToken)
    {
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

internal sealed record EfiBankSplitResponse(
    [property: JsonPropertyName("id")] string Id);
