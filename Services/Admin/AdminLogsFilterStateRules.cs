namespace Confirmai.Services.Admin;

public static class AdminLogsFilterStateRules
{
    public static bool ShouldShowRestoredNotice(AdminLogsFilterState state)
    {
        return !string.IsNullOrWhiteSpace(state.GlobalSearch)
            || !string.IsNullOrWhiteSpace(state.UserId)
            || !string.IsNullOrWhiteSpace(state.Source)
            || !string.IsNullOrWhiteSpace(state.Message)
            || !string.IsNullOrWhiteSpace(state.Level)
            || !string.IsNullOrWhiteSpace(state.EventType)
            || !string.IsNullOrWhiteSpace(state.EntityType)
            || state.StartDate.HasValue
            || state.EndDate.HasValue
            || (state.Page ?? 1) > 1;
    }
}

