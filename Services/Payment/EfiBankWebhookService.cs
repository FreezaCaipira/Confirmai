using Confirmai.Services.Core;
using Confirmai.Services.Payment.Shared;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confirmai.Configuration;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Handles EfiBank Pix webhook POST requests.
/// EfiBank sends a JSON payload with a "pix" array each time a Pix is received.
/// This service finds the matching EventConfirmation via PixTxId and marks it as paid.
/// </summary>
public sealed class EfiBankWebhookService
{
    private readonly LogService _log;
    private readonly EfiBankOptions _options;
    private readonly WebhookPaymentMarker _paymentMarker;

    public EfiBankWebhookService(
        LogService log,
        IOptions<EfiBankOptions> options,
        WebhookPaymentMarker paymentMarker)
    {
        _log = log;
        _options = options.Value;
        _paymentMarker = paymentMarker;
    }

    public async Task<IResult> HandleAsync(HttpContext context)
    {
        // ── mTLS client-certificate validation (optional) ───────────────────
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
        if (!WebhookSecretValidator.Validate(context, _options.WebhookSecret))
        {
            await _log.LogAsync(
                $"EfiBank webhook: webhookSecret inválido. IP={context.Connection.RemoteIpAddress}",
                source: "Webhook", level: "Warning");
            return Results.Unauthorized();
        }

        // ── Read body (size-bounded) ────────────────────────────────────────
        var body = await WebhookBodyReader.ReadBodyAsync(context);
        if (body is null)
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
                    await _paymentMarker.MarkConfirmationPaidAsync(pix.TxId, "EfiBank", "EfiBank");
            }
        }

        return Results.Ok();
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
