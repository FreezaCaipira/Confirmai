using Confirmai.Configuration;

namespace Confirmai.Services.Notification;

public class WhatsAppNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public WhatsAppNotificationService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<bool> SendNotificationAsync(string phoneNumber, string message)
    {
        var whatsappConfig = _config.GetSection("WhatsApp");
        var apiKey = whatsappConfig["ApiKey"];
        var apiUrl = whatsappConfig["ApiUrl"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiUrl))
        {
            return false;
        }

        try
        {
            var payload = new
            {
                phone = phoneNumber,
                message = message
            };

            var response = await _httpClient.PostAsJsonAsync($"{apiUrl}?key={apiKey}", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendEventReminderAsync(string phoneNumber, string eventName, DateTime eventDate)
    {
        var message = $"🎮 Lembrete: {eventName} acontece em {eventDate:dd/MM/yyyy} às {eventDate:HH:mm}. Não esqueça de confirmar sua presença!";
        return await SendNotificationAsync(phoneNumber, message);
    }

    public async Task<bool> SendPaymentReminderAsync(string phoneNumber, string eventName, decimal amount)
    {
        var message = $"💰 Pagamento pendente: {eventName} - Valor: R$ {amount:F2}. Por favor, realize o pagamento para confirmar sua presença.";
        return await SendNotificationAsync(phoneNumber, message);
    }
}
