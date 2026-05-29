using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Microsoft.Extensions.Options;

namespace Confirmai.Services;

/// <summary>
/// Pix payment service backed by AbacatePay Transparent Checkout API.
/// Implements <see cref="IBitcoinPaymentService"/> so it plugs into the
/// existing <see cref="BitcoinPaymentFactory"/> plugin pattern.
/// </summary>
public sealed class AbacatePayPixService : IBitcoinPaymentService
{
    private readonly HttpClient _http;
    private readonly AbacatePayOptions _options;
    private readonly ILogger<AbacatePayPixService> _logger;

    public string Name => "Pix";
    public bool IsEnabled => _options.IsEnabled;

    public AbacatePayPixService(
        IHttpClientFactory httpClientFactory,
        IOptions<AbacatePayOptions> options,
        ILogger<AbacatePayPixService> logger)
    {
        _options = options.Value;
        _http = httpClientFactory.CreateClient("AbacatePay");
        _logger = logger;
    }

    /// <summary>
    /// Creates a Pix charge via AbacatePay and returns
    /// (brCode = copia-e-cola, chargeId = AbacatePay charge ID).
    /// </summary>
    public async Task<(string Address, string PaymentId)> GenerateAddressAsync(
        decimal amount, string? orderId = null)
    {
        if (!_options.IsEnabled)
            throw new InvalidOperationException(
                "AbacatePay não está configurado. Defina AbacatePay:ApiKey nas configurações.");

        // AbacatePay expects centavos (integer)
        var amountCentavos = (int)Math.Round(amount * 100);

        var request = new
        {
            method = "PIX",
            data = new
            {
                amount = amountCentavos,
                description = orderId is not null
                    ? $"Pagamento {orderId}"
                    : "Pagamento via Confirmai",
                expiresIn = _options.PixExpiresInSeconds,
                metadata = orderId is not null
                    ? (object)new { orderId }
                    : new { }
            }
        };

        var response = await _http.PostAsJsonAsync("/transparents/create", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content
            .ReadFromJsonAsync<AbacatePayEnvelope<AbacatePayTransparent>>();

        if (envelope?.Success is not true || envelope.Data is null)
            throw new InvalidOperationException(
                $"AbacatePay retornou erro: {envelope?.Error ?? "resposta inválida"}");

        _logger.LogInformation(
            "AbacatePay Pix gerado: id={ChargeId}, amount={Amount}",
            envelope.Data.Id, amount);

        return (envelope.Data.BrCode, envelope.Data.Id);
    }

    /// <summary>
    /// Polls the charge status. Returns the full payment amount when PAID,
    /// or <c>0</c> when pending/expired/unknown.
    /// </summary>
    public async Task<decimal> GetReceivedAmountAsync(string chargeId)
    {
        if (!_options.IsEnabled)
            return 0m;

        var response = await _http.GetAsync(
            $"/transparents/check?id={Uri.EscapeDataString(chargeId)}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "AbacatePay check retornou {Status} para chargeId={ChargeId}",
                response.StatusCode, chargeId);
            return 0m;
        }

        var envelope = await response.Content
            .ReadFromJsonAsync<AbacatePayEnvelope<AbacatePayCheckResult>>();

        if (envelope?.Data is null)
            return 0m;

        // Return MaxValue so PaymentConfirmationService always treats PAID as confirmed
        return string.Equals(envelope.Data.Status, "PAID", StringComparison.OrdinalIgnoreCase)
            ? decimal.MaxValue
            : 0m;
    }

    /// <summary>Not applicable to Pix — Pix has no private key concept.</summary>
    public Task<(string Address, string PaymentId, string PrivateKey)> GenerateAddressWithKeyAsync(
        decimal amount, string? orderId = null)
        => throw new NotSupportedException("Pix não suporta geração de endereço com chave privada.");
}

// ── Internal DTOs ────────────────────────────────────────────────────────────

internal sealed record AbacatePayEnvelope<T>(
    [property: JsonPropertyName("data")]    T?     Data,
    [property: JsonPropertyName("success")] bool   Success,
    [property: JsonPropertyName("error")]   string? Error);

internal sealed record AbacatePayTransparent(
    [property: JsonPropertyName("id")]          string   Id,
    [property: JsonPropertyName("brCode")]      string   BrCode,
    [property: JsonPropertyName("brCodeBase64")] string? BrCodeBase64,
    [property: JsonPropertyName("status")]      string   Status,
    [property: JsonPropertyName("amount")]      int      Amount,
    [property: JsonPropertyName("expiresAt")]   DateTime? ExpiresAt);

internal sealed record AbacatePayCheckResult(
    [property: JsonPropertyName("id")]        string   Id,
    [property: JsonPropertyName("status")]    string   Status,
    [property: JsonPropertyName("expiresAt")] DateTime? ExpiresAt);
