using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services;

/// <summary>
/// Handles EfiBank Pix webhook POST requests.
/// EfiBank sends a JSON payload with a "pix" array each time a Pix is received.
/// This service finds the matching EventConfirmation via PixTxId and marks it as paid.
/// </summary>
public sealed class EfiBankWebhookService
{
    private const int MaxBodyBytes = 64 * 1024;

    private readonly AppDbContext _db;
    private readonly LogService _log;
    private readonly EfiBankOptions _options;
    private readonly PaymentEventBus _eventBus;

    public EfiBankWebhookService(
        AppDbContext db,
        LogService log,
        IOptions<EfiBankOptions> options,
        PaymentEventBus eventBus)
    {
        _db = db;
        _log = log;
        _options = options.Value;
        _eventBus = eventBus;
    }

    public async Task<IResult> HandleAsync(HttpContext context)
    {
        // ── mTLS client-certificate validation (optional) ───────────────────
        // When EfiBank:WebhookClientCertSubject is configured, verify that the certificate
        // EfiBank presents during the TLS handshake has the expected subject.
        // Requires Kestrel (or nginx via X-SSL-Client-Cert header) to pass the client cert.
        if (!string.IsNullOrWhiteSpace(_options.WebhookClientCertSubject))
        {
            var clientCert = context.Connection.ClientCertificate
                ?? TryGetCertFromHeader(context);

            if (clientCert is null ||
                !clientCert.Subject.Contains(
                    _options.WebhookClientCertSubject, StringComparison.OrdinalIgnoreCase))
            {
                await _log.LogAsync(
                    $"EfiBank webhook: certificado cliente inválido ou ausente. Subject={clientCert?.Subject ?? "(none)"}. IP={context.Connection.RemoteIpAddress}",
                    source: "Webhook", level: "Warning");
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        // ── Optional query-string secret check ─────────────────────────────
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
                    $"EfiBank webhook: webhookSecret inválido. IP={context.Connection.RemoteIpAddress}",
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

        // ── Parse payload ───────────────────────────────────────────────────
        EfiBankWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EfiBankWebhookPayload>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            await _log.LogAsync(
                $"EfiBank webhook: payload inválido. {ex.Message}",
                source: "Webhook", level: "Warning");
            return Results.BadRequest("Payload inválido.");
        }

        var pixCount = payload?.Pix?.Count ?? 0;
        await _log.LogAsync(
            $"EfiBank webhook recebido: {pixCount} pix(es).",
            source: "Webhook", level: "Info");

        if (payload?.Pix is { Count: > 0 })
        {
            foreach (var pix in payload.Pix)
            {
                if (!string.IsNullOrWhiteSpace(pix.TxId))
                    await MarkPaymentPaidAsync(pix.TxId);
            }
        }

        return Results.Ok();
    }

    private async Task MarkPaymentPaidAsync(string txId)
    {
        var conf = await _db.EventConfirmations
            .FirstOrDefaultAsync(c => c.PixTxId == txId);

        if (conf is null)
        {
            await _log.LogAsync(
                $"EfiBank webhook: EventConfirmation não encontrado para txId={txId}.",
                source: "Webhook", level: "Warning");
            return;
        }

        if (conf.PaymentStatus == EventConfirmationPaymentStatus.Paid)
            return; // idempotent

        if (conf.PaymentStatus == EventConfirmationPaymentStatus.Refunded)
            return; // do not resurrect refunded confirmations

        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        if (string.IsNullOrWhiteSpace(conf.PaymentGatewayName))
            conf.PaymentGatewayName = "EfiBank";

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return; // another process beat us to it
        }

        _eventBus.NotifyPaymentConfirmed(conf.UserId, txId);

        await _log.LogAsync(
            $"EfiBank: pagamento txId={txId} confirmado via webhook. ConfirmationId={conf.Id}",
            source: "Webhook", level: "Info");
    }

    /// <summary>
    /// Reads the client certificate from the <c>X-SSL-Client-Cert</c> header injected by nginx
    /// when Kestrel is behind a reverse proxy with <c>ssl_client_certificate</c> configured.
    /// </summary>
    private static System.Security.Cryptography.X509Certificates.X509Certificate2? TryGetCertFromHeader(
        HttpContext context)
    {
        var headerValue = context.Request.Headers["X-SSL-Client-Cert"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue)) return null;
        try
        {
            var pem = Uri.UnescapeDataString(headerValue);
            return System.Security.Cryptography.X509Certificates.X509CertificateLoader
                .LoadCertificate(System.Text.Encoding.ASCII.GetBytes(pem));
        }
        catch
        {
            return null;
        }
    }
}

// ── Internal DTOs ─────────────────────────────────────────────────────────────

internal sealed record EfiBankWebhookPayload(
    [property: JsonPropertyName("pix")] List<EfiBankPixWebhookEntry>? Pix);

internal sealed record EfiBankPixWebhookEntry(
    [property: JsonPropertyName("txid")]    string? TxId,
    [property: JsonPropertyName("valor")]   string? Valor,
    [property: JsonPropertyName("horario")] string? Horario);
