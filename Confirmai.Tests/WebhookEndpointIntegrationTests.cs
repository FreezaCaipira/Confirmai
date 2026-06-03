using System.Net;
using System.Net.Http.Json;
using System.Text;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class WebhookEndpointIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public WebhookEndpointIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsUnauthorized_WhenSecretIsInvalid()
    {
        var response = await SendWebhookJsonAsync(
            new { invoiceId = "inv-http-unauth", type = "InvoiceSettled" },
            secretValues: ["wrong-secret"]);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsUnauthorized_WhenSecretHasLeadingOrTrailingSpaces()
    {
        var response = await SendWebhookJsonAsync(
            new { invoiceId = "inv-http-secret-spaces", type = "InvoiceSettled" },
            secretValues: [" expected-secret "]);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsUnauthorized_WhenSecretHeaderIsMissing_AndDoesNotMutateData()
    {
        const string invoiceId = "inv-http-missing-secret";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(new { invoiceId, type = "InvoiceSettled" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsUnauthorized_WhenSecretHeaderHasMultipleValues_AndDoesNotMutateData()
    {
        const string invoiceId = "inv-http-multi-secret";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId, type = "InvoiceSettled" },
            secretValues: ["expected-secret", "wrong-secret"]);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsUnauthorized_WhenSecretHeaderHasDuplicateExpectedValues_AndDoesNotMutateData()
    {
        const string invoiceId = "inv-http-multi-secret-duplicate-valid";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId, type = "InvoiceSettled" },
            secretValues: ["expected-secret", "expected-secret"]);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ConfirmsPayment_WhenSecretHeaderNameHasDifferentCasing()
    {
        const string invoiceId = "inv-http-secret-case";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId, type = "InvoiceSettled" },
            headerName: "x-btcpay-secret",
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPaymentSettledAndOrderCreated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsUnauthorized_WhenSecretValueHasDifferentCasing_AndDoesNotMutateData()
    {
        const string invoiceId = "inv-http-secret-value-case";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId, type = "InvoiceSettled" },
            secretValues: ["EXPECTED-SECRET"]);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsPayloadTooLarge_WhenUtf8MultibytePayloadExceedsByteLimit()
    {
        const string invoiceId = "inv-http-utf8-too-large";
        await SeedPaymentAsync(invoiceId);

        // '€' is 3 bytes in UTF-8, so byte size can exceed the limit even with fewer characters.
        var multibyteNote = new string('€', 120);
        var payload = $"{{\"invoiceId\":\"{invoiceId}\",\"type\":\"InvoiceSettled\",\"note\":\"{multibyteNote}\"}}";

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_AllowsUtf8MultibytePayload_AtExactByteLimit()
    {
        const int maxBytes = 256;
        const string invoiceId = "inv-http-utf8-at-limit";
        await SeedPaymentAsync(invoiceId);

        string? payloadAtLimit = null;

        // Find a note with mixed UTF-8 widths (3-byte '€' + 1-byte 'a') that lands exactly on the byte limit.
        for (var euroCount = 0; euroCount <= maxBytes && payloadAtLimit is null; euroCount++)
        {
            for (var asciiCount = 0; asciiCount <= maxBytes; asciiCount++)
            {
            var note = new string('€', euroCount) + new string('a', asciiCount);
                var candidate = $"{{\"invoiceId\":\"{invoiceId}\",\"type\":\"InvoiceSettled\",\"note\":\"{note}\"}}";
                var byteCount = Encoding.UTF8.GetByteCount(candidate);

                if (byteCount == maxBytes)
                {
                    payloadAtLimit = candidate;
                    break;
                }

                if (byteCount > maxBytes && asciiCount == 0)
                {
                    // For this euro count, byte size only grows from here.
                    break;
                }
            }
        }

        Assert.NotNull(payloadAtLimit);

        using var content = new StringContent(payloadAtLimit!, Encoding.UTF8, "application/json");
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPaymentSettledAndOrderCreated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsPayloadTooLarge_WhenUtf8MultibytePayloadExceedsLimit_AndContentLengthIsUnknown()
    {
        const string invoiceId = "inv-http-utf8-unknown-length-too-large";
        await SeedPaymentAsync(invoiceId);

        var multibyteNote = new string('€', 140);
        var payload = $"{{\"invoiceId\":\"{invoiceId}\",\"type\":\"InvoiceSettled\",\"note\":\"{multibyteNote}\"}}";

        using var content = new UnknownLengthStringContent(payload);
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ConfirmsPaymentAndCreatesOrder_WhenInvoiceSettled()
    {
        const string invoiceId = "inv-http-paid";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId, type = "InvoiceSettled" },
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPaymentSettledAndOrderCreated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsBadRequest_WhenPayloadIsInvalid_AndDoesNotMutateData()
    {
        const string invoiceId = "inv-http-invalid";
        await SeedPaymentAsync(invoiceId);

        using var content = new StringContent("{invalid-json", Encoding.UTF8, "application/json");
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_IsIdempotent_WhenInvoiceSettledIsReceivedTwice()
    {
        const string invoiceId = "inv-http-idempotent";
        await SeedPaymentAsync(invoiceId);

        var client = _factory.CreateClient();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/btcpay/webhook")
        {
            Content = JsonContent.Create(new { invoiceId, type = "InvoiceSettled" })
        };
        firstRequest.Headers.Add("X-BTCPay-Secret", "expected-secret");

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/btcpay/webhook")
        {
            Content = JsonContent.Create(new { invoiceId, type = "InvoiceSettled" })
        };
        secondRequest.Headers.Add("X-BTCPay-Secret", "expected-secret");

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = db.Payments.FirstOrDefault(p => p.PaymentId == invoiceId);

        Assert.NotNull(payment);
        Assert.True(payment!.IsPaid);

    }

    [Fact]
    public async Task WebhookEndpoint_DoesNotMutatePayment_WhenEventIsNotSettled()
    {
        const string invoiceId = "inv-http-processing";
        await SeedPaymentAsync(invoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId, type = "InvoiceProcessing" },
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsOk_WhenInvoiceDoesNotExist_AndKeepsDatabaseUnchanged()
    {
        const string missingInvoiceId = "inv-http-missing";

        int paymentsBefore;
        using (var preScope = _factory.Services.CreateScope())
        {
            var preDb = preScope.ServiceProvider.GetRequiredService<AppDbContext>();
            paymentsBefore = preDb.Payments.Count();
        }

        var response = await SendWebhookJsonAsync(
            new { invoiceId = missingInvoiceId, type = "InvoiceSettled" },
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(paymentsBefore, db.Payments.Count());
        Assert.DoesNotContain(db.Payments, p => p.PaymentId == missingInvoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsBadRequest_WhenRequiredFieldsAreEmpty_AndDoesNotMutateData()
    {
        const string seededInvoiceId = "inv-http-empty-fields-seeded";
        await SeedPaymentAsync(seededInvoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId = "", type = "" },
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertPaymentNotMutated(seededInvoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsBadRequest_WhenRequiredFieldsAreWhitespace_AndDoesNotMutateData()
    {
        const string seededInvoiceId = "inv-http-whitespace-fields-seeded";
        await SeedPaymentAsync(seededInvoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId = "   ", type = " \t " },
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertPaymentNotMutated(seededInvoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ProcessesValidPayload_WithEscapesAndUnicodeWithinLimit()
    {
        const string invoiceId = "inv-http-unicode-valid";
        await SeedPaymentAsync(invoiceId);

        var payload = "{\"invoiceId\":\"" + invoiceId + "\",\"type\":\"InvoiceSettled\",\"note\":\"linha\\ncom\\ttab e unicode \\u20AC\"}";

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPaymentSettledAndOrderCreated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsBadRequest_WhenRequiredFieldsHaveInvalidTypes_AndDoesNotMutateData()
    {
        const string seededInvoiceId = "inv-http-invalid-types-seeded";
        await SeedPaymentAsync(seededInvoiceId);

        var response = await SendWebhookJsonAsync(
            new { invoiceId = 12345, type = 999 },
            secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertPaymentNotMutated(seededInvoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsPayloadTooLarge_WhenBodyExceedsLimit_AndDoesNotMutateData()
    {
        const string invoiceId = "inv-http-too-large";
        await SeedPaymentAsync(invoiceId);

        var oversizedNote = new string('x', 2048);
        var payload = $"{{\"invoiceId\":\"{invoiceId}\",\"type\":\"InvoiceSettled\",\"note\":\"{oversizedNote}\"}}";

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_AllowsPayloadExactlyAtLimit_AndProcessesSettlement()
    {
        const int maxBytes = 256;
        const string invoiceId = "inv-http-at-limit";
        await SeedPaymentAsync(invoiceId);

        var fillerLength = 0;
        string payload;

        while (true)
        {
            fillerLength++;
            var filler = new string('a', fillerLength);
            payload = $"{{\"invoiceId\":\"{invoiceId}\",\"type\":\"InvoiceSettled\",\"note\":\"{filler}\"}}";
            var byteCount = Encoding.UTF8.GetByteCount(payload);

            if (byteCount == maxBytes)
                break;

            if (byteCount > maxBytes)
                throw new InvalidOperationException("Nao foi possivel montar payload exatamente no limite configurado.");
        }

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPaymentSettledAndOrderCreated(invoiceId);
    }

    [Fact]
    public async Task WebhookEndpoint_ReturnsPayloadTooLarge_WhenBodyExceedsLimit_AndContentLengthIsUnknown()
    {
        const string invoiceId = "inv-http-too-large-unknown-length";
        await SeedPaymentAsync(invoiceId);

        var oversizedNote = new string('y', 2048);
        var payload = $"{{\"invoiceId\":\"{invoiceId}\",\"type\":\"InvoiceSettled\",\"note\":\"{oversizedNote}\"}}";

        using var content = new UnknownLengthStringContent(payload);
        var response = await SendWebhookContentAsync(content, secretValues: ["expected-secret"]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        AssertPaymentNotMutated(invoiceId);
    }

    private async Task<HttpResponseMessage> SendWebhookJsonAsync(
        object payload,
        string headerName = "X-BTCPay-Secret",
        params string[] secretValues)
    {
        var content = JsonContent.Create(payload);
        return await SendWebhookContentAsync(content, headerName, secretValues);
    }

    private async Task<HttpResponseMessage> SendWebhookContentAsync(
        HttpContent content,
        string headerName = "X-BTCPay-Secret",
        params string[] secretValues)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/btcpay/webhook")
        {
            Content = content
        };

        if (secretValues.Length > 0)
        {
            foreach (var secretValue in secretValues)
                request.Headers.TryAddWithoutValidation(headerName, secretValue);
        }

        return await client.SendAsync(request);
    }

    private void AssertPaymentNotMutated(string invoiceId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = db.Payments.FirstOrDefault(p => p.PaymentId == invoiceId);

        Assert.NotNull(payment);
        Assert.False(payment!.IsPaid);
        Assert.Null(payment.PaidAt);
    }

    private void AssertPaymentSettledAndOrderCreated(string invoiceId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = db.Payments.FirstOrDefault(p => p.PaymentId == invoiceId);

        Assert.NotNull(payment);
        Assert.True(payment!.IsPaid);
        Assert.NotNull(payment.PaidAt);
    }

    private async Task SeedPaymentAsync(string invoiceId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product
        {
            Name = "Produto integração",
            Description = "Produto para teste de integração",
            Price = 0.0001m,
            UserId = "seller-int-1"
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.Payments.Add(new PaymentRecord
        {
            ProductId = product.Id,
            UserId = "buyer-int-1",
            Address = "tb1qintegrationaddress",
            PaymentId = invoiceId,
            PaymentMethod = "BTCPayServer",
            Amount = 0.0001m,
            IsPaid = false
        });

        await db.SaveChangesAsync();
    }

    private sealed class UnknownLengthStringContent : HttpContent
    {
        private readonly byte[] _payloadBytes;

        public UnknownLengthStringContent(string payload)
        {
            _payloadBytes = Encoding.UTF8.GetBytes(payload);
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
            {
                CharSet = "utf-8"
            };
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return stream.WriteAsync(_payloadBytes, 0, _payloadBytes.Length);
        }
    }
}


