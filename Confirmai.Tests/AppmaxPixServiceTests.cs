using Confirmai.Services.Payment;
using System.Text.Json;

namespace Confirmai.Tests;

public class AppmaxPixServiceTests
{
    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("5511987654321", "11987654321")]
    [InlineData("55119876543210", "19876543210")]
    [InlineData("11987654321", "11987654321")]
    [InlineData("+55 11 98765-4321", "11987654321")]
    public void NormalizePhone_ValidPhones_ReturnsNormalizedDigits(string input, string expected)
    {
        // Use reflection to call private static method
        var method = typeof(AppmaxPixService).GetMethod("NormalizePhone", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    public void NormalizePhone_InvalidPhones_ReturnsNull(string? input)
    {
        var method = typeof(AppmaxPixService).GetMethod("NormalizePhone", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object?[] { input }) as string;
        
        Assert.Null(result);
    }

    [Theory]
    [InlineData(10.00, 1000)]
    [InlineData(10.50, 1050)]
    [InlineData(0.01, 1)]
    [InlineData(100.99, 10099)]
    [InlineData(10.555, 1056)] // Rounds up
    [InlineData(10.554, 1055)] // Rounds down
    public void ToCents_ConvertsCorrectly(decimal amount, int expectedCents)
    {
        var method = typeof(AppmaxPixService).GetMethod("ToCents", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { amount }) as int?;
        
        Assert.Equal(expectedCents, result);
    }

    [Theory]
    [InlineData("aprovado", true)]
    [InlineData("APROVADO", true)]
    [InlineData("Aprovado", true)]
    [InlineData("approved", true)]
    [InlineData("pago", true)]
    [InlineData("paid", true)]
    [InlineData("integrado", true)]
    [InlineData("pendente", false)]
    [InlineData("cancelado", false)]
    [InlineData("", false)]
    public void IsPaidOrderStatus_RecognizesPaidStatuses(string status, bool expected)
    {
        var method = typeof(AppmaxPixService).GetMethod("IsPaidOrderStatus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { status }) as bool?;
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryGetInt64Path_FindsNumberInNestedPath()
    {
        var json = JsonDocument.Parse(@"{""data"":{""customer"":{""id"":12345}}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.customer.id" } }) as long?;
        
        Assert.Equal(12345, result);
    }

    [Fact]
    public void TryGetInt64Path_FindsNumberAsString()
    {
        var json = JsonDocument.Parse(@"{""data"":{""id"":""67890""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.id" } }) as long?;
        
        Assert.Equal(67890, result);
    }

    [Fact]
    public void TryGetInt64Path_TriesMultiplePaths()
    {
        var json = JsonDocument.Parse(@"{""order_id"":""999""}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.order.id", "data.id", "order_id", "id" } }) as long?;
        
        Assert.Equal(999, result);
    }

    [Fact]
    public void TryGetInt64Path_ReturnsNullWhenNotFound()
    {
        var json = JsonDocument.Parse(@"{""data"":{""name"":""test""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.id" } }) as long?;
        
        Assert.Null(result);
    }

    [Fact]
    public void TryGetStringPath_FindsStringInNestedPath()
    {
        var json = JsonDocument.Parse(@"{""data"":{""payment"":{""pix"":{""qr_code"":""00020126580014br.gov.bcb.pix""}}}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.payment.pix.qr_code" } }) as string;
        
        Assert.Equal("00020126580014br.gov.bcb.pix", result);
    }

    [Fact]
    public void TryGetStringPath_TriesMultiplePaths()
    {
        var json = JsonDocument.Parse(@"{""pix"":{""copy_paste"":""code123""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.payment.pix.qr_code", "data.pix.copy_paste", "pix.copy_paste" } }) as string;
        
        Assert.Equal("code123", result);
    }

    [Fact]
    public void TryGetStringPath_ReturnsNullWhenNotFound()
    {
        var json = JsonDocument.Parse(@"{""data"":{""name"":""test""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.code" } }) as string;
        
        Assert.Null(result);
    }

    [Fact]
    public void TryResolvePath_ResolvesNestedPath()
    {
        var json = JsonDocument.Parse(@"{""data"":{""customer"":{""id"":123}}}");
        var method = typeof(AppmaxPixService).GetMethod("TryResolvePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object?[] { json.RootElement, "data.customer.id", null };
        method?.Invoke(null, parameters);
        var result = parameters[2] as JsonElement?;
        
        Assert.NotNull(result);
        Assert.Equal(JsonValueKind.Number, result.Value.ValueKind);
    }

    [Fact]
    public void TryResolvePath_ReturnsFalseForInvalidPath()
    {
        var json = JsonDocument.Parse(@"{""data"":{""name"":""test""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryResolvePath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object?[] { json.RootElement, "data.nonexistent", null };
        var result = method?.Invoke(null, parameters) as bool?;

        Assert.False(result);
    }

    [Fact]
    public void TryGetStringPath_ReturnsNullForEmptyString()
    {
        var json = JsonDocument.Parse(@"{""data"":{""code"":""""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.code" } }) as string;

        Assert.Null(result);
    }

    [Fact]
    public void TryGetStringPath_ReturnsNullForWhitespaceString()
    {
        var json = JsonDocument.Parse(@"{""data"":{""code"":""   ""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.code" } }) as string;

        Assert.Null(result);
    }

    [Fact]
    public void TryGetInt64Path_ReturnsNullForNonNumericValue()
    {
        var json = JsonDocument.Parse(@"{""data"":{""id"":""not-a-number""}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.id" } }) as long?;

        Assert.Null(result);
    }

    [Fact]
    public void TryGetInt64Path_ReturnsNullForNonNumericElement()
    {
        var json = JsonDocument.Parse(@"{""data"":{""id"":true}}");
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { json.RootElement, new[] { "data.id" } }) as long?;

        Assert.Null(result);
    }

    [Theory]
    [InlineData("55129876543210", "29876543210")]
    [InlineData("55 12 98765-4321", "12987654321")]
    public void NormalizePhone_HandlesLongPhones(string input, string expected)
    {
        var method = typeof(AppmaxPixService).GetMethod("NormalizePhone",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object?[] { input }) as string;

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.00, 0)]
    [InlineData(0.99, 99)]
    [InlineData(999.99, 99999)]
    public void ToCents_HandlesEdgeCases(decimal amount, int expectedCents)
    {
        var method = typeof(AppmaxPixService).GetMethod("ToCents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { amount }) as int?;

        Assert.Equal(expectedCents, result);
    }
}
