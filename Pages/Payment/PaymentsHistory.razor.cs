using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Core;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Confirmai.Shared.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Payment;

public partial class PaymentsHistory : IAsyncDisposable
{
    private List<PaymentRecord>? payments;
    private List<PaymentRecord>? receivedPayments;
    private List<PaymentRecord>? filteredPayments;
    private Dictionary<int, string> productNamesById = new();
    private string activeTab = "sent";
    private decimal? filterMinAmount;
    private decimal? filterMaxAmount;
    private string filterStatus = "";
    private string filterGateway = "";
    private DateTime? filterDate;
    [CascadingParameter] public Toast? ToastRef { get; set; }
    private string? currentUserId;
    private decimal? btcUsdRate;
    private decimal? btcBrlRate;

    private int pageSize = 10;
    private int currentPage = 1;
    private int totalPages => filteredPayments == null ? 1 : Math.Max(1, (int)Math.Ceiling((double)filteredPayments.Count / pageSize));

    private bool CanGoToNextPage => currentPage < totalPages;
    private bool CanGoToPreviousPage => currentPage > 1;

    protected override async Task OnInitializedAsync()
    {
                var quote = await BitcoinQuoteService.GetQuoteAsync();
                btcUsdRate = quote?.btc_usd;
                btcBrlRate = quote?.btc_brl;

        var authState = await AuthProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst("sub")?.Value
          ?? authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await using var db = await DbFactory.CreateDbContextAsync();

        payments = await db.Payments
                        .Include(p => p.Product)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        receivedPayments = await db.Payments
            .Include(p => p.Product)
            .Include(p => p.User)
            .Where(p => p.SellerId == userId
                || (p.SellerId == null && p.Product != null && p.Product.UserId == userId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var productIds = payments
            .Select(p => p.ProductId)
            .Distinct()
            .ToList();

        productNamesById = await db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name ?? string.Empty);

        ApplyFilters();

        currentUserId = userId;
        PaymentEventBus.OnPaymentConfirmed += OnPaymentConfirmed;
    }

    private void OnPaymentConfirmed(string userId, string paymentId)
    {
        if (userId != currentUserId) return;

        var payment = payments?.FirstOrDefault(p => p.PaymentId == paymentId);
        if (payment != null && !payment.IsPaid)
        {
            payment.IsPaid = true;
            payment.PaidAt = DateTime.UtcNow;
            InvokeAsync(() =>
            {
                ToastRef?.Show(string.Format(T["PaymentHistory.ConfirmedToast"], paymentId), "success");
            });
        }
    }

    public ValueTask DisposeAsync()
    {
        PaymentEventBus.OnPaymentConfirmed -= OnPaymentConfirmed;
        return ValueTask.CompletedTask;
    }

    private void SwitchTab(string tab)
    {
        activeTab = tab;
        ClearFilters();
    }

    private void ApplyFilters()
    {
        currentPage = 1;
        var source = activeTab == "received" ? receivedPayments : payments;
        filteredPayments = source?
            .Where(p =>
                (!filterMinAmount.HasValue || p.Amount >= filterMinAmount.Value) &&
                (!filterMaxAmount.HasValue || p.Amount <= filterMaxAmount.Value) &&
                (string.IsNullOrWhiteSpace(filterStatus) ||
                    (filterStatus == "paid" && p.IsPaid) ||
                    (filterStatus == "pending" && !p.IsPaid)) &&
                (string.IsNullOrWhiteSpace(filterGateway) || (p.PaymentMethod?.Contains(filterGateway, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (!filterDate.HasValue || p.CreatedAt.Date == filterDate.Value.Date)
            )
            .ToList();
    }

    private void ClearFilters()
    {
        filterMinAmount = null;
        filterMaxAmount = null;
        filterStatus = "";
        filterGateway = "";
        filterDate = null;
        ApplyFilters();
    }

    private async Task CancelPayment(int paymentId)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var payment = await db.Payments.FindAsync(paymentId);
        if (payment != null && !payment.IsPaid)
        {
            db.Payments.Remove(payment);
            await db.SaveChangesAsync();
            payments?.Remove(payment);

            await LogService.LogAsync(
                $"Pagamento cancelado (ID: {paymentId}) para produto {payment.ProductId}.",
                source: "Payment",
                level: "Warning",
                userId: payment.UserId
            );

            ApplyFilters();
            ToastRef?.Show(T["PaymentHistory.CanceledToast"], "info");
        }
    }

    private async Task ConfirmCancelPayment(int paymentId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", T["PaymentHistory.ConfirmCancel"])) return;
        await CancelPayment(paymentId);
    }

    private void NextPage()
    {
        if (CanGoToNextPage)
            currentPage++;
    }

    private void PrevPage()
    {
        if (CanGoToPreviousPage)
            currentPage--;
    }

    private MarkupString FormatBtcWithUsd(decimal amount)
    {
        return BtcUsdFormatter.FormatMarkup(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
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

    private string GetProductDisplayName(PaymentRecord payment)
    {
        if (payment.Product is { Name: { Length: > 0 } productName })
        {
            return productName;
        }

        if (productNamesById.TryGetValue(payment.ProductId, out var mappedName)
            && !string.IsNullOrWhiteSpace(mappedName))
        {
            return mappedName;
        }

        return T["PaymentHistory.UnknownProduct"];
    }

    private async Task ExportToCsv()
    {
        if (filteredPayments == null || !filteredPayments.Any()) return;

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Data,Produto,Valor,Status,Gateway");

        foreach (var p in filteredPayments)
        {
            var status = p.IsPaid ? T["Common.Paid"] : T["Common.Pending"];
            var product = GetProductDisplayName(p);
            var amount = FormatPaymentAmount(p.Amount, p.Currency);
            var date = p.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var gateway = p.PaymentMethod ?? "-";

            csv.AppendLine($"\"{date}\",\"{product}\",\"{amount}\",\"{status}\",\"{gateway}\"");
        }

        var fileName = $"pagamentos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        await JS.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, csv.ToString(), "text/csv");
    }

    private async Task ExportToPdf()
    {
        if (filteredPayments == null || !filteredPayments.Any()) return;

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; }}
        h1 {{ color: #333; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f4f4f4; }}
        .paid {{ color: green; font-weight: bold; }}
        .pending {{ color: orange; font-weight: bold; }}
    </style>
</head>
<body>
    <h1>Histórico de Pagamentos</h1>
    <p>Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
    <table>
        <thead>
            <tr>
                <th>Data</th>
                <th>Produto</th>
                <th>Valor</th>
                <th>Status</th>
                <th>Gateway</th>
            </tr>
        </thead>
        <tbody>";

        foreach (var p in filteredPayments)
        {
            var status = p.IsPaid ? T["Common.Paid"] : T["Common.Pending"];
            var statusClass = p.IsPaid ? "paid" : "pending";
            var product = GetProductDisplayName(p);
            var amount = FormatPaymentAmount(p.Amount, p.Currency);
            var date = p.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var gateway = p.PaymentMethod ?? "-";

            html += $@"
            <tr>
                <td>{date}</td>
                <td>{product}</td>
                <td>{amount}</td>
                <td class='{statusClass}'>{status}</td>
                <td>{gateway}</td>
            </tr>";
        }

        html += @"
        </tbody>
    </table>
</body>
</html>";

        var fileName = $"pagamentos_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        await JS.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, html, "text/html");
    }
}
