using System.Net.Http.Headers;
using System.Net.Http.Json;
using Confirmai.Configuration;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Notification;

/// <summary>
/// C41 — whatsapp-bridge sender. Contract: POST {BaseUrl}/send/group-text,
/// header "Authorization: Bearer {ApiKey}", body { jid, text }.
/// The bridge (whatsapp-bridge/ in this repo, whatsmeow under the hood) owns
/// the WhatsApp session — no instance name needed here.
/// </summary>
public sealed class BridgeWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<BridgeWhatsAppSender> _logger;

    public BridgeWhatsAppSender(
        HttpClient httpClient,
        IOptions<WhatsAppOptions> options,
        ILogger<BridgeWhatsAppSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WhatsAppSendResult> SendGroupTextAsync(string groupJid, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return WhatsAppSendResult.Failed("missing_config");
        }

        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/send/group-text";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new { jid = groupJid, text })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return WhatsAppSendResult.Failed($"http_{(int)response.StatusCode}");
            }

            return WhatsAppSendResult.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp bridge: falha ao enviar texto para grupo {GroupJid}", groupJid);
            return WhatsAppSendResult.Failed("exception");
        }
    }
}
