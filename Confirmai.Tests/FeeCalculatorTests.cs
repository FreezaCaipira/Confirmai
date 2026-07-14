using Confirmai.Services.Payment;
using Xunit;

namespace Confirmai.Tests;

public class FeeCalculatorTests
{
    [Fact]
    public void Calculate_WithZeroFee_ReturnsBaseAmount()
    {
        // Arrange
        var calculator = new FeeCalculator(0);
        var baseAmount = 15.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(15.00m, result.BaseAmount);
        Assert.Equal(0.00m, result.FeeAmount);
        Assert.Equal(15.00m, result.TotalAmount);
        Assert.Equal(0, result.FeePercentBps);
    }

    [Fact]
    public void Calculate_With4PercentFee_CalculatesCorrectly()
    {
        // Arrange
        var calculator = new FeeCalculator(400); // 4% = 400 bps
        var baseAmount = 15.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(15.00m, result.BaseAmount);
        Assert.Equal(0.60m, result.FeeAmount); // 15.00 * 0.04 = 0.60
        Assert.Equal(15.60m, result.TotalAmount); // 15.00 + 0.60 = 15.60
        Assert.Equal(400, result.FeePercentBps);
    }

    [Fact]
    public void Calculate_WithFractionalAmount_RoundsToCents()
    {
        // Arrange
        var calculator = new FeeCalculator(400); // 4%
        var baseAmount = 15.33m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(15.33m, result.BaseAmount);
        Assert.Equal(0.61m, result.FeeAmount); // 15.33 * 0.04 = 0.6132 -> round to 0.61
        Assert.Equal(15.94m, result.TotalAmount); // 15.33 + 0.61 = 15.94
    }

    [Fact]
    public void Calculate_WithSmallAmount_CalculatesCorrectly()
    {
        // Arrange
        var calculator = new FeeCalculator(400); // 4%
        var baseAmount = 1.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(1.00m, result.BaseAmount);
        Assert.Equal(0.04m, result.FeeAmount); // 1.00 * 0.04 = 0.04
        Assert.Equal(1.04m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_WithLargeAmount_CalculatesCorrectly()
    {
        // Arrange
        var calculator = new FeeCalculator(400); // 4%
        var baseAmount = 1000.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(1000.00m, result.BaseAmount);
        Assert.Equal(40.00m, result.FeeAmount); // 1000.00 * 0.04 = 40.00
        Assert.Equal(1040.00m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_WithDifferentPercent_CalculatesCorrectly()
    {
        // Arrange
        var calculator = new FeeCalculator(500); // 5%
        var baseAmount = 20.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(20.00m, result.BaseAmount);
        Assert.Equal(1.00m, result.FeeAmount); // 20.00 * 0.05 = 1.00
        Assert.Equal(21.00m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_WithNegativeBaseAmount_ThrowsException()
    {
        // Arrange
        var calculator = new FeeCalculator(400);
        var baseAmount = -10.00m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => calculator.Calculate(baseAmount));
    }

    [Fact]
    public void Calculate_WithZeroBaseAmount_ReturnsZero()
    {
        // Arrange
        var calculator = new FeeCalculator(400);
        var baseAmount = 0.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        Assert.Equal(0.00m, result.BaseAmount);
        Assert.Equal(0.00m, result.FeeAmount);
        Assert.Equal(0.00m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_RoundingMidpoint_RoundsAwayFromZero()
    {
        // Arrange
        var calculator = new FeeCalculator(333); // 3.33%
        var baseAmount = 10.00m;

        // Act
        var result = calculator.Calculate(baseAmount);

        // Assert
        // 10.00 * 1.0333 = 10.333 -> round to 10.33 (away from zero)
        Assert.Equal(10.00m, result.BaseAmount);
        Assert.Equal(0.33m, result.FeeAmount);
        Assert.Equal(10.33m, result.TotalAmount);
    }

    [Fact]
    public void Constructor_WithNegativeFeePercent_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new FeeCalculator(-100));
    }
}
