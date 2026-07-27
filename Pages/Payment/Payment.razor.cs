using Confirmai.Configuration;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Shared.Components;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Payment;

public partial class Payment : IAsyncDisposable
{
    [Inject] private PaymentInitializationService InitService { get; set; } = default!;
    [Inject] private PaymentCommandService PaymentCommands { get; set; } = default!;
    [Inject] private PaymentPageOrchestrator Orchestrator { get; set; } = default!;

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
    private string offerCurrency = "BTC";
    private string? pixCurrencyNotice;
    private CancellationTokenSource? _copyCts;
    private Task? _copyTask = null;
    private string? pixSellerKeyNotice;
    private PaymentRecord? paymentRecord;
    private List<GatewayInfo> ActiveGateways = new();

    private bool IsPixCurrentPayment =>
        string.Equals(paymentRecord?.PaymentMethod ?? SelectedMethod, "Pix", StringComparison.OrdinalIgnoreCase);

    public string QRCodeValue =>
        IsPixCurrentPayment ? Address
        : Address.StartsWith("lnbc") ? Address
        : $"bitcoin:{Address}?amount={Amount}";

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
        InvokeAsync(async () => await CheckPayment());
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

    private async Task GenerateAddressCallback() => await GenerateAddress();
    private async Task CheckPaymentCallback() => await OnCheckPaymentClick();
    private async Task MethodChangedCallback(string method)
    {
        SelectedMethod = method;
        await OnSelectedMethodChangedAsync(true);
    }
    private async Task IntermediaryChangedCallback() => await OnUseSiteIntermediaryChangedAsync();
    private async Task CopyAddressCallback() => await CopyAddress();

    private async Task GenerateAddress()
    {
        if (!await ValidateOfferBeforePaymentAsync()) return;

        if (string.Equals(SelectedMethod, "Pix", StringComparison.OrdinalIgnoreCase))
        {
            await GeneratePixPaymentAsync();
            return;
        }

        isLoading = true;
        try
        {
            var result = await Orchestrator.GenerateBtcAddressAsync(
                ProductId, Amount, SelectedMethod, sellerUser?.Id, offerCurrency, MinBtcAmount);

            if (result == null || result.Error != null)
            {
                var msg = result?.Error ?? "Falha ao gerar endereco.";
                NotifyUser(string.Format(T["PaymentBuy.GenerateError"], msg), "error");
                return;
            }

            Address = result.Address;
            PaymentId = result.PaymentId;
            IsPaid = false;
            paymentRecord = result.PaymentRecord;
            NotifyUser(T["PaymentBuy.Generated"], "success");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task GeneratePixPaymentAsync()
    {
        isLoading = true;
        pixSellerKeyNotice = null;
        try
        {
            var result = await Orchestrator.GeneratePixPaymentAsync(
                ProductId, Amount, SelectedMethod, sellerUser?.Id, offerCurrency,
                UseSiteIntermediary, product);

            if (result == null)
            {
                NotifyUser("Nao foi possivel gerar o pagamento PIX.", "error");
                return;
            }

            if (result.SellerKeyNotice != null)
            {
                pixSellerKeyNotice = result.SellerKeyNotice;
                return;
            }

            if (result.Error != null)
            {
                NotifyUser($"Erro ao gerar pagamento PIX: {result.Error}", "error");
                return;
            }

            Address = result.Address;
            PaymentId = result.PaymentId;
            IsPaid = false;
            paymentRecord = result.PaymentRecord;
            NotifyUser("Pagamento PIX gerado. Escaneie o QR code ou copie o codigo PIX.", "success");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnCheckPaymentClick() => await CheckPayment();

    private async Task CheckPayment()
    {
        var result = await Orchestrator.CheckPaymentAsync(
            Address, IsPaid, IsPixCurrentPayment,
            T["PaymentBuy.NotFoundByAddress"],
            T["PaymentBuy.AlreadyConfirmed"],
            T["PaymentBuy.Confirmed"],
            T["PaymentBuy.CheckError"],
            "A confirmacao automatica para PIX nao esta disponivel. Aguarde a validacao manual.",
            FormatBtcWithUsdText);

        if (result.IsPaid)
            IsPaid = true;

        NotifyUser(result.Message, result.MessageType);
    }

    private async Task OnSelectedMethodChangedAsync() => await OnSelectedMethodChangedAsync(true);

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
            await JS.InvokeVoidAsync("localStorage.setItem", "Confirmai.fiatCurrency", CurrencyPreferenceService.SelectedFiatCurrency);
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
        if (string.IsNullOrEmpty(Address)) return;

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
                copyIcon = "fas fa-copy";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copyIcon = "fas fa-copy";
        }
    }

    private string BuildSellerProfileUrl(string sellerUserId)
    {
        var safeUserId = Uri.EscapeDataString(sellerUserId ?? string.Empty);
        var currentRelativePath = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        return $"/profile/{safeUserId}?returnUrl={Uri.EscapeDataString(currentRelativePath)}";
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

    private decimal GetUnitPriceAmount() => selectedOfferUnitPrice ?? product?.Price ?? 0m;

    private MarkupString FormatBtcWithUsdMarkup(decimal amount)
        => BtcUsdFormatter.FormatMarkup(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);

    private string FormatBtcWithUsdText(decimal amount)
        => BtcUsdFormatter.Format(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);

    private Task<bool> ValidateOfferBeforePaymentAsync() => Task.FromResult(true);
}
