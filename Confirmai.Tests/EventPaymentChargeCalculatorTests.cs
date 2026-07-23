using Confirmai.Configuration;
using Confirmai.Services.Payment;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Confirmai.Tests;

public class EventPaymentChargeCalculatorTests
{
    private static EventPaymentChargeCalculator CreateCalculator(FeeOptions feeOptions)
    {
        var mock = new Mock<IOptions<FeeOptions>>();
        mock.SetupGet(x => x.Value).Returns(feeOptions);
        return new EventPaymentChargeCalculator(mock.Object);
    }
    [Fact]
    public void Calculate_FeeNotConfigured_ReturnsBasePriceNoFee()
    {
        var feeOptions = new FeeOptions { Enabled = false, AppFeeFixed = 0, GatewayFeeFixed = 0 };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "EfiBank");

        Assert.Equal(25.00m, result.ChargeAmount);
        Assert.Equal(0m, result.ServiceFeePercentage);
    }

    [Fact]
    public void Calculate_FeeConfigured_UnsupportedGateway_ReturnsBasePriceNoFee()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 1.00m,
            GatewayFeeFixed = 0.50m,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "AbacatePay");

        Assert.Equal(25.00m, result.ChargeAmount);
        Assert.Equal(0m, result.ServiceFeePercentage);
    }

    [Fact]
    public void Calculate_FeeConfigured_SupportedGateway_AddsFixedFees()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 1.00m,
            GatewayFeeFixed = 0.50m,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "EfiBank");

        Assert.Equal(26.50m, result.ChargeAmount);
    }

    [Fact]
    public void Calculate_FeeConfigured_SupportedGateway_ComputesPercentage()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 1.00m,
            GatewayFeeFixed = 0.50m,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "EfiBank");

        var totalFee = 1.50m;
        var expectedPercentage = (totalFee / 26.50m) * 100;
        Assert.Equal(expectedPercentage, result.ServiceFeePercentage);
    }

    [Fact]
    public void Calculate_FeeConfigured_ZeroFees_ReturnsBasePriceZeroPercentage()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 0,
            GatewayFeeFixed = 0,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "EfiBank");

        Assert.Equal(25.00m, result.ChargeAmount);
        Assert.Equal(0m, result.ServiceFeePercentage);
    }

    [Fact]
    public void Calculate_GatewayNameCaseInsensitive_MatchesSupportedGateway()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 1.00m,
            GatewayFeeFixed = 0.50m,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "efibank");

        Assert.Equal(26.50m, result.ChargeAmount);
    }

    [Fact]
    public void Calculate_FeeConfiguredButNotEnabled_ReturnsBasePriceNoFee()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = false,
            AppFeeFixed = 1.00m,
            GatewayFeeFixed = 0.50m,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 25.00m, gatewayName: "EfiBank");

        Assert.Equal(25.00m, result.ChargeAmount);
        Assert.Equal(0m, result.ServiceFeePercentage);
    }

    [Fact]
    public void Calculate_ZeroBasePrice_WithFees_ReturnsOnlyFees()
    {
        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 1.00m,
            GatewayFeeFixed = 0.50m,
            SupportedGateways = ["EfiBank"]
        };
        var calculator = CreateCalculator(feeOptions);

        var result = calculator.Calculate(basePrice: 0m, gatewayName: "EfiBank");

        Assert.Equal(1.50m, result.ChargeAmount);
    }
}
