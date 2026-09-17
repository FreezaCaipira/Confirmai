using System.Globalization;
using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentSummary
{
    [Parameter] public EventConfirmation? Confirmation { get; set; }

    /// <summary>Set true when the group is in V1 manual mode (no gateways) and the fee breakdown should be shown.</summary>
    [Parameter] public bool ShowFeeBreakdown { get; set; }

    [Inject] private IOptions<FeeOptions> FeeOptions { get; set; } = default!;
    [Inject] private PlatformFeePolicy FeePolicy { get; set; } = default!;

    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    private decimal BasePrice => Confirmation?.Event?.Price ?? 0m;

    private decimal ManualFee => FeePolicy.ResolveManualFee(Confirmation?.Event?.Group, DateTime.UtcNow);

    private decimal GetTotalAmount()
    {
        if (Confirmation?.Event?.Price is null or <= 0m)
            return 0m;

        var basePrice = Confirmation.Event.Price.Value;

        // V1 manual flow (no gateways): total via ManualPlatformFee so the fee
        // only applies to futsal and resolves to 0 while the group is waived —
        // never the V2 fee fields.
        if (Confirmation.Event?.Group is { EnablePaymentGateways: false })
            return ManualPlatformFee.TotalToPay(
                gatewaysEnabled: false,
                isFutsal: Confirmation.Event.Sport == Sport.Futsal,
                basePrice,
                ManualFee);

        if (!FeeOptions.Value.IsConfigured)
            return basePrice;

        // V2 with gateways
        return basePrice + FeeOptions.Value.AppFeeFixed + FeeOptions.Value.GatewayFeeFixed;
    }
}
