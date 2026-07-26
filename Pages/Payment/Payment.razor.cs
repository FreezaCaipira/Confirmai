using Confirmai.Configuration;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Shared.Components;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Payment;

public partial class Payment : IAsyncDisposable
{
    [Inject] private PaymentInitializationService InitService { get; set; } = default!;
    [Inject] private PaymentCommandService PaymentCommands { get; set; } = default!;

    [Parameter] public int ProductId { get; set; }
    public Confirmai.Models.Product? product;
    private ApplicationUser? sellerUser;
    public string Address { get; set; } = "";
    public string? PaymentId { get; set; }
    public decimal Amount { get; set; } = 0.000001m;
    public bool IsPaid { get; set; } = false;
    public string SelectedMethod { get; set; } = "";
    private bool ShowPrivateKeyValue => SelectedMethod == "Testnet";
    [CascadingParameter] public Toast? ToastRef { get; set; }
    private const decimal MinBtcAmount = 0.000003m;
    private bool isLoading = false;
    private decimal? btcUsdRate;
    private decimal? btcBrlRate;
    private string? feedbackMessage;
    private string feedbackType = "info";
    private string copyIcon = "fas fa-copy";
    private bool UseSiteIntermediary = true;
    private int selectedPurchaseQuantity = 1;
    private int maxPurchaseQuantity = 1;
    private decimal? selectedOfferUnitPrice;
    private string offerCurrency = "BTC"; // BRL, USD or BTC
    private string? pixCurrencyNotice;
    private CancellationTokenSource? _copyCts;
    private Task? _copyTask = null;
    private string? pixSellerKeyNotice;

    private PaymentRecord? paymentRecord;

    private bool IsPixCurrentPayment =>
        string.Equals(paymentRecord?.PaymentMethod ?? SelectedMethod, "Pix", StringComparison.OrdinalIgnoreCase);

    public string QRCodeValue =>
        IsPixCurrentPayment
            ? Address
            : Address.StartsWith("lnbc")
            ? Address
            : $"bitcoin:{Address}?amount={Amount}";

    private List<GatewayInfo> ActiveGateways = new();

    protected override async Task OnInitializedAsync()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var (qty, maxQty, unitPrice, currency) = InitService.ParseQueryParameters(uri);

        selectedPurchaseQuantity = Math.Clamp(qty, 1, Math.Max(1, maxQty));
        maxPurchaseQuantity = maxQty;
        selectedOfferUnitPrice = unitPrice;
        offerCurrency = currency;

        isLoading = true;
        var (btcUsd, btcBrl) = await InitService.FetchBitcoinRatesAsync();
        btcUsdRate = btcUsd;
        btcBrlRate = btcBrl;

        product = await InitService.LoadProductAsync(ProductId);
        isLoading = false;

        sellerUser = await InitService.ResolveSeller(uri, product);

        if (product != null)
        {
            var unitPriceAmount = selectedOfferUnitPrice ?? product.Price;
            Amount = InitService.CalculateTotalAmount(unitPriceAmount, selectedPurchaseQuantity);
        }

        var (gateways, defaultMethod) = await InitService.SetupPaymentGatewaysAsync();
        ActiveGateways = gateways;
        SelectedMethod = defaultMethod;

        await OnSelectedMethodChangedAsync(false);

