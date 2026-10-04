using System.Net.Http.Json;
using Confirmai.Configuration;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Notification;

/// <summary>
/// C31 — Evolution API sender. Contract: POST {BaseUrl}/message/sendText/{Instance},
/// header "apikey", body { number = group JID, text }.
/// </summary>
public sealed class EvolutionWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<EvolutionWhatsAppSender> _logger;

    public EvolutionWhatsAppSender(
        HttpClient httpClient,
        IOptions<WhatsAppOptions> options,
        ILogger<EvolutionWhatsAppSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WhatsAppSendResult> SendGroupTextAsync(string groupJid, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Instance) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return WhatsAppSendResult.Failed("missing_config");
        }

        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/message/sendText/{_options.Instance}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new { number = groupJid, text })
            };
            request.Headers.TryAddWithoutValidation("apikey", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return WhatsAppSendResult.Failed($"http_{(int)response.StatusCode}");
            }

            return WhatsAppSendResult.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Evolution: falha ao enviar texto para grupo {GroupJid}", groupJid);
            return WhatsAppSendResult.Failed("exception");
        }
    }
}
