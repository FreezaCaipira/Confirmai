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
            return new EventPaymentFeeCalculation(basePrice);

        if (!_feeOptions.SupportedGateways.Contains(gatewayName, StringComparer.OrdinalIgnoreCase))
            return new EventPaymentFeeCalculation(basePrice);

        var chargeAmount = basePrice + _feeOptions.AppFeeFixed + _feeOptions.GatewayFeeFixed;

        return new EventPaymentFeeCalculation(chargeAmount);
    }
}

public record EventPaymentFeeCalculation(decimal ChargeAmount);
