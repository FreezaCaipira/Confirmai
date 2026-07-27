using Confirmai.Enums;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Services.Admin;

public sealed class AdminLogsFilterOrchestrator
{
    public string GlobalSearch { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";
    public string Level { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string EventType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public AdminLogsQuickRangePreset QuickRangePreset { get; set; } = AdminLogsQuickRangePreset.None;
    public AdminLogsAuditQuickFilter AuditQuickFilter { get; set; } = AdminLogsAuditQuickFilter.All;
    public AdminLogSortColumn SortColumn { get; set; } = AdminLogSortColumn.Timestamp;
    public bool SortAscending { get; set; }
    public bool ShowRestoredNotice { get; set; }

    public bool HasInvalidDateRange =>
        StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date;

    public bool HasDateRangeFilter => StartDate.HasValue || EndDate.HasValue;

    private readonly AdminLogsFilterStateService _stateService;
    private readonly NavigationManager _navigationManager;

    public AdminLogsFilterOrchestrator(
        AdminLogsFilterStateService stateService,
        NavigationManager navigationManager)
    {
        _stateService = stateService;
        _navigationManager = navigationManager;
    }

    public AdminLogFilterCriteria BuildPrimaryFilterCriteria() => new()
    {
        GlobalTerm = GlobalSearch,
        UserId = UserId,
        Source = Source,
        Message = Message,
        Level = Level,
        StartDate = StartDate,
        EndDate = EndDate,
        EventType = EventType,
        EntityType = EntityType
    };

    public AdminLogFilterCriteria BuildAuditCountsFilterCriteria() => new()
    {
        GlobalTerm = GlobalSearch,
        UserId = UserId,
        Message = Message,
        StartDate = StartDate,
        EndDate = EndDate
    };

    public async Task LoadFromStorageAsync()
    {
        var storedState = await _stateService.LoadAsync();
        var uri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
        var queryOverrides = AdminLogsQueryOverridesParser.Parse(uri);
        var effectiveState = AdminLogsFilterStateMerger.ApplyQueryOverrides(storedState, queryOverrides);

        GlobalSearch = effectiveState.GlobalSearch;
        UserId = effectiveState.UserId;
        Source = effectiveState.Source;
        Message = effectiveState.Message;
        Level = effectiveState.Level;
        StartDate = effectiveState.StartDate;
        EndDate = effectiveState.EndDate;
        EventType = effectiveState.EventType;
        EntityType = effectiveState.EntityType;

        QuickRangePreset = effectiveState.QuickRangePreset ?? InferQuickRangePreset();
        AuditQuickFilter = effectiveState.AuditQuickFilter ?? InferAuditQuickFilter();

        if (effectiveState.SortColumn.HasValue)
            SortColumn = effectiveState.SortColumn.Value;

        if (effectiveState.SortAscending.HasValue)
            SortAscending = effectiveState.SortAscending.Value;

        if (queryOverrides.HasAny)
        {
            QuickRangePreset = InferQuickRangePreset();
            AuditQuickFilter = InferAuditQuickFilter();
            ShowRestoredNotice = false;
            return;
        }

        ShowRestoredNotice = AdminLogsFilterStateRules.ShouldShowRestoredNotice(storedState);
    }

    public void ApplyQueryOverridesToCurrentFilters()
    {
        var uri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
        var queryOverrides = AdminLogsQueryOverridesParser.Parse(uri);

        if (!queryOverrides.HasAny)
            return;

        var currentState = BuildCurrentState();
        var effectiveState = AdminLogsFilterStateMerger.ApplyQueryOverrides(currentState, queryOverrides);

        GlobalSearch = effectiveState.GlobalSearch;
        UserId = effectiveState.UserId;
        Source = effectiveState.Source;
        Message = effectiveState.Message;
        Level = effectiveState.Level;
        StartDate = effectiveState.StartDate;
        EndDate = effectiveState.EndDate;
        EventType = effectiveState.EventType;
        EntityType = effectiveState.EntityType;
        QuickRangePreset = InferQuickRangePreset();
        AuditQuickFilter = InferAuditQuickFilter();
    }

    public async Task PersistAsync()
    {
        await _stateService.SaveAsync(BuildCurrentState());
    }

    public void ClearAll()
    {
        GlobalSearch = "";
        UserId = "";
        Source = "";
        Message = "";
        Level = "";
        EventType = "";
        EntityType = "";
        AuditQuickFilter = AdminLogsAuditQuickFilter.All;
        QuickRangePreset = AdminLogsQuickRangePreset.None;
        StartDate = null;
        EndDate = null;
        ShowRestoredNotice = false;
    }

    public void SetTodayRange()
    {
        var today = DateTime.Today;
        QuickRangePreset = AdminLogsQuickRangePreset.Today;
        StartDate = today;
        EndDate = today;
    }

    public void SetLastDaysRange(int days)
    {
        if (days < 1) return;
        QuickRangePreset = days == 7 ? AdminLogsQuickRangePreset.Last7Days : AdminLogsQuickRangePreset.Last30Days;
        EndDate = DateTime.Today;
        StartDate = EndDate.Value.AddDays(-(days - 1));
    }

    public void SetCurrentMonthRange()
    {
        var today = DateTime.Today;
        QuickRangePreset = AdminLogsQuickRangePreset.CurrentMonth;
        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
    }

    public void ClearDateRange()
    {
        QuickRangePreset = AdminLogsQuickRangePreset.None;
        StartDate = null;
        EndDate = null;
    }

    public void SetAuditQuickFilter(AdminLogsAuditQuickFilter filter)
    {
        AuditQuickFilter = filter;

        switch (filter)
        {
            case AdminLogsAuditQuickFilter.SecurityPolicy:
                Source = AdminAuditSources.SecurityPolicy;
                Level = "";
                EventType = "";
                break;
            case AdminLogsAuditQuickFilter.PaymentPanelStale:
                Source = "";
                Level = "";
                EventType = AuditEvents.PaymentReconciliationPanelStale;
                break;
            default:
                Source = "";
                Level = "";
                EventType = "";
                break;
        }
    }

    public void SyncQuickRangePreset() => QuickRangePreset = InferQuickRangePreset();
    public void SyncAuditQuickFilter() => AuditQuickFilter = InferAuditQuickFilter();

    public void ToggleSort(AdminLogSortColumn column)
    {
        if (SortColumn == column)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
    }

    public string SortIndicator(AdminLogSortColumn column)
    {
        if (SortColumn != column) return "⇅";
        return SortAscending ? "↑" : "↓";
    }

    public string QuickRangeButtonClass(AdminLogsQuickRangePreset preset)
        => QuickRangePreset == preset ? "quick-range-btn active" : "quick-range-btn";

    public string AuditQuickFilterButtonClass(AdminLogsAuditQuickFilter filter)
        => AuditQuickFilter == filter ? "quick-range-btn active" : "quick-range-btn";

    private AdminLogsFilterState BuildCurrentState() => new()
    {
        GlobalSearch = GlobalSearch,
        UserId = UserId,
        Source = Source,
        Message = Message,
        Level = Level,
        StartDate = StartDate,
        EndDate = EndDate,
        EventType = EventType,
        EntityType = EntityType,
        QuickRangePreset = QuickRangePreset,
        AuditQuickFilter = AuditQuickFilter,
        SortColumn = SortColumn,
        SortAscending = SortAscending
    };

    private AdminLogsQuickRangePreset InferQuickRangePreset()
        => AdminLogsFilterInference.InferQuickRangePreset(StartDate, EndDate, DateTime.Today);

    private AdminLogsAuditQuickFilter InferAuditQuickFilter()
        => AdminLogsFilterInference.InferAuditQuickFilter(Source, Level, EventType);
}
