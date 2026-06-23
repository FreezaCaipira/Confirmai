using Confirmai.Services.Interfaces;

namespace Confirmai.Tests;

public class EventPaymentChargeResultTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var chargeId = "charge-123";
        var brCode = "qr-code-456";

        // Act
        var result = new EventPaymentChargeResult(chargeId, brCode);

        // Assert
        Assert.Equal(chargeId, result.ChargeId);
        Assert.Equal(brCode, result.BrCode);
    }

    [Fact]
    public void Constructor_WithEmptyStrings()
    {
        // Act
        var result = new EventPaymentChargeResult(string.Empty, string.Empty);

        // Assert
        Assert.Equal(string.Empty, result.ChargeId);
        Assert.Equal(string.Empty, result.BrCode);
    }

    [Fact]
    public void Constructor_WithNullValues()
    {
        // Act
        var result = new EventPaymentChargeResult(null!, null!);

        // Assert
        Assert.Null(result.ChargeId);
        Assert.Null(result.BrCode);
    }

    [Fact]
    public void Constructor_WithLongStrings()
    {
        // Arrange
        var chargeId = new string('A', 100);
        var brCode = new string('B', 200);

        // Act
        var result = new EventPaymentChargeResult(chargeId, brCode);

        // Assert
        Assert.Equal(100, result.ChargeId.Length);
        Assert.Equal(200, result.BrCode.Length);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters()
    {
        // Arrange
        var chargeId = "charge-123@#$%";
        var brCode = "qr-code-456&*()";

        // Act
        var result = new EventPaymentChargeResult(chargeId, brCode);

        // Assert
        Assert.Equal(chargeId, result.ChargeId);
        Assert.Equal(brCode, result.BrCode);
    }
}