        PaymentEventBus.OnPaymentConfirmed += OnPaymentConfirmed;
    }

    private void OnPaymentConfirmed(string userId, string paymentId)
    {
        if (paymentId != this.PaymentId) return;

        InvokeAsync(async () =>
        {
            await CheckPayment();
        });
    }

    public async ValueTask DisposeAsync()
    {
        PaymentEventBus.OnPaymentConfirmed -= OnPaymentConfirmed;

        _copyCts?.Cancel();

        if (_copyTask is not null)
        {
            try { await _copyTask; }
            catch (OperationCanceledException) { }
        }

        _copyCts?.Dispose();
    }

    private async Task GenerateAddressCallback()
        => await GenerateAddress();

    private async Task CheckPaymentCallback()
        => await OnCheckPaymentClick();

    private async Task MethodChangedCallback(string method)
    {
        SelectedMethod = method;
        await OnSelectedMethodChangedAsync(true);
    }

    private async Task IntermediaryChangedCallback()
        => await OnUseSiteIntermediaryChangedAsync();

    private async Task CopyAddressCallback()
        => await CopyAddress();

    private async Task GenerateAddress()
    {
        if (!await ValidateOfferBeforePaymentAsync())
        {
            return;
        }

        if (string.Equals(SelectedMethod, "Pix", StringComparison.OrdinalIgnoreCase))
        {
            await GeneratePixPaymentAsync();
            return;
        }

        if (Amount < MinBtcAmount)
        {
            await PaymentCommands.LogWarningAsync($"Tentativa de pagamento abaixo do m\u00EDnimo: {Amount} BTC.", sellerUser?.Id);
            NotifyUser(string.Format(T["PaymentBuy.MinAmountWarning"], FormatBtcWithUsdText(MinBtcAmount)), "warning");
            return;
        }

        try
        {
            isLoading = true;

            AuthenticationState? authState = await AuthProvider.GetAuthenticationStateAsync();
            string? userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var result = await PaymentCommands.GenerateBtcAddressAsync(
                ProductId, Amount, SelectedMethod, sellerUser?.Id, userId, offerCurrency);

            if (result == null)
            {
                NotifyUser(string.Format(T["PaymentBuy.GenerateError"], "Falha ao gerar endere\u00E7o."), "error");
                return;
            }

            Address = result.Address;
            PaymentId = result.PaymentId;
            IsPaid = false;
            paymentRecord = result.PaymentRecord;

            await PaymentCommands.SavePaymentRecordAsync(paymentRecord);
            await PaymentCommands.LogAsync($"Endere\u00E7o/invoice gerado para produto {ProductId} via {SelectedMethod}.", userId);

            NotifyUser(T["PaymentBuy.Generated"], "success");
        }
        catch (Exception ex)
        {
            await PaymentCommands.LogAsync("Erro ao gerar endere\u00E7o/invoice.", sellerUser?.Id, ex);
            NotifyUser(string.Format(T["PaymentBuy.GenerateError"], ex.Message), "error");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnCheckPaymentClick()
    {
        await CheckPayment();
    }

    private async Task CheckPayment()
    {
        if (IsPixCurrentPayment && !AbacatePayOpts.Value.IsEnabled)
        {
            NotifyUser("A confirmacao automatica para PIX nao esta disponivel. Aguarde a validacao manual.", "info");
            return;
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            NotifyUser(T["PaymentBuy.NotFoundByAddress"], "error");
            return;
        }

        if (IsPaid)
        {
            NotifyUser(T["PaymentBuy.AlreadyConfirmed"], "success");
            return;
        }

        try
        {
            var payment = await PaymentCommands.FindPaymentByAddressAsync(Address);
            if (payment == null)
            {
                NotifyUser(T["PaymentBuy.NotFoundByAddress"], "error");
                return;
            }

            var result = await PaymentCommands.ConfirmPaymentAsync(payment);

            if (result.AlreadyPaid)
            {
                IsPaid = true;
                NotifyUser(T["PaymentBuy.AlreadyConfirmed"], "success");
                return;
            }

            if (result.Confirmed)
            {
                IsPaid = true;
                NotifyUser(T["PaymentBuy.Confirmed"], "success");
            }
            else
            {
                NotifyUser(string.Format(T["PaymentBuy.NotReceived"], FormatBtcWithUsdText(result.ReceivedAmount), FormatBtcWithUsdText(Amount)), "warning");
            }
        }
        catch (Exception ex)
        {
            await PaymentCommands.LogAsync("Erro ao verificar pagamento.", null, ex);
            NotifyUser(T["PaymentBuy.CheckError"], "error");
        }
    }

    private async Task OnSelectedMethodChangedAsync()
    {
        await OnSelectedMethodChangedAsync(true);
    }

    private async Task OnSelectedMethodChangedAsync(bool persistPreference)
    {
        if (!string.Equals(SelectedMethod, "Pix", StringComparison.OrdinalIgnoreCase))
        {
            pixCurrencyNotice = null;
            pixSellerKeyNotice = null;
            return;
        }

        if (string.Equals(CurrencyPreferenceService.SelectedFiatCurrency, "BRL", StringComparison.OrdinalIgnoreCase))
        {
            pixCurrencyNotice = null;
            return;
        }

        CurrencyPreferenceService.SetCurrency("BRL");
        if (persistPreference)
        {
            await JS.InvokeVoidAsync("localStorage.setItem", "Confirmai.fiatCurrency", CurrencyPreferenceService.SelectedFiatCurrency);
        }
        pixCurrencyNotice = "PIX funciona apenas com BRL. A cotacao foi alterada automaticamente para BRL.";
    }

    private Task OnUseSiteIntermediaryChangedAsync()
    {
        pixSellerKeyNotice = null;
        return Task.CompletedTask;
    }

    private void NotifyUser(string message, string type)
    {
        feedbackMessage = message;
        feedbackType = type;
        ToastRef?.Show(message, type);
    }

    private async Task CopyAddress()
    {
        if (!string.IsNullOrEmpty(Address))
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", Address);

            _copyCts?.Cancel();
            _copyCts = new CancellationTokenSource();
            var ct = _copyCts.Token;

            try
            {
                copyIcon = "fas fa-check";
                StateHasChanged();
                await Task.Delay(1500, ct);
                if (!ct.IsCancellationRequested)
                {
                    copyIcon = "fas fa-copy";
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                copyIcon = "fas fa-copy";
            }
        }
    }

    private string BuildSellerProfileUrl(string sellerUserId)
    {
        var safeUserId = Uri.EscapeDataString(sellerUserId ?? string.Empty);
        var currentRelativePath = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        var profileUrl = $"/profile/{safeUserId}?returnUrl={Uri.EscapeDataString(currentRelativePath)}";
        return profileUrl;
    }

    private string FormatOfferPrice(decimal amount)
    {
        return offerCurrency switch
        {
            "BRL" => $"R$ {amount:N2}",
            "USD" => $"$ {amount:N2}",
            _ => BtcUsdFormatter.Format(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency)
        };
    }

    private decimal GetUnitPriceAmount()
    {
        return selectedOfferUnitPrice ?? product?.Price ?? 0m;
    }

    private MarkupString FormatBtcWithUsdMarkup(decimal amount)
    {
        return BtcUsdFormatter.FormatMarkup(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
    }

    private string FormatBtcWithUsdText(decimal amount)
    {
        return BtcUsdFormatter.Format(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
    }

    private async Task GeneratePixPaymentAsync()
    {
        try
        {
            isLoading = true;
            pixSellerKeyNotice = null;

            AuthenticationState? authState = await AuthProvider.GetAuthenticationStateAsync();
            string? userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var result = await PaymentCommands.GeneratePixPaymentAsync(
                ProductId, Amount, SelectedMethod, sellerUser?.Id, userId, offerCurrency,
                AbacatePayOpts.Value.IsEnabled, product, UseSiteIntermediary);

            if (result == null)
            {
                if (!UseSiteIntermediary && string.IsNullOrWhiteSpace(await ResolvePixRecipientKeyForNoticeAsync()))
                {
                    pixSellerKeyNotice = "O vendedor nao possui chave PIX cadastrada no perfil. Escolha intermedio do ADM do site ou use outra forma de pagamento.";
                }
                else
                {
                    NotifyUser("Nao foi possivel gerar o pagamento PIX.", "error");
                }
                return;
            }

            Address = result.Address;
            PaymentId = result.PaymentId;
            IsPaid = false;
            paymentRecord = result.PaymentRecord;

            await PaymentCommands.SavePaymentRecordAsync(paymentRecord);
            await PaymentCommands.LogAsync(
                $"QR PIX gerado para produto {ProductId} via {(AbacatePayOpts.Value.IsEnabled ? "AbacatePay" : "payload estatico")}.",
                userId);

            NotifyUser("Pagamento PIX gerado. Escaneie o QR code ou copie o codigo PIX.", "success");
        }
        catch (Exception ex)
        {
            await PaymentCommands.LogAsync("Erro ao gerar pagamento PIX.", sellerUser?.Id, ex);
            NotifyUser($"Erro ao gerar pagamento PIX: {ex.Message}", "error");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task<string?> ResolvePixRecipientKeyForNoticeAsync()
    {
        if (UseSiteIntermediary)
        {
            var sitePixKey = await AdminSettingsService.GetSiteIntermediaryPixKeyAsync();
            return string.IsNullOrWhiteSpace(sitePixKey) ? null : sitePixKey.Trim();
        }

        var sellerUserId = sellerUser?.Id;
        if (string.IsNullOrWhiteSpace(sellerUserId))
            return null;

        return null;
    }

    private Task<bool> ValidateOfferBeforePaymentAsync() => Task.FromResult(true);
}
