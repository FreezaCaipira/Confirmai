using System.Globalization;
using Confirmai.Data;
using Confirmai.Services.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Admin;

public partial class AdminRevenue
{
    private DateTime? startDate;
    private DateTime? endDate;
    private bool isLoading = false;
    private RevenueSummary? summary;
    private List<GroupRevenueBreakdown> groupBreakdown = new();
    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AdminRevenueReportService RevenueReportService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Default to last 30 days
        endDate = DateTime.UtcNow;
        startDate = DateTime.UtcNow.AddDays(-30);
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        isLoading = true;
        try
        {
            summary = await RevenueReportService.GetRevenueSummaryAsync(startDate, endDate);
            groupBreakdown = await RevenueReportService.GetRevenueByGroupAsync(startDate, endDate);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ClearFilters()
    {
        startDate = null;
        endDate = null;
    }
}
