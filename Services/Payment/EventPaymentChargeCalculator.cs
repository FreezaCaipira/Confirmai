using Confirmai.Configuration;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

public class EventPaymentChargeCalculator
{
    private readonly FeeOptions _feeOptions;

    public EventPaymentChargeCalculator(IOptions<FeeOptions> feeOptions)
    {
        _feeOptions = feeOptions.Value;
    }

    public EventPaymentFeeCalculation Calculate(decimal basePrice, string gatewayName)
    {
        if (!_feeOptions.IsConfigured)
            return new EventPaymentFeeCalculation(basePrice, 0m);

        if (!_feeOptions.SupportedGateways.Contains(gatewayName, StringComparer.OrdinalIgnoreCase))
            return new EventPaymentFeeCalculation(basePrice, 0m);

        var chargeAmount = basePrice + _feeOptions.AppFeeFixed + _feeOptions.GatewayFeeFixed;
        var totalFee = _feeOptions.AppFeeFixed + _feeOptions.GatewayFeeFixed;
        var serviceFeePercentage = totalFee > 0 ? (totalFee / chargeAmount) * 100 : 0m;

        return new EventPaymentFeeCalculation(chargeAmount, serviceFeePercentage);
    }
}

public record EventPaymentFeeCalculation(decimal ChargeAmount, decimal ServiceFeePercentage);
