using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Confirmai.Services.Interfaces;

namespace Confirmai.Services.Payment;

public class BtcPayServerPaymentService : IBitcoinPaymentService
{
    public string Name => "BTCPayServer";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _storeId;
    private readonly string _btcpayUrl;

    public BtcPayServerPaymentService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = config["BtcPay:ApiKey"] ?? "";
        _storeId = config["BtcPay:StoreId"] ?? "";
        _btcpayUrl = config["BtcPay:Url"] ?? "";
    }

    public async Task<(string Address, string PaymentId)> GenerateAddressAsync(decimal amount, string? orderId = null)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_btcpayUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", _apiKey);

        var body = new
        {
            amount = amount.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
            currency = "BTC",
            metadata = new { orderId }
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/api/v1/stores/{_storeId}/invoices", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var invoiceId = doc.RootElement.GetProperty("id").GetString() ?? "";
        var paymentAddress = doc.RootElement.GetProperty("checkoutLink").GetString() ?? "";

        return (paymentAddress, invoiceId);
    }

    public async Task<decimal> GetReceivedAmountAsync(string invoiceId)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_btcpayUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", _apiKey);

        var response = await client.GetAsync($"/api/v1/stores/{_storeId}/invoices/{invoiceId}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("amountPaid", out var amountPaidProp))
        {
            // Pode ser decimal, int, ou string
            decimal amountPaid = 0;
            if (amountPaidProp.ValueKind == JsonValueKind.Number)
                amountPaid = amountPaidProp.GetDecimal();
            else if (amountPaidProp.ValueKind == JsonValueKind.String && decimal.TryParse(amountPaidProp.GetString(), out var parsed))
                amountPaid = parsed;

            return amountPaid;
        }

        return 0m;
    }

    public Task<(string Address, string PaymentId, string PrivateKey)> GenerateAddressWithKeyAsync(decimal amount, string? orderId = null)
    {
        // Não suportado para BTCPayServer, lance exceção ou implemente se necessário
        throw new NotImplementedException("GenerateAddressWithKeyAsync não implementado para BtcPayServerPaymentService.");
    }
}
