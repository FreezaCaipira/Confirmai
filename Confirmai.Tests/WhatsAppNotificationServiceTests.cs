using System.Net;
using System.Text.Json;
using Confirmai.Services.Notification;
using Microsoft.Extensions.Configuration;

namespace Confirmai.Tests;

public class WhatsAppNotificationServiceTests
{
    private static IConfiguration CreateConfig(string? apiKey = null, string? apiUrl = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["WhatsApp:ApiKey"] = apiKey,
            ["WhatsApp:ApiUrl"] = apiUrl
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static HttpClient CreateMockClient(HttpStatusCode statusCode, out List<string> requests)
    {
        requests = new List<string>();
        var handler = new MockHttpMessageHandler(statusCode, requests);
        return new HttpClient(handler);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly List<string> _requests;

        public MockHttpMessageHandler(HttpStatusCode statusCode, List<string> requests)
        {
            _statusCode = statusCode;
            _requests = requests;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request.RequestUri?.ToString() ?? "");
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }

    [Fact]
    public async Task SendNotification_NoConfig_ReturnsFalse()
    {
        var config = CreateConfig(apiKey: null, apiUrl: null);
        var client = CreateMockClient(HttpStatusCode.OK, out _);
        var service = new WhatsAppNotificationService(client, config);

        var result = await service.SendNotificationAsync("5511999999999", "test");

        Assert.False(result);
    }

    [Fact]
    public async Task SendNotification_WithConfig_CallsApi()
    {
        var config = CreateConfig(apiKey: "test-key", apiUrl: "https://api.example.com/send");
        var client = CreateMockClient(HttpStatusCode.OK, out var requests);
        var service = new WhatsAppNotificationService(client, config);

        var result = await service.SendNotificationAsync("5511999999999", "test message");

        Assert.True(result);
        Assert.Single(requests);
        Assert.Contains("test-key", requests[0]);
    }

    [Fact]
    public async Task SendNotification_ApiError_ReturnsFalse()
    {
        var config = CreateConfig(apiKey: "test-key", apiUrl: "https://api.example.com/send");
        var client = CreateMockClient(HttpStatusCode.InternalServerError, out _);
        var service = new WhatsAppNotificationService(client, config);

        var result = await service.SendNotificationAsync("5511999999999", "test");

        Assert.False(result);
    }

    [Fact]
    public async Task SendEventReminder_FormatsMessageCorrectly()
    {
        var config = CreateConfig(apiKey: "test-key", apiUrl: "https://api.example.com/send");
        var client = CreateMockClient(HttpStatusCode.OK, out var requests);
        var service = new WhatsAppNotificationService(client, config);

        var result = await service.SendEventReminderAsync("5511999999999", "Racha Quintas", new DateTime(2026, 7, 10, 20, 0, 0));

        Assert.True(result);
        Assert.Single(requests);
    }

    [Fact]
    public async Task SendPaymentReminder_FormatsAmountCorrectly()
    {
        var config = CreateConfig(apiKey: "test-key", apiUrl: "https://api.example.com/send");
        var client = CreateMockClient(HttpStatusCode.OK, out var requests);
        var service = new WhatsAppNotificationService(client, config);

        var result = await service.SendPaymentReminderAsync("5511999999999", "Racha Quintas", 25.00m);

        Assert.True(result);
        Assert.Single(requests);
    }
}
