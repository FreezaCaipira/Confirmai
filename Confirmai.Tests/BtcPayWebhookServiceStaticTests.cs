using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Http;

namespace Confirmai.Tests;

public class BtcPayWebhookServiceStaticTests
{
    [Fact]
    public void IsValidWebhookSecret_ValidSecrets_ReturnsTrue()
    {
        var method = typeof(BtcPayWebhookService).GetMethod("IsValidWebhookSecret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { "secret123", "secret123" });
        Assert.True(result);
    }

    [Fact]
    public void IsValidWebhookSecret_DifferentSecrets_ReturnsFalse()
    {
        var method = typeof(BtcPayWebhookService).GetMethod("IsValidWebhookSecret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { "secret123", "secret456" });
        Assert.False(result);
    }

    [Fact]
    public void IsValidWebhookSecret_NullExpected_ReturnsFalse()
    {
        var method = typeof(BtcPayWebhookService).GetMethod("IsValidWebhookSecret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { null, "secret123" });
        Assert.False(result);
    }

    [Fact]
    public void IsValidWebhookSecret_NullReceived_ReturnsFalse()
    {
        var method = typeof(BtcPayWebhookService).GetMethod("IsValidWebhookSecret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { "secret123", null });
        Assert.False(result);
    }

    [Fact]
    public void IsValidWebhookSecret_EmptyExpected_ReturnsFalse()
    {
        var method = typeof(BtcPayWebhookService).GetMethod("IsValidWebhookSecret",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { "", "secret123" });
        Assert.False(result);
    }

    [Fact]
    public void GetSingleWebhookSecretHeader_ValidHeader_ReturnsValue()
    {
        var headers = new HeaderDictionary
        {
            { "X-BTCPay-Secret", "secret123" }
        };

        var method = typeof(BtcPayWebhookService).GetMethod("GetSingleWebhookSecretHeader",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { headers }) as string;
        Assert.Equal("secret123", result);
    }

    [Fact]
    public void GetSingleWebhookSecretHeader_MissingHeader_ReturnsNull()
    {
        var headers = new HeaderDictionary();

        var method = typeof(BtcPayWebhookService).GetMethod("GetSingleWebhookSecretHeader",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { headers }) as string;
        Assert.Null(result);
    }

    [Fact]
    public void GetSingleWebhookSecretHeader_MultipleValues_ReturnsNull()
    {
        var headers = new HeaderDictionary
        {
            { "X-BTCPay-Secret", new Microsoft.Extensions.Primitives.StringValues(new[] { "secret1", "secret2" }) }
        };

        var method = typeof(BtcPayWebhookService).GetMethod("GetSingleWebhookSecretHeader",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { headers }) as string;
        Assert.Null(result);
    }

    [Fact]
    public void TryParseWebhookPayload_ValidPayload_ReturnsTrue()
    {
        var body = @"{
            ""invoiceId"": ""inv-123"",
            ""type"": ""InvoiceSettled"",
            ""deliveryId"": ""del-456"",
            ""timestamp"": 1234567890
        }";

        var method = typeof(BtcPayWebhookService).GetMethod("TryParseWebhookPayload",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { body, null!, null!, null!, 0L, null!, null! };
        var result = (bool?)method?.Invoke(null, parameters);

        Assert.True(result);
        Assert.Equal("inv-123", parameters[1]);
        Assert.Equal("InvoiceSettled", parameters[2]);
        Assert.Equal("del-456", parameters[3]);
        Assert.Equal(1234567890L, parameters[4]);
    }

    [Fact]
    public void TryParseWebhookPayload_InvalidJson_ReturnsFalse()
    {
        var body = "{invalid-json}";

        var method = typeof(BtcPayWebhookService).GetMethod("TryParseWebhookPayload",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { body, null!, null!, null!, 0L, null!, null! };
        var result = (bool?)method?.Invoke(null, parameters);

        Assert.False(result);
    }

    [Fact]
    public void TryParseWebhookPayload_MissingRequiredFields_ReturnsFalse()
    {
        var body = @"{""invoiceId"": ""inv-123""}";

        var method = typeof(BtcPayWebhookService).GetMethod("TryParseWebhookPayload",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { body, null!, null!, null!, 0L, null!, null! };
        var result = (bool?)method?.Invoke(null, parameters);

        Assert.False(result);
    }
}
