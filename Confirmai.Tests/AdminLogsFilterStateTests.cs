using Confirmai.Enums;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogsFilterStateTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var globalSearch = "test search";
        var userId = "user123";
        var source = "TestSource";
        var message = "test message";
        var level = "Info";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(1);
        var eventType = "test.event";
        var entityType = "User";
        var page = 2;
        var quickRangePreset = AdminLogsQuickRangePreset.Last7Days;
        var auditQuickFilter = AdminLogsAuditQuickFilter.SecurityPolicy;
        var sortColumn = AdminLogSortColumn.Timestamp;
        var sortAscending = true;

        // Act
        var state = new AdminLogsFilterState
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

        // Assert
        Assert.Equal(globalSearch, state.GlobalSearch);
        Assert.Equal(userId, state.UserId);
        Assert.Equal(source, state.Source);
        Assert.Equal(message, state.Message);
        Assert.Equal(level, state.Level);
        Assert.Equal(startDate, state.StartDate);
        Assert.Equal(endDate, state.EndDate);
        Assert.Equal(eventType, state.EventType);
        Assert.Equal(entityType, state.EntityType);
        Assert.Equal(page, state.Page);
        Assert.Equal(quickRangePreset, state.QuickRangePreset);
        Assert.Equal(auditQuickFilter, state.AuditQuickFilter);
        Assert.Equal(sortColumn, state.SortColumn);
        Assert.Equal(sortAscending, state.SortAscending);
    }

    [Fact]
    public void Constructor_WithDefaultValues()
    {
        // Act
        var state = new AdminLogsFilterState();

        // Assert
        Assert.Equal(string.Empty, state.GlobalSearch);
        Assert.Equal(string.Empty, state.UserId);
        Assert.Equal(string.Empty, state.Source);
        Assert.Equal(string.Empty, state.Message);
        Assert.Equal(string.Empty, state.Level);
        Assert.Null(state.StartDate);
        Assert.Null(state.EndDate);
        Assert.Equal(string.Empty, state.EventType);
        Assert.Equal(string.Empty, state.EntityType);
        Assert.Null(state.Page);
        Assert.Null(state.QuickRangePreset);
        Assert.Null(state.AuditQuickFilter);
        Assert.Null(state.SortColumn);
        Assert.Null(state.SortAscending);
    }

    [Fact]
    public void Constructor_WithNullDates()
    {
        // Act
        var state = new AdminLogsFilterState
        {
            GlobalSearch = "test",
            StartDate = null,
            EndDate = null
        };

        // Assert
        Assert.Equal("test", state.GlobalSearch);
        Assert.Null(state.StartDate);
        Assert.Null(state.EndDate);
    }

    [Fact]
    public void Constructor_WithPageZero()
    {
        // Act
        var state = new AdminLogsFilterState
        {
            Page = 0
        };

        // Assert
        Assert.Equal(0, state.Page);
    }

    [Fact]
    public void Constructor_WithLargePageNumber()
    {
        // Act
        var state = new AdminLogsFilterState
        {
            Page = 1000
        };

        // Assert
        Assert.Equal(1000, state.Page);
    }

    [Fact]
    public void Constructor_WithAllQuickRangePresets()
    {
        // Arrange
        var presets = new[]
        {
            AdminLogsQuickRangePreset.None,
            AdminLogsQuickRangePreset.Today,
            AdminLogsQuickRangePreset.Last7Days,
            AdminLogsQuickRangePreset.Last30Days,
            AdminLogsQuickRangePreset.CurrentMonth,
            AdminLogsQuickRangePreset.Custom
        };

        // Act & Assert
        foreach (var preset in presets)
        {
            var state = new AdminLogsFilterState
            {
                QuickRangePreset = preset
            };
            Assert.Equal(preset, state.QuickRangePreset);
        }
    }

    [Fact]
    public void Constructor_WithAllAuditQuickFilters()
    {
        // Arrange
        var filters = new[]
        {
            AdminLogsAuditQuickFilter.All,
            AdminLogsAuditQuickFilter.SecurityPolicy,
            AdminLogsAuditQuickFilter.PaymentPanelStale
        };

        // Act & Assert
        foreach (var filter in filters)
        {
            var state = new AdminLogsFilterState
            {
                AuditQuickFilter = filter
            };
            Assert.Equal(filter, state.AuditQuickFilter);
        }
    }

    [Fact]
    public void Constructor_WithAllSortColumns()
    {
        // Arrange
        var columns = new[]
        {
            AdminLogSortColumn.Timestamp,
            AdminLogSortColumn.Level,
            AdminLogSortColumn.Source,
            AdminLogSortColumn.User
        };

        // Act & Assert
        foreach (var column in columns)
        {
            var state = new AdminLogsFilterState
            {
                SortColumn = column
            };
            Assert.Equal(column, state.SortColumn);
        }
    }

    [Fact]
    public void Constructor_WithSortAscendingFalse()
    {
        // Act
        var state = new AdminLogsFilterState
        {
            SortAscending = false
        };

        // Assert
        Assert.False(state.SortAscending);
    }
}
