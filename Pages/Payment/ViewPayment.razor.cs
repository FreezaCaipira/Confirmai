using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Crypto;
using Confirmai.Services.Factories;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Payment;

public partial class ViewPayment : IAsyncDisposable
{
    [Parameter] public int PaymentId { get; set; }
    private PaymentRecord? payment;
    [CascadingParameter] public Toast? ToastRef { get; set; }
    private bool isLoading = false;
    public string SelectedMethod { get; set; } = "";
    private decimal? btcUsdRate;
    private decimal? btcBrlRate;
    private string? feedbackMessage;
    private string feedbackType = "info";
    private bool isCheckingPayment;
    private string currentUserId = "";

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private BitcoinPaymentFactory PaymentFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private PaymentConfirmationService PaymentConfirmationService { get; set; } = default!;
    [Inject] private BitcoinQuoteService BitcoinQuoteService { get; set; } = default!;
    [Inject] private CurrencyPreferenceService CurrencyPreferenceService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;
    [Inject] private PaymentEventBus PaymentEventBus { get; set; } = default!;

    private bool IsPixPayment => string.Equals(payment?.PaymentMethod, "Pix", StringComparison.OrdinalIgnoreCase);
    private string AddressLabel => IsPixPayment ? T["PaymentBuy.PixCodeLabel"] : T["PaymentBuy.InvoiceAddress"];

    public string QRCodeValue =>
        IsPixPayment
            ? payment?.Address ?? string.Empty
            : payment != null && payment.Address != null && payment.Address.StartsWith("lnbc")
                ? payment.Address
                : $"bitcoin:{payment?.Address}?amount={payment?.Amount}";

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        var quote = await BitcoinQuoteService.GetQuoteAsync();
        btcUsdRate = quote?.btc_usd;
        btcBrlRate = quote?.btc_brl;

        await using var db = await DbFactory.CreateDbContextAsync();
        payment = await db.Payments
            .Include(p => p.User)
            .Include(p => p.Seller)
            .Include(p => p.Product)
            .ThenInclude(prod => prod!.User)
            .FirstOrDefaultAsync(p => p.Id == PaymentId);
        isLoading = false;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        if (!string.IsNullOrEmpty(currentUserId) && payment != null)
        {
        }

        var methods = PaymentFactory.GetAvailableMethods().ToList();
        SelectedMethod = methods.Contains("Pix") ? "Pix" : methods.FirstOrDefault() ?? "";

        PaymentEventBus.OnPaymentConfirmed += OnPaymentConfirmed;
    }

    private void OnPaymentConfirmed(string userId, string paymentId)
    {
        if (paymentId != payment?.PaymentId) return;

        InvokeAsync(async () =>
        {
            await CheckPayment();
        });
    }

    public ValueTask DisposeAsync()
    {
        PaymentEventBus.OnPaymentConfirmed -= OnPaymentConfirmed;
        return ValueTask.CompletedTask;
    }

    private async Task OnCheckPaymentClick()
    {
        if (isCheckingPayment)
        {
            return;
        }

        await CheckPayment();
    }

    private async Task CheckPayment()
    {
        if (isCheckingPayment)
        {
            return;
        }

        isCheckingPayment = true;

        try
        {
            if (payment == null)
            {
                NotifyUser(T["PaymentView.NotFound"], "error");
                return;
            }

            try
            {
                var result = await PaymentConfirmationService.ConfirmAsync(payment);

                if (result.AlreadyPaid)
                {
                    payment.IsPaid = true;
                    NotifyUser(T["PaymentBuy.AlreadyConfirmed"], "success");
                    return;
                }

                if (result.Confirmed)
                {
                    payment.IsPaid = true;
                    payment.PaidAt = DateTime.UtcNow;
                    NotifyUser(T["PaymentBuy.Confirmed"], "success");
                }
                else
                {
                    NotifyUser(string.Format(T["PaymentBuy.NotReceived"], FormatBtcWithUsdText(result.ReceivedAmount), FormatBtcWithUsdText(payment.Amount)), "warning");
                }
            }
            catch
            {
                NotifyUser(T["PaymentBuy.CheckError"], "error");
            }
        }
        finally
        {
            isCheckingPayment = false;
        }
    }

    private void NotifyUser(string message, string type)
    {
        feedbackMessage = message;
        feedbackType = type;
        ToastRef?.Show(message, type);
    }

    private MarkupString FormatBtcWithUsdMarkup(decimal amount)
    {
        return BtcUsdFormatter.FormatMarkup(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
    }

    private string FormatBtcWithUsdText(decimal amount)
    {
        return BtcUsdFormatter.Format(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
    }

    private string FormatPaymentAmount(decimal amount, string? currency)
    {
        return currency switch
        {
            "BRL" => $"R$ {amount:N2}",
            "USD" => $"$ {amount:N2}",
            _ => BtcUsdFormatter.Format(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency)
        };
    }
}
