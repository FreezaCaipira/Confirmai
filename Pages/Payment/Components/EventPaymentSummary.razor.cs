using System.Globalization;
using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentSummary
{
    [Parameter] public EventConfirmation? Confirmation { get; set; }

    /// <summary>Set true when the group is in V1 manual mode (no gateways) and the fee breakdown should be shown.</summary>
    [Parameter] public bool ShowFeeBreakdown { get; set; }

    [Inject] private IOptions<FeeOptions> FeeOptions { get; set; } = default!;

    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    private decimal BasePrice => Confirmation?.Event?.Price ?? 0m;

    private decimal ManualFee => FeeOptions.Value.ManualPlatformFeeFixed;

    private decimal GetTotalAmount()
    {
        if (Confirmation?.Event?.Price is null or <= 0m)
            return 0m;

        var basePrice = Confirmation.Event.Price.Value;

        if (ShowFeeBreakdown)
        {
            return basePrice + FeeOptions.Value.ManualPlatformFeeFixed;
        }

        if (!FeeOptions.Value.IsConfigured)
            return basePrice;

        // V2 with gateways
        return basePrice + FeeOptions.Value.AppFeeFixed + FeeOptions.Value.GatewayFeeFixed;
    }
}
