using Confirmai.Services.Core;
using Confirmai.Services.Payment.Shared;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Confirmai.Data;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Handles AbacatePay webhook POST requests.
/// Security model (two layers):
///   1. Query-string secret  — ?webhookSecret=YOUR_SECRET verified with timing-safe compare
///   2. HMAC-SHA256 signature — X-Webhook-Signature header verified against AbacatePay's public key
/// </summary>
public sealed class AbacatePayWebhookService
{
    private readonly LogService _log;
    private readonly AbacatePayOptions _options;
    private readonly WebhookPaymentMarker _paymentMarker;

    public AbacatePayWebhookService(
        LogService log,
        IOptions<AbacatePayOptions> options,
        WebhookPaymentMarker paymentMarker)
    {
        _log = log;
        _options = options.Value;
        _paymentMarker = paymentMarker;
    }

    public async Task<IResult> HandleAsync(HttpContext context)
    {
        // ── Layer 1: query-string secret ───────────────────────────────────
        if (!WebhookSecretValidator.Validate(context, _options.WebhookSecret))
        {
            await _log.LogAsync(
                $"AbacatePay webhook: webhookSecret inválido. IP={context.Connection.RemoteIpAddress}",
                source: "Webhook", level: "Warning");
            return Results.Unauthorized();
        }

        // ── Read body (size-bounded) ────────────────────────────────────────
        var body = await WebhookBodyReader.ReadBodyAsync(context);
        if (body is null)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        // ── Layer 2: HMAC-SHA256 signature ─────────────────────────────────
        var signature = context.Request.Headers["X-Webhook-Signature"].FirstOrDefault() ?? "";
        if (!string.IsNullOrWhiteSpace(signature))
        {
            var expected = ComputeHmacBase64(body);
            if (!WebhookSecretValidator.FixedTimeCompare(signature, expected))
            {
                await _log.LogAsync(
                    "AbacatePay webhook: assinatura HMAC inválida.",
                    source: "Webhook", level: "Warning");
                return Results.Unauthorized();
            }
        }

        // ── Parse payload ──────────────────────────────────────────────────
        AbacatePayWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AbacatePayWebhookPayload>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            await _log.LogAsync(
                $"AbacatePay webhook: payload inválido. {ex.Message}",
                source: "Webhook", level: "Warning");
            return Results.BadRequest("Payload inválido.");
        }

        await _log.LogAsync(
            $"AbacatePay webhook recebido: event={payload?.Event ?? "null"}",
            source: "Webhook", level: "Info");

        if (payload?.Event == "transparent.completed" &&
            payload.Data?.Id is string chargeId)
        {
            var found = await _paymentMarker.MarkConfirmationPaidAsync(chargeId, "Pix", "AbacatePay");
            if (!found)
            {
                await _paymentMarker.MarkMarketplacePaymentPaidAsync(chargeId, "AbacatePay");
            }
        }

        return Results.Ok();
    }

    /// <summary>HMAC-SHA256 of the raw UTF-8 body, base64-encoded.</summary>
    private static string ComputeHmacBase64(string body)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(AbacatePayOptions.PublicHmacKey);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash      = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToBase64String(hash);
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

internal sealed record AbacatePayWebhookPayload(
    [property: JsonPropertyName("id")]    string? Id,
    [property: JsonPropertyName("event")] string? Event,
    [property: JsonPropertyName("data")]  AbacatePayWebhookData? Data);

internal sealed record AbacatePayWebhookData(
    [property: JsonPropertyName("id")]     string? Id,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("amount")] int     Amount);
