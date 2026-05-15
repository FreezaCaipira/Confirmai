using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services;

/// <summary>
/// Handles AbacatePay webhook POST requests.
/// Security model (two layers):
///   1. Query-string secret  — ?webhookSecret=YOUR_SECRET verified with timing-safe compare
///   2. HMAC-SHA256 signature — X-Webhook-Signature header verified against AbacatePay's public key
/// </summary>
public sealed class AbacatePayWebhookService
{
    private const int MaxBodyBytes = 64 * 1024;

    private readonly AppDbContext _db;
    private readonly LogService _log;
    private readonly AbacatePayOptions _options;
    private readonly PaymentEventBus _eventBus;

    public AbacatePayWebhookService(
        AppDbContext db,
        LogService log,
        IOptions<AbacatePayOptions> options,
        PaymentEventBus eventBus)
    {
        _db = db;
        _log = log;
        _options = options.Value;
        _eventBus = eventBus;
    }

    public async Task<IResult> HandleAsync(HttpContext context)
    {
        // ── Layer 1: query-string secret ───────────────────────────────────
        if (!string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            var provided = context.Request.Query["webhookSecret"].ToString() ?? "";
            var expected = _options.WebhookSecret;

            var providedBytes = Encoding.UTF8.GetBytes(provided.PadRight(expected.Length));
            var expectedBytes = Encoding.UTF8.GetBytes(expected);

            if (provided.Length != expected.Length ||
                !CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
            {
                await _log.LogAsync(
                    $"AbacatePay webhook: webhookSecret inválido. IP={context.Connection.RemoteIpAddress}",
                    source: "Webhook", level: "Warning");
                return Results.Unauthorized();
            }
        }

        // ── Read body (size-bounded) ────────────────────────────────────────
        if (context.Request.ContentLength > MaxBodyBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        if (Encoding.UTF8.GetByteCount(body) > MaxBodyBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        // ── Layer 2: HMAC-SHA256 signature ─────────────────────────────────
        var signature = context.Request.Headers["X-Webhook-Signature"].FirstOrDefault() ?? "";
        if (!string.IsNullOrWhiteSpace(signature))
        {
            var expected = ComputeHmacBase64(body);

            var sigBytes = Encoding.UTF8.GetBytes(signature.PadRight(expected.Length));
            var expBytes = Encoding.UTF8.GetBytes(expected);

            if (signature.Length != expected.Length ||
                !CryptographicOperations.FixedTimeEquals(sigBytes, expBytes))
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
            await MarkPaymentPaidAsync(chargeId);
        }

        return Results.Ok();
    }

    private async Task MarkPaymentPaidAsync(string chargeId)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == chargeId);

        if (payment is null)
        {
            await _log.LogAsync(
                $"AbacatePay webhook: pagamento não encontrado para chargeId={chargeId}.",
                source: "Webhook", level: "Warning");
            return;
        }

        if (payment.IsPaid)
            return; // idempotent

        payment.IsPaid = true;
        payment.PaidAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another process already updated; treat as success
            return;
        }

        _eventBus.NotifyPaymentConfirmed(payment.UserId ?? "", chargeId);

        await _log.LogAsync(
            $"AbacatePay: pagamento chargeId={chargeId} confirmado via webhook.",
            source: "Webhook", level: "Info");
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
