using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class AdminLogs : IDisposable
{
    private List<AppLog> currentPageLogs = new();
    private int totalLogs;
    private int currentPage = 1;
    private int totalPages = 1;
    private int auditCountAll;
    private int auditCountSecurityPolicy;
    private int auditCountPaymentPanelStale;
    private const int PageSize = 20;
    private const int ExportMaxRows = 10000;
    private bool filtersLoaded;
    private string? exportNotice;
    private readonly SemaphoreSlim loadLogsLock = new(1, 1);
    private bool isDisposed;
    private readonly DebounceDispatcher globalSearchDebouncer = new();

    private bool HasInvalidDateRange => Filters.HasInvalidDateRange;
    private bool HasDateRangeFilter => Filters.HasDateRangeFilter;
    private string ActivePeriodSummary => BuildActivePeriodSummary();

    protected override async Task OnInitializedAsync()
    {
        Filters.ApplyQueryOverridesToCurrentFilters();
        await LoadCountsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || filtersLoaded)
            return;

        await Filters.LoadFromStorageAsync();
        currentPage = 1;
        await LoadCountsAsync();
        filtersLoaded = true;
    }

    private async Task LoadCountsAsync()
    {
        await loadLogsLock.WaitAsync();
        try
        {
            if (isDisposed) return;

            var data = await AdminLogsQueryService.GetPageDataAsync(
                primaryCriteria: Filters.BuildPrimaryFilterCriteria(),
                auditCountsCriteria: Filters.BuildAuditCountsFilterCriteria(),
                sortColumn: Filters.SortColumn,
                sortAscending: Filters.SortAscending,
                requestedPage: currentPage,
                pageSize: PageSize);

            if (isDisposed) return;

            totalLogs = data.TotalLogs;
            totalPages = Math.Max(1, (int)Math.Ceiling((double)totalLogs / PageSize));
            if (currentPage > totalPages)
                currentPage = totalPages;
            currentPageLogs = data.Logs;
            auditCountAll = data.AuditCounts.All;
            auditCountSecurityPolicy = data.AuditCounts.SecurityPolicy;
            auditCountPaymentPanelStale = data.AuditCounts.PaymentPanelStale;
        }
        finally
        {
            if (!isDisposed)
                loadLogsLock.Release();
        }
    }

    private async Task GoToPrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            await LoadCountsAsync();
        }
    }

    private async Task GoToNextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            await LoadCountsAsync();
        }
    }

    private async Task ApplyFilters()
    {
        if (HasInvalidDateRange) return;
        Filters.SyncQuickRangePreset();
        Filters.SyncAuditQuickFilter();
        Filters.ShowRestoredNotice = false;
        currentPage = 1;
        await Filters.PersistAsync();
        await LoadCountsAsync();
    }

    private async Task OnGlobalSearchInput(ChangeEventArgs args)
    {
        Filters.GlobalSearch = args.Value?.ToString() ?? string.Empty;
        await globalSearchDebouncer.DebounceAsync(TimeSpan.FromMilliseconds(350), ApplyFilters);
    }

    private async Task HandleGlobalSearchInput()
    {
        await globalSearchDebouncer.DebounceAsync(TimeSpan.FromMilliseconds(350), ApplyFilters);
    }

    private async Task SetTodayRangeAsync()
    {
        Filters.SetTodayRange();
        await ApplyFiltersCore(preserveQuickRange: true, preserveAudit: false);
    }

    private async Task SetLastDaysRangeAsync(int days)
    {
        Filters.SetLastDaysRange(days);
        await ApplyFiltersCore(preserveQuickRange: true, preserveAudit: false);
    }

    private async Task SetCurrentMonthRangeAsync()
    {
        Filters.SetCurrentMonthRange();
        await ApplyFiltersCore(preserveQuickRange: true, preserveAudit: false);
    }

    private async Task ClearDateRangeAsync()
    {
        Filters.ClearDateRange();
        await ApplyFiltersCore(preserveQuickRange: true, preserveAudit: false);
    }

    private async Task SetAuditQuickFilterAsync(AdminLogsAuditQuickFilter filter)
    {
        Filters.SetAuditQuickFilter(filter);
        await ApplyFiltersCore(preserveQuickRange: false, preserveAudit: true);
    }

    private async Task ApplyFiltersCore(bool preserveQuickRange, bool preserveAudit)
    {
        if (HasInvalidDateRange) return;
        if (!preserveQuickRange) Filters.SyncQuickRangePreset();
        if (!preserveAudit) Filters.SyncAuditQuickFilter();
        Filters.ShowRestoredNotice = false;
        currentPage = 1;
        await Filters.PersistAsync();
        await LoadCountsAsync();
    }

    private async Task HandleSortAsync(string columnString)
    {
        if (Enum.TryParse<AdminLogSortColumn>(columnString, out var column))
        {
            Filters.ToggleSort(column);
            await Filters.PersistAsync();
            await LoadCountsAsync();
        }
    }

    private string SortIndicator(AdminLogSortColumn column) => Filters.SortIndicator(column);
    private string QuickRangeButtonClass(AdminLogsQuickRangePreset preset) => Filters.QuickRangeButtonClass(preset);
    private string AuditQuickFilterButtonClass(AdminLogsAuditQuickFilter filter) => Filters.AuditQuickFilterButtonClass(filter);

    private async Task ClearFilters()
    {
        Filters.ClearAll();
        currentPage = 1;
        await Filters.PersistAsync();
        await ApplyFiltersCore(preserveQuickRange: true, preserveAudit: true);
    }

    private async Task ExportCsvAsync()
    {
        if (HasInvalidDateRange) return;
        exportNotice = await ExportService.ExportCsvAsync(
            Filters.BuildPrimaryFilterCriteria(), ExportMaxRows,
            Filters.Level, Filters.Source, Filters.StartDate, Filters.EndDate,
            T["AdminLogs.ExportTruncated"]);
    }

    private async Task ExportJsonAsync()
    {
        if (HasInvalidDateRange) return;
        exportNotice = await ExportService.ExportJsonAsync(
            Filters.BuildPrimaryFilterCriteria(), ExportMaxRows,
            Filters.Level, Filters.Source, Filters.StartDate, Filters.EndDate,
            T["AdminLogs.ExportTruncated"]);
    }

    private void DismissRestoredNotice() => Filters.ShowRestoredNotice = false;
    private void DismissExportNotice() => exportNotice = null;

    private string BuildActivePeriodSummary()
    {
        if (Filters.StartDate.HasValue && Filters.EndDate.HasValue)
            return string.Format(T["AdminLogs.PeriodRange"], Filters.StartDate.Value.ToString("dd/MM/yyyy"), Filters.EndDate.Value.ToString("dd/MM/yyyy"));
        if (Filters.StartDate.HasValue)
            return string.Format(T["AdminLogs.PeriodFrom"], Filters.StartDate.Value.ToString("dd/MM/yyyy"));
        if (Filters.EndDate.HasValue)
            return string.Format(T["AdminLogs.PeriodUntil"], Filters.EndDate.Value.ToString("dd/MM/yyyy"));
        return T["AdminLogs.PeriodNone"];
    }

    private static string SourceBadgeClass(string source) => "log-source-badge";

    public void Dispose()
    {
        isDisposed = true;
        globalSearchDebouncer.Dispose();
        loadLogsLock.Dispose();
    }
}
