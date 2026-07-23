using System.Globalization;
using Confirmai.Configuration;
using Confirmai.Services.Factories;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Confirmai.Pages.Payment.Components;

public partial class EventPaymentGateways
{
    [Parameter] public List<EventPaymentGatewayOption> AvailableGateways { get; set; } = new();
    [Parameter] public IReadOnlyList<EventPaymentGatewayOption> ComingSoonGateways { get; set; } = new List<EventPaymentGatewayOption>();
    [Parameter] public string? SelectedGatewayName { get; set; }
    [Parameter] public bool GroupGatewaysEnabled { get; set; }
    [Parameter] public bool IsGenerating { get; set; }
    [Parameter] public string? ErrorMsg { get; set; }
    [Parameter] public decimal Price { get; set; }
    [Parameter] public string? SelectedGatewayDisplayName { get; set; }

    [Parameter] public EventCallback<string> OnSelectGateway { get; set; }
    [Parameter] public EventCallback OnGenerateCharge { get; set; }

    [Inject] private IOptions<FeeOptions> FeeOptions { get; set; } = default!;

    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    private bool SelectedGatewaySupportsFee =>
        !string.IsNullOrWhiteSpace(SelectedGatewayName) &&
        FeeOptions.Value.SupportedGateways.Contains(SelectedGatewayName, StringComparer.OrdinalIgnoreCase);

    private decimal AppFeeAmount
    {
        get
        {
            if (!FeeOptions.Value.IsConfigured || !SelectedGatewaySupportsFee)
                return 0m;

            return FeeOptions.Value.AppFeeFixed;
        }
    }

    private decimal GatewayFeeAmount
    {
        get
        {
            if (!FeeOptions.Value.IsConfigured || !SelectedGatewaySupportsFee)
                return 0m;

            return FeeOptions.Value.GatewayFeeFixed;
        }
    }

    private decimal TotalAmount
    {
        get
        {
            if (!FeeOptions.Value.IsConfigured || !SelectedGatewaySupportsFee)
                return Price;

            return Price + AppFeeAmount + GatewayFeeAmount;
        }
    }

    private async Task HandleSelectGateway(string gatewayName)
        => await OnSelectGateway.InvokeAsync(gatewayName);

    private async Task HandleGenerateCharge()
        => await OnGenerateCharge.InvokeAsync();
}
