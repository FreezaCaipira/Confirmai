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

public class AdminLogsFilterStateService
{
    private readonly IJSRuntime _js;

    public AdminLogsFilterStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<AdminLogsFilterState> LoadAsync()
    {
        try
        {
            var globalSearch = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.GlobalSearch);
            var userId = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.UserFilter);
            var source = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.SourceFilter);
            var message = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.MessageFilter);
            var level = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.LevelFilter);
            var startDate = await LocalStorageStateHelpers.GetDateAsync(_js, AdminLogsStorageKeys.StartDateFilter);
            var endDate = await LocalStorageStateHelpers.GetDateAsync(_js, AdminLogsStorageKeys.EndDateFilter);
            var eventType = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.EventTypeFilter);
            var entityType = await LocalStorageStateHelpers.GetStringAsync(_js, AdminLogsStorageKeys.EntityTypeFilter);
            var page = await LocalStorageStateHelpers.GetPositiveIntAsync(_js, AdminLogsStorageKeys.Page);
            var quickRangePreset = await LocalStorageStateHelpers.GetEnumAsync<AdminLogsQuickRangePreset>(_js, AdminLogsStorageKeys.QuickRangePreset);
            var auditQuickFilter = await LocalStorageStateHelpers.GetEnumAsync<AdminLogsAuditQuickFilter>(_js, AdminLogsStorageKeys.AuditQuickFilter);
            var sortColumn = await LocalStorageStateHelpers.GetEnumAsync<AdminLogSortColumn>(_js, AdminLogsStorageKeys.SortColumn);
            var sortAscending = await LocalStorageStateHelpers.GetNullableBoolAsync(_js, AdminLogsStorageKeys.SortAscending);

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
        catch
        {
            return new AdminLogsFilterState();
        }
    }

    public async Task SaveAsync(AdminLogsFilterState state)
    {
        try
        {
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.GlobalSearch, state.GlobalSearch);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.UserFilter, state.UserId);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.SourceFilter, state.Source);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.MessageFilter, state.Message);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.LevelFilter, state.Level);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.EventTypeFilter, state.EventType);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.EntityTypeFilter, state.EntityType);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.QuickRangePreset, state.QuickRangePreset?.ToString() ?? string.Empty);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.AuditQuickFilter, state.AuditQuickFilter?.ToString() ?? string.Empty);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.SortColumn, state.SortColumn?.ToString() ?? string.Empty);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminLogsStorageKeys.SortAscending, state.SortAscending?.ToString() ?? string.Empty);
            await LocalStorageStateHelpers.SetOrRemoveDateAsync(_js, AdminLogsStorageKeys.StartDateFilter, state.StartDate);
            await LocalStorageStateHelpers.SetOrRemoveDateAsync(_js, AdminLogsStorageKeys.EndDateFilter, state.EndDate);
            await LocalStorageStateHelpers.SetPageAsync(_js, AdminLogsStorageKeys.Page, state.Page);
        }
        catch
        {
            // No-op: ignore storage failures.
        }
    }
}

