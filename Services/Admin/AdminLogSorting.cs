using Confirmai.Models;

namespace Confirmai.Services.Admin;

public enum AdminLogSortColumn
{
    Timestamp,
    Level,
    Source,
    User
}

public static class AdminLogSorting
{
    public static IQueryable<AppLog> Apply(IQueryable<AppLog> source, AdminLogSortColumn sortColumn, bool sortAscending)
    {
        return (sortColumn, sortAscending) switch
        {
            (AdminLogSortColumn.Timestamp, true) => source
                .OrderBy(log => log.Timestamp)
                .ThenBy(log => log.Id),

            (AdminLogSortColumn.Timestamp, false) => source
                .OrderByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id),

            (AdminLogSortColumn.Level, true) => source
                .OrderBy(log => log.Level)
                .ThenByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id),

            (AdminLogSortColumn.Level, false) => source
                .OrderByDescending(log => log.Level)
                .ThenByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id),

            (AdminLogSortColumn.Source, true) => source
                .OrderBy(log => log.Source)
                .ThenByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id),

            (AdminLogSortColumn.Source, false) => source
                .OrderByDescending(log => log.Source)
                .ThenByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id),

            (AdminLogSortColumn.User, true) => source
                .OrderBy(log => log.UserId ?? string.Empty)
                .ThenByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id),

            _ => source
                .OrderByDescending(log => log.UserId ?? string.Empty)
                .ThenByDescending(log => log.Timestamp)
                .ThenByDescending(log => log.Id)
        };
    }
}


