using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Shared;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Payment;

public partial class PaymentCheckoutPanel
{
    [Parameter]
    public string Address { get; set; } = string.Empty;

    [Parameter]
    public bool IsPaid { get; set; }

    [Parameter]
    public List<GatewayInfo> ActiveGateways { get; set; } = new();

    [Parameter]
    public string SelectedMethod { get; set; } = string.Empty;

    [Parameter]
    public bool UseSiteIntermediary { get; set; } = true;

    [Parameter]
    public string QRCodeValue { get; set; } = string.Empty;

    [Parameter]
    public string? PixCurrencyNotice { get; set; }

    [Parameter]
    public string? PixSellerKeyNotice { get; set; }

    [Parameter]
    public string? FeedbackMessage { get; set; }

    [Parameter]
    public string FeedbackType { get; set; } = "info";

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public bool IsPixCurrentPayment { get; set; }

    [Parameter]
    public string CopyIcon { get; set; } = "fas fa-copy";

    [Parameter]
    public bool AbacatePayIsEnabled { get; set; }

    [Parameter]
    public bool ShowPrivateKey { get; set; }

    [Parameter]
    public string? PrivateKey { get; set; }

    [Parameter]
    public EventCallback OnGeneratePayment { get; set; }

    [Parameter]
    public EventCallback OnCheckPayment { get; set; }

    [Parameter]
    public EventCallback<string> OnMethodChanged { get; set; }

    [Parameter]
    public EventCallback OnIntermediaryChanged { get; set; }

    [Parameter]
    public EventCallback OnCopyAddress { get; set; }

    [Inject] private UiTextService T { get; set; } = default!;

    private async Task HandleGeneratePayment()
    {
        await OnGeneratePayment.InvokeAsync();
    }

    private async Task HandleCheckPayment()
    {
        await OnCheckPayment.InvokeAsync();
    }

    private async Task HandleMethodChanged()
    {
        await OnMethodChanged.InvokeAsync(SelectedMethod);
    }

    private async Task HandleIntermediaryChanged()
    {
        await OnIntermediaryChanged.InvokeAsync();
    }

    private async Task HandleCopyAddress()
    {
        await OnCopyAddress.InvokeAsync();
    }
}
