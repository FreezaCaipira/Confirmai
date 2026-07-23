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

    [Inject] private IOptions<FeeOptions> FeeOptions { get; set; } = default!;

    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    private decimal GetTotalAmount()
    {
        if (Confirmation?.Event?.Price is null)
            return 0m;

        var basePrice = Confirmation.Event.Price.Value;

        if (!FeeOptions.Value.IsConfigured)
            return basePrice;

        // Use fixed fees
        return basePrice + FeeOptions.Value.AppFeeFixed + FeeOptions.Value.GatewayFeeFixed;
    }
}
