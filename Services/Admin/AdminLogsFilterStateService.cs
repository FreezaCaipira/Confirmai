using Confirmai.Services.Utility;
using Microsoft.JSInterop;

namespace Confirmai.Services.Admin;

public sealed class AdminLogsFilterState
{
    public string GlobalSearch { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public int? Page { get; init; }
    public AdminLogsQuickRangePreset? QuickRangePreset { get; init; }
    public AdminLogsAuditQuickFilter? AuditQuickFilter { get; init; }
    public AdminLogSortColumn? SortColumn { get; init; }
    public bool? SortAscending { get; init; }
}

public class AdminLogsFilterStateService : AdminFilterStateServiceBase<AdminLogsFilterState>
{
    public AdminLogsFilterStateService(IJSRuntime js) : base(js) { }

    protected override async Task<AdminLogsFilterState> LoadCoreAsync()
    {
        var globalSearch = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.GlobalSearch);
        var userId = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.UserFilter);
        var source = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.SourceFilter);
        var message = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.MessageFilter);
        var level = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.LevelFilter);
        var startDate = await LocalStorageStateHelpers.GetDateAsync(Js, AdminLogsStorageKeys.StartDateFilter);
        var endDate = await LocalStorageStateHelpers.GetDateAsync(Js, AdminLogsStorageKeys.EndDateFilter);
        var eventType = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.EventTypeFilter);
        var entityType = await LocalStorageStateHelpers.GetStringAsync(Js, AdminLogsStorageKeys.EntityTypeFilter);
        var page = await LocalStorageStateHelpers.GetPositiveIntAsync(Js, AdminLogsStorageKeys.Page);
        var quickRangePreset = await LocalStorageStateHelpers.GetEnumAsync<AdminLogsQuickRangePreset>(Js, AdminLogsStorageKeys.QuickRangePreset);
        var auditQuickFilter = await LocalStorageStateHelpers.GetEnumAsync<AdminLogsAuditQuickFilter>(Js, AdminLogsStorageKeys.AuditQuickFilter);
        var sortColumn = await LocalStorageStateHelpers.GetEnumAsync<AdminLogSortColumn>(Js, AdminLogsStorageKeys.SortColumn);
        var sortAscending = await LocalStorageStateHelpers.GetNullableBoolAsync(Js, AdminLogsStorageKeys.SortAscending);

        return new AdminLogsFilterState
        {
            GlobalSearch = globalSearch,
            UserId = userId,
            Source = source,
            Message = message,
            Level = level,
            StartDate = startDate,
            EndDate = endDate,
            EventType = eventType,
            EntityType = entityType,
            Page = page,
            QuickRangePreset = quickRangePreset,
            AuditQuickFilter = auditQuickFilter,
            SortColumn = sortColumn,
            SortAscending = sortAscending
        };
    }

    protected override async Task SaveCoreAsync(AdminLogsFilterState state)
    {
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.GlobalSearch, state.GlobalSearch);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.UserFilter, state.UserId);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.SourceFilter, state.Source);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.MessageFilter, state.Message);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.LevelFilter, state.Level);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.EventTypeFilter, state.EventType);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.EntityTypeFilter, state.EntityType);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.QuickRangePreset, state.QuickRangePreset?.ToString() ?? string.Empty);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.AuditQuickFilter, state.AuditQuickFilter?.ToString() ?? string.Empty);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.SortColumn, state.SortColumn?.ToString() ?? string.Empty);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminLogsStorageKeys.SortAscending, state.SortAscending?.ToString() ?? string.Empty);
        await LocalStorageStateHelpers.SetOrRemoveDateAsync(Js, AdminLogsStorageKeys.StartDateFilter, state.StartDate);
        await LocalStorageStateHelpers.SetOrRemoveDateAsync(Js, AdminLogsStorageKeys.EndDateFilter, state.EndDate);
        await LocalStorageStateHelpers.SetPageAsync(Js, AdminLogsStorageKeys.Page, state.Page);
    }
}
