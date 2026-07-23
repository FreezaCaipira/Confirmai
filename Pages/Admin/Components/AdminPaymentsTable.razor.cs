using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Admin.Components;

public partial class AdminPaymentsTable
{
    [Parameter] public int TotalItems { get; set; }
    [Parameter] public List<PaymentRecord> Payments { get; set; } = new();
    [Parameter] public int CurrentPage { get; set; }
    [Parameter] public int TotalPages { get; set; }
    [Parameter] public Func<decimal, MarkupString>? FormatAmount { get; set; }
    [Parameter] public EventCallback<int> OnViewPayment { get; set; }
    [Parameter] public EventCallback OnPrevPage { get; set; }
    [Parameter] public EventCallback OnNextPage { get; set; }

    private async Task PrevPage() => await OnPrevPage.InvokeAsync();
    private async Task NextPage() => await OnNextPage.InvokeAsync();
}
