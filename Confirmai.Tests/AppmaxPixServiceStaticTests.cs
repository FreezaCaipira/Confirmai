using Confirmai.Services.Payment;
using System.Globalization;
using System.Text.Json;

namespace Confirmai.Tests;

public class AppmaxPixServiceStaticTests
{
    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("5511987654321", "11987654321")]
    [InlineData("+55 11 98765-4321", "11987654321")]
    [InlineData("9876543210", "9876543210")]
    [InlineData("11987654321", "11987654321")]
    public void NormalizePhone_ValidPhones_ReturnsNormalized(string input, string expected)
    {
        // Act
        var method = typeof(AppmaxPixService).GetMethod("NormalizePhone", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { input }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    public void NormalizePhone_InvalidPhones_ReturnsNull(string? input)
    {
        // Act
        var method = typeof(AppmaxPixService).GetMethod("NormalizePhone", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { input }) as string;

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(10.00, 1000)]
    [InlineData(10.50, 1050)]
    [InlineData(0.01, 1)]
    [InlineData(100.00, 10000)]
    [InlineData(10.555, 1056)]
    [InlineData(10.554, 1055)]
    public void ToCents_ValidAmounts_ReturnsCents(decimal input, int expected)
    {
        // Act
        var method = typeof(AppmaxPixService).GetMethod("ToCents", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { input }) as int?;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("aprovado")]
    [InlineData("approved")]
    [InlineData("pago")]
    [InlineData("paid")]
    [InlineData("integrado")]
    [InlineData(" APROVADO ")]
    [InlineData(" Pago ")]
    public void IsPaidOrderStatus_PaidStatuses_ReturnsTrue(string status)
    {
        // Act
        var method = typeof(AppmaxPixService).GetMethod("IsPaidOrderStatus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { status }) as bool?;

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("pendente")]
    [InlineData("cancelado")]
    [InlineData("recusado")]
    [InlineData("pending")]
    [InlineData("cancelled")]
    public void IsPaidOrderStatus_UnpaidStatuses_ReturnsFalse(string status)
    {
        // Act
        var method = typeof(AppmaxPixService).GetMethod("IsPaidOrderStatus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { status }) as bool?;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryGetInt64Path_ValidPath_ReturnsValue()
    {
        // Arrange
        var json = @"{""order"": {""id"": 12345}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { root, new[] { "order.id" } }) as long?;

        // Assert
        Assert.Equal(12345, result);
    }

    [Fact]
    public void TryGetInt64Path_StringNumber_ReturnsParsedValue()
    {
        // Arrange
        var json = @"{""order"": {""id"": ""12345""}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { root, new[] { "order.id" } }) as long?;

        // Assert
        Assert.Equal(12345, result);
    }

    [Fact]
    public void TryGetInt64Path_InvalidPath_ReturnsNull()
    {
        // Arrange
        var json = @"{""order"": {""id"": 12345}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryGetInt64Path", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { root, new[] { "order.invalid" } }) as long?;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryGetStringPath_ValidPath_ReturnsValue()
    {
        // Arrange
        var json = @"{""order"": {""status"": ""aprovado""}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { root, new[] { "order.status" } }) as string;

        // Assert
        Assert.Equal("aprovado", result);
    }

    [Fact]
    public void TryGetStringPath_InvalidPath_ReturnsNull()
    {
        // Arrange
        var json = @"{""order"": {""status"": ""aprovado""}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryGetStringPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { root, new[] { "order.invalid" } }) as string;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryResolvePath_ValidPath_ReturnsTrue()
    {
        // Arrange
        var json = @"{""order"": {""status"": ""aprovado""}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryResolvePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { root, "order.status", null! };
        var result = (bool?)method?.Invoke(null, parameters);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryResolvePath_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var json = @"{""order"": {""status"": ""aprovado""}}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var method = typeof(AppmaxPixService).GetMethod("TryResolvePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { root, "order.invalid", null! };
        var result = (bool?)method?.Invoke(null, parameters);

        // Assert
        Assert.False(result);
    }
}
