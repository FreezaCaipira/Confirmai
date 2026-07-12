using System.Globalization;
using System.Text;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Pages.Payment;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Crypto;
using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Confirmai.Shared.Components;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Payment;

public partial class Payment : IAsyncDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;

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
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("qty", out var queryQty) && int.TryParse(queryQty.LastOrDefault(), out var parsedQty) && parsedQty > 0)
        {
            selectedPurchaseQuantity = parsedQty;
        }

        if (query.TryGetValue("maxQty", out var queryMaxQty) && int.TryParse(queryMaxQty.LastOrDefault(), out var parsedMaxQty) && parsedMaxQty > 0)
        {
            maxPurchaseQuantity = parsedMaxQty;
        }

        if (query.TryGetValue("unitPrice", out var queryUnitPrice)
            && decimal.TryParse(queryUnitPrice.LastOrDefault(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedUnitPrice)
            && parsedUnitPrice > 0m)
        {
            selectedOfferUnitPrice = parsedUnitPrice;
        }

        if (query.TryGetValue("currency", out var queryCurrency))
        {
            var c = queryCurrency.LastOrDefault()?.ToUpperInvariant();
            if (c == "BRL" || c == "USD" || c == "BTC")
                offerCurrency = c;
        }

        selectedPurchaseQuantity = Math.Clamp(selectedPurchaseQuantity, 1, Math.Max(1, maxPurchaseQuantity));

        isLoading = true;
        var quote = await BitcoinQuoteService.GetQuoteAsync();
        btcUsdRate = quote?.btc_usd;
        btcBrlRate = quote?.btc_brl;

        await using var db = await DbFactory.CreateDbContextAsync();
        product = await db.Products.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == ProductId);
        isLoading = false;

        if (query.TryGetValue("sellerId", out var querySellerIdVal))
        {
            var sellerIdStr = querySellerIdVal.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(sellerIdStr))
                sellerUser = await db.Users.FirstOrDefaultAsync(u => u.Id == sellerIdStr);
        }
        sellerUser ??= product?.User;

        if (product != null)
        {
            var unitPrice = selectedOfferUnitPrice ?? product.Price;
            Amount = unitPrice * selectedPurchaseQuantity;
        }

        ActiveGateways = (await GatewayService.GetAllAsync()).Where(g => g.Enabled).ToList();
        SelectedMethod = ActiveGateways.Any(g => g.Name == "Pix")
            ? "Pix"
            : ActiveGateways.FirstOrDefault()?.Name ?? "";

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
        // Validate the seller's offer is still available before hitting the payment gateway.
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
            await LogService.LogAsync(
                message: $"Tentativa de pagamento abaixo do mínimo: {Amount} BTC.",
                source: "Payment",
                level: "Warning",
                userId: sellerUser?.Id
            );
            NotifyUser(string.Format(T["PaymentBuy.MinAmountWarning"], FormatBtcWithUsdText(MinBtcAmount)), "warning");
            return;
        }

        try
        {
            isLoading = true;
            IBitcoinPaymentService? service = PaymentFactory.GetService(SelectedMethod);
            string? privateKey = null;

            if (SelectedMethod == "Testnet")
            {
                var (address, paymentId, privKey) = await ((TestnetBitcoinPaymentService)service)
                    .GenerateAddressWithKeyAsync(Amount, orderId: $"order-{ProductId}-{Guid.NewGuid()}");
                Address = address;
                PaymentId = paymentId;
                privateKey = privKey;
            }
            else
            {
                var (address, paymentId) = await service.GenerateAddressAsync(Amount, $"order-{ProductId}-{Guid.NewGuid()}");
                Address = address;
                PaymentId = paymentId;
            }
            IsPaid = false;

            AuthenticationState? authState = await AuthProvider.GetAuthenticationStateAsync();
            string? userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            paymentRecord = new PaymentRecord
            {
                ProductId = ProductId,
                UserId = userId,
                SellerId = sellerUser?.Id,
                Currency = offerCurrency,
                Address = Address,
                PaymentId = PaymentId,
                Amount = Amount,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow,
                PaymentMethod = SelectedMethod,
                PrivateKey = privateKey,
            };

            await using var db = await DbFactory.CreateDbContextAsync();
            db.Payments.Add(paymentRecord);
            await db.SaveChangesAsync();

            await LogService.LogAsync(
                $"Endereço/invoice gerado para produto {ProductId} via {SelectedMethod}.",
                source: "Payment",
                level: "Info",
                userId: userId
            );

            NotifyUser(T["PaymentBuy.Generated"], "success");
        }
        catch (Exception ex)
        {
            await LogService.LogAsync(
                "Erro ao gerar endereço/invoice.",
                source: "Payment",
                level: "Error",
                userId: sellerUser?.Id,
                ex: ex
            );
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
            var normalizedAddress = Address.Trim();
            await using var db = await DbFactory.CreateDbContextAsync();
            PaymentRecord? payment = await db.Payments.FirstOrDefaultAsync(p => p.Address == normalizedAddress);
            if (payment == null)
            {
                NotifyUser(T["PaymentBuy.NotFoundByAddress"], "error");
                return;
            }

            (bool Confirmed, bool AlreadyPaid, decimal ReceivedAmount) result = await PaymentConfirmationService.ConfirmAsync(payment);

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
            await LogService.LogAsync(
                "Erro ao verificar pagamento.",
                source: "Payment",
                level: "Error",
                ex: ex
            );
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

            if (AbacatePayOpts.Value.IsEnabled)
            {
                // External Pix gateway: AbacatePay Transparent Checkout
                var orderId = $"order-{ProductId}-{Guid.NewGuid():N}";
                var pixService = PaymentFactory.GetService("Pix");
                var (address, paymentId) = await pixService.GenerateAddressAsync(Amount, orderId);
                Address = address;
                PaymentId = paymentId;
            }
            else
            {
                // Fallback: static EMVCo Pix payload (requires manual seller confirmation)
                var recipientPixKey = await ResolvePixRecipientKeyAsync();
                if (string.IsNullOrWhiteSpace(recipientPixKey))
                    return;

                var merchantName = PixPayloadBuilder.SanitizePixText(product?.User?.FullName ?? product?.User?.UserName ?? "OTSERV MARKET", 25);
                var merchantCity = PixPayloadBuilder.SanitizePixText("SAO PAULO", 15);
                var txId = PixPayloadBuilder.BuildPixTxId(ProductId);
                Address = PixPayloadBuilder.BuildPixPayload(recipientPixKey, Amount, merchantName, merchantCity, txId);
                PaymentId = $"pix-{Guid.NewGuid():N}";
            }

            IsPaid = false;

            AuthenticationState? authState = await AuthProvider.GetAuthenticationStateAsync();
            string? userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            paymentRecord = new PaymentRecord
            {
                ProductId = ProductId,
                UserId = userId,
                SellerId = sellerUser?.Id,
                Currency = offerCurrency,
                Address = Address,
                PaymentId = PaymentId,
                Amount = Amount,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow,
                PaymentMethod = SelectedMethod,
                PrivateKey = null,
            };

            await using var pixDb = await DbFactory.CreateDbContextAsync();
            pixDb.Payments.Add(paymentRecord);
            await pixDb.SaveChangesAsync();

            await LogService.LogAsync(
                $"QR PIX gerado para produto {ProductId} via {(AbacatePayOpts.Value.IsEnabled ? "AbacatePay" : "payload estatico")}.",
                source: "Payment",
                level: "Info",
                userId: userId
            );

            NotifyUser("Pagamento PIX gerado. Escaneie o QR code ou copie o codigo PIX.", "success");
        }
        catch (Exception ex)
        {
            await LogService.LogAsync(
                "Erro ao gerar pagamento PIX.",
                source: "Payment",
                level: "Error",
                userId: sellerUser?.Id,
                ex: ex
            );

            NotifyUser($"Erro ao gerar pagamento PIX: {ex.Message}", "error");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task<string?> ResolvePixRecipientKeyAsync()
    {
        if (UseSiteIntermediary)
        {
            var sitePixKey = await AdminSettingsService.GetSiteIntermediaryPixKeyAsync();
            if (string.IsNullOrWhiteSpace(sitePixKey))
            {
                NotifyUser("O intermedio do site esta ativo, mas o admin ainda nao cadastrou a chave PIX no painel admin.", "warning");
                return null;
            }

            return sitePixKey.Trim();
        }

        var sellerUserId = sellerUser?.Id;
        if (string.IsNullOrWhiteSpace(sellerUserId))
        {
            NotifyUser("Nao foi possivel identificar o vendedor para gerar o PIX.", "error");
            return null;
        }

        await using var db = await DbFactory.CreateDbContextAsync();
        var sellerPixKey = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == sellerUserId)
            .Select(u => u.PixKey)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(sellerPixKey))
        {
            pixSellerKeyNotice = "O vendedor nao possui chave PIX cadastrada no perfil. Escolha intermedio do ADM do site ou use outra forma de pagamento.";
            return null;
        }

        return sellerPixKey.Trim();
    }

    private Task<bool> ValidateOfferBeforePaymentAsync() => Task.FromResult(true);
}
