using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Confirmai.Configuration;
using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Creates and checks Appmax Pix charges for event confirmations.
/// Flow: get bearer token -> create customer -> create order -> create Pix payment.
/// </summary>
public sealed class AppmaxPixService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly AppmaxOptions _options;
    private readonly ILogger<AppmaxPixService> _logger;

    public AppmaxPixService(
        IHttpClientFactory httpClientFactory,
        IDbContextFactory<AppDbContext> dbFactory,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IOptions<AppmaxOptions> options,
        ILogger<AppmaxPixService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.IsEnabled;

    public async Task<(string ChargeId, string BrCode)> CreateChargeAsync(decimal amount, int confirmationId)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException(
                "Appmax não está configurado. Defina Appmax:EnableEventCheckout e credenciais de autenticação.");
        }

        var checkout = await BuildCheckoutContextAsync(confirmationId);
        var bearer = await GetBearerTokenAsync();

        var customerId = await CreateCustomerAsync(checkout, bearer);
        var orderId = await CreateOrderAsync(checkout, customerId, amount, bearer);
        var brCode = await CreatePixPaymentAsync(orderId, bearer);

        _logger.LogInformation(
            "Appmax Pix criado: orderId={OrderId}, confirmationId={ConfirmationId}, amount={Amount}",
            orderId, confirmationId, amount);

        return (orderId.ToString(CultureInfo.InvariantCulture), brCode);
    }

    public async Task<bool> IsChargePaidAsync(string chargeId)
    {
        if (!IsEnabled)
            return false;

        if (!long.TryParse(chargeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var orderId))
            return false;

        try
        {
            var bearer = await GetBearerTokenAsync();
            var status = await GetOrderStatusAsync(orderId, bearer);
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return IsPaidOrderStatus(status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appmax: erro ao verificar status do pedido {OrderId}", orderId);
            return false;
        }
    }

    private async Task<string> GetBearerTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            return _options.ApiKey.Trim();

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Appmax requer ApiKey ou ClientId/ClientSecret para autenticação.");
        }

        var cacheKey = $"Appmax_AccessToken_{_options.ClientId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
            return cachedToken;

        var authClient = _httpClientFactory.CreateClient("AppmaxAuth");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/oauth2/token")
        {
            Content = form
        };

        using var response = await authClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha ao autenticar na Appmax ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var token = root.TryGetProperty("access_token", out var tokenEl)
            ? tokenEl.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Appmax não retornou access_token na autenticação.");

        var expiresIn = root.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var exp)
            ? exp
            : 3600;

        _cache.Set(cacheKey, token, TimeSpan.FromSeconds(Math.Max(60, expiresIn - 60)));
        return token;
    }

    private async Task<long> CreateCustomerAsync(AppmaxCheckoutContext checkout, string bearer)
    {
        var apiClient = CreateApiClient(bearer);

        var payload = new
        {
            first_name = checkout.FirstName,
            last_name = checkout.LastName,
            email = checkout.Email,
            phone = checkout.Phone,
            ip = checkout.Ip,
            products = new[]
            {
                new
                {
                    sku = checkout.ProductSku,
                    name = checkout.ProductName,
                    quantity = 1,
                    unit_value = checkout.AmountCents,
                    type = "digital"
                }
            }
        };

        using var response = await apiClient.PostAsJsonAsync("/v1/customers", payload);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Appmax falhou ao criar cliente ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var customerId = TryGetInt64Path(json.RootElement,
            "data.customer.id",
            "data.id",
            "customer_id",
            "id");

        if (customerId is null)
            throw new InvalidOperationException("Appmax não retornou customer id ao criar cliente.");

        return customerId.Value;
    }

    private async Task<long> CreateOrderAsync(AppmaxCheckoutContext checkout, long customerId, decimal amount, string bearer)
    {
        var apiClient = CreateApiClient(bearer);

        var amountCents = ToCents(amount);
        var payload = new
        {
            customer_id = customerId,
            products = new[]
            {
                new
                {
                    sku = checkout.ProductSku,
                    name = checkout.ProductName,
                    quantity = 1,
                    unit_value = amountCents,
                    type = "digital"
                }
            }
        };

        using var response = await apiClient.PostAsJsonAsync("/v1/orders", payload);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Appmax falhou ao criar pedido ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var orderId = TryGetInt64Path(json.RootElement,
            "data.order.id",
            "data.id",
            "order_id",
            "id");

        if (orderId is null)
            throw new InvalidOperationException("Appmax não retornou order id ao criar pedido.");

        return orderId.Value;
    }

    private async Task<string> CreatePixPaymentAsync(long orderId, string bearer)
    {
        var apiClient = CreateApiClient(bearer);

        var payload = new
        {
            order_id = orderId
        };

        using var response = await apiClient.PostAsJsonAsync("/v1/payments/pix", payload);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Appmax falhou ao criar pagamento Pix ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var brCode = TryGetStringPath(json.RootElement,
            "data.payment.pix.qr_code",
            "data.payment.pix.copy_paste",
            "data.payment.pix.emv",
            "data.pix.qr_code",
            "data.pix.copy_paste",
            "data.pix.emv",
            "pix.qr_code",
            "pix.copy_paste",
            "pix.emv");

        if (string.IsNullOrWhiteSpace(brCode))
            throw new InvalidOperationException("Appmax não retornou o código Pix (copia e cola/EMV).");

        return brCode;
    }

    private async Task<string?> GetOrderStatusAsync(long orderId, string bearer)
    {
        var apiClient = CreateApiClient(bearer);

        using var response = await apiClient.GetAsync($"/v1/orders/{orderId}");
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        return TryGetStringPath(json.RootElement,
            "data.order.status",
            "order.status",
            "status");
    }

    private async Task<AppmaxCheckoutContext> BuildCheckoutContextAsync(int confirmationId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var confirmation = await db.EventConfirmations
            .Include(c => c.User)
            .Include(c => c.Event)
                .ThenInclude(e => e.Group)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (confirmation is null)
            throw new InvalidOperationException("Confirmação não encontrada para criar pagamento Appmax.");

        var fullName = !string.IsNullOrWhiteSpace(confirmation.User.FullName)
            ? confirmation.User.FullName!.Trim()
            : (confirmation.User.UserName?.Trim() ?? string.Empty);

        if (string.IsNullOrWhiteSpace(fullName))
            fullName = "Cliente Confirmai";

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = parts.Length > 0 ? parts[0] : "Cliente";
        var lastName = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : "Confirmai";

        var email = confirmation.User.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "Seu usuário não possui e-mail válido para gerar cobrança via Appmax.");
        }

        var phone = NormalizePhone(confirmation.User.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException(
                "Seu usuário não possui telefone válido (DDD + número) para cobrança via Appmax.");
        }

        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(ip))
            ip = "127.0.0.1";

        var amountCents = ToCents(confirmation.Event.Price ?? 0m);
        var productSku = $"event-{confirmation.EventId}";
        var productName = string.IsNullOrWhiteSpace(confirmation.Event.Group?.Name)
            ? $"Evento #{confirmation.EventId}"
            : confirmation.Event.Group.Name;

        return new AppmaxCheckoutContext(
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Phone: phone,
            Ip: ip,
            ProductSku: productSku,
            ProductName: productName,
            AmountCents: amountCents);
    }

    private HttpClient CreateApiClient(string bearer)
    {
        var client = _httpClientFactory.CreateClient("Appmax");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return client;
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length > 11)
            digits = digits[2..];

        if (digits.Length > 11)
            digits = digits[^11..];

        return digits.Length is >= 10 and <= 11 ? digits : null;
    }

    private static int ToCents(decimal amount)
    {
        var normalized = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        return (int)(normalized * 100m);
    }

    private static bool IsPaidOrderStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized is "aprovado" or "approved" or "pago" or "paid" or "integrado";
    }

    private static long? TryGetInt64Path(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryResolvePath(root, path, out var element))
                continue;

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
                return number;

            if (element.ValueKind == JsonValueKind.String &&
                long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? TryGetStringPath(JsonElement root, params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!TryResolvePath(root, path, out var element))
                continue;

            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private static bool TryResolvePath(JsonElement root, string path, out JsonElement element)
    {
        element = root;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(segment, out element))
                return false;
        }

        return true;
    }

    private sealed record AppmaxCheckoutContext(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string Ip,
        string ProductSku,
        string ProductName,
        int AmountCents);
}
