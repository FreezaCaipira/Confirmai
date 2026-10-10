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

    private decimal BasePrice => Confirmation is not null ? EventCharge.PriceOf(Confirmation) ?? 0m : 0m;

    // C39-A: para o nao-carimbado a taxa resolve pela regra do esporte
    // (futsal = fixa; poker = % do evento/mesa), isencao incluida.
    // C39-C: o breakdown mostra o que o jogador paga — carimbo da parceria
    // vence a taxa cheia.
    private decimal ManualFee
        => Confirmation?.PlayerFeeAmount ?? Confirmation?.PlatformFeeAmount
           ?? FeePolicy.ResolveStampsForNewConfirmation(
                  Confirmation?.Event?.Group,
                  Confirmation is not null ? EventCharge.PriceOf(Confirmation) : null,
                  Confirmation?.ConfirmedAt ?? DateTime.UtcNow,
                  Confirmation?.Event?.PlatformFeePercent).PlayerFee
           ?? 0m;

    private decimal GetTotalAmount()
    {
        if (BasePrice <= 0m)
            return 0m;

        var basePrice = BasePrice;

        // V1 manual flow (no gateways): total via ManualPlatformFee so the fee
        // resolves by sport and to 0 while the group is waived —
        // never the V2 fee fields.
        if (Confirmation?.Event?.Group is { EnablePaymentGateways: false })
            return ManualPlatformFee.TotalToPay(
                gatewaysEnabled: false,
                basePrice,
                ManualFee);

        if (!FeeOptions.Value.IsConfigured)
            return basePrice;

        // V2 with gateways
        return basePrice + FeeOptions.Value.AppFeeFixed + FeeOptions.Value.GatewayFeeFixed;
    }
}
