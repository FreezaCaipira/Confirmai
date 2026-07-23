using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Admin.Components;

public partial class AdminPaymentsFilters
{
    private string filterUserId = "";
    private decimal? filterMinAmount;
    private decimal? filterMaxAmount;
    private string filterStatus = "";
    private DateTime? filterDate;

    [Parameter] public string FilterUserId { get; set; } = "";
    [Parameter] public decimal? FilterMinAmount { get; set; }
    [Parameter] public decimal? FilterMaxAmount { get; set; }
    [Parameter] public string FilterStatus { get; set; } = "";
    [Parameter] public DateTime? FilterDate { get; set; }
    [Parameter] public bool ShowRestoredNotice { get; set; }

    [Parameter] public EventCallback<(string UserId, decimal? MinAmount, decimal? MaxAmount, string Status, DateTime? Date)> OnApplyFilters { get; set; }
    [Parameter] public EventCallback OnClearFilters { get; set; }
    [Parameter] public EventCallback OnDismissNotice { get; set; }

    protected override void OnInitialized()
    {
        filterUserId = FilterUserId;
        filterMinAmount = FilterMinAmount;
        filterMaxAmount = FilterMaxAmount;
        filterStatus = FilterStatus;
        filterDate = FilterDate;
    }

    private async Task ApplyFilters()
    {
        await OnApplyFilters.InvokeAsync((filterUserId, filterMinAmount, filterMaxAmount, filterStatus, filterDate));
    }

    private async Task ClearFilters()
    {
        filterUserId = "";
        filterMinAmount = null;
        filterMaxAmount = null;
        filterStatus = "";
        filterDate = null;
        await OnClearFilters.InvokeAsync();
    }

    private async Task DismissNotice()
    {
        await OnDismissNotice.InvokeAsync();
    }
}
