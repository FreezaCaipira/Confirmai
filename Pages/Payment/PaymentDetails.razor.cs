using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Crypto;
using Confirmai.Services.User;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Payment;

public partial class PaymentDetails
{
    [Parameter] public int PaymentId { get; set; }
    public PaymentRecord? payment;
    private decimal? btcUsdRate;
    private decimal? btcBrlRate;

    private bool IsPixPayment => string.Equals(payment?.PaymentMethod, "Pix", StringComparison.OrdinalIgnoreCase);
    private string AddressLabel => IsPixPayment ? T["PaymentBuy.PixCodeLabel"] : T["Common.Address"];

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private BitcoinQuoteService BitcoinQuoteService { get; set; } = default!;
    [Inject] private CurrencyPreferenceService CurrencyPreferenceService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var quote = await BitcoinQuoteService.GetQuoteAsync();
        btcUsdRate = quote?.btc_usd;
        btcBrlRate = quote?.btc_brl;

        await using var db = await DbFactory.CreateDbContextAsync();
        payment = await db.Payments
            .Include(p => p.Product)
            .ThenInclude(product => product!.User)
            .FirstOrDefaultAsync(p => p.Id == PaymentId);
    }

    void GoBack() => Navigation.NavigateTo("/payments");

    private MarkupString FormatBtcWithUsd(decimal amount)
    {
        return BtcUsdFormatter.FormatMarkup(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
    }
}
