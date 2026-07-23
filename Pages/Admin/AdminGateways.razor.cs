using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Payment;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class AdminGateways
{
    private List<GatewayInfo> methods = new();
    private HashSet<string> configuredGatewayNames = new();
    private bool isLoading = true;
    [CascadingParameter] public Toast ToastRef { get; set; } = default!;

    [Inject] private BitcoinPaymentFactory PaymentFactory { get; set; } = default!;
    [Inject] private EventPaymentGatewayFactory EventGatewayFactory { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private GatewayService GatewayService { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        methods = await GatewayService.GetAllAsync();
        configuredGatewayNames = EventGatewayFactory.GetConfiguredNames();
        isLoading = false;
    }

    private async Task OnSwitchChanged(GatewayInfo method)
    {
        var newValue = method.Enabled;

        if (!newValue)
        {
            var confirm = await JS.InvokeAsync<bool>("confirm", string.Format(T["AdminGateways.ConfirmDisable"], method.Name));
            if (!confirm)
            {
                // Reverte o valor visual do switch
                method.Enabled = true;
                StateHasChanged();
                return;
            }
        }

        var success = await GatewayService.SetStatusAsync(method.Name, newValue);
        if (success)
        {
            var actionText = newValue ? T["AdminGateways.EnabledAction"] : T["AdminGateways.DisabledAction"];
            await LogService.LogAsync(
                string.Format(T["AdminGateways.LogChanged"], method.Name, actionText),
                source: "Admin",
                level: newValue ? "Info" : "Warning"
            );
            ToastRef?.Show(string.Format(T["AdminGateways.ToastChanged"], method.Name, actionText), newValue ? "success" : "warning");
        }
        else
        {
            ToastRef?.Show(T["AdminGateways.SaveError"], "error");
            method.Enabled = !newValue;
            StateHasChanged();
        }
    }
}
