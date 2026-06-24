using Confirmai.Services.Core;

namespace Confirmai.Tests;

public class OperationFeeBreakdownTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var percent = 5.5m;
        var feeAmount = 10.25m;
        var netAmount = 89.75m;

        // Act
        var breakdown = new OperationFeeBreakdown(percent, feeAmount, netAmount);

        // Assert
        Assert.Equal(percent, breakdown.Percent);
        Assert.Equal(feeAmount, breakdown.FeeAmount);
        Assert.Equal(netAmount, breakdown.NetAmount);
    }

    [Fact]
    public void Constructor_WithZeroValues()
    {
        // Arrange
        var percent = 0m;
        var feeAmount = 0m;
        var netAmount = 100m;

        // Act
        var breakdown = new OperationFeeBreakdown(percent, feeAmount, netAmount);

        // Assert
        Assert.Equal(0m, breakdown.Percent);
        Assert.Equal(0m, breakdown.FeeAmount);
        Assert.Equal(100m, breakdown.NetAmount);
    }

    [Fact]
    public void Constructor_WithLargeValues()
    {
        // Arrange
        var percent = 15.75m;
        var feeAmount = 1575.50m;
        var netAmount = 8424.50m;

        // Act
        var breakdown = new OperationFeeBreakdown(percent, feeAmount, netAmount);

        // Assert
        Assert.Equal(15.75m, breakdown.Percent);
        Assert.Equal(1575.50m, breakdown.FeeAmount);
        Assert.Equal(8424.50m, breakdown.NetAmount);
    }
}
