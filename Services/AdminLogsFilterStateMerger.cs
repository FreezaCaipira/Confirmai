namespace Confirmai.Services;

public static class AdminLogsFilterStateMerger
{
    public static AdminLogsFilterState ApplyQueryOverrides(AdminLogsFilterState state, AdminLogsQueryOverrides overrides)
    {
        if (!overrides.HasAny)
        {
            return state;
        }

        return new AdminLogsFilterState
        {
            GlobalSearch = state.GlobalSearch,
            UserId = state.UserId,
            Source = overrides.Source ?? state.Source,
            Message = state.Message,
            Level = overrides.Level ?? state.Level,
            StartDate = overrides.StartDate ?? state.StartDate,
            EndDate = overrides.EndDate ?? state.EndDate,
            EventType = overrides.EventType ?? state.EventType,
            EntityType = overrides.EntityType ?? state.EntityType,
            Page = 1,
            QuickRangePreset = state.QuickRangePreset,
            AuditQuickFilter = state.AuditQuickFilter,
            SortColumn = state.SortColumn,
            SortAscending = state.SortAscending
        };
    }
}