using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogsStorageKeysTests
{
    [Fact]
    public void GlobalSearch_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.global", AdminLogsStorageKeys.GlobalSearch);
    }

    [Fact]
    public void UserFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.user", AdminLogsStorageKeys.UserFilter);
    }

    [Fact]
    public void SourceFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.source", AdminLogsStorageKeys.SourceFilter);
    }

    [Fact]
    public void MessageFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.message", AdminLogsStorageKeys.MessageFilter);
    }

    [Fact]
    public void LevelFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.level", AdminLogsStorageKeys.LevelFilter);
    }

    [Fact]
    public void StartDateFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.startDate", AdminLogsStorageKeys.StartDateFilter);
    }

    [Fact]
    public void EndDateFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.endDate", AdminLogsStorageKeys.EndDateFilter);
    }

    [Fact]
    public void Page_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.page", AdminLogsStorageKeys.Page);
    }

    [Fact]
    public void QuickRangePreset_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.quickRangePreset", AdminLogsStorageKeys.QuickRangePreset);
    }

    [Fact]
    public void AuditQuickFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.auditQuickFilter", AdminLogsStorageKeys.AuditQuickFilter);
    }

    [Fact]
    public void SortColumn_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.sortColumn", AdminLogsStorageKeys.SortColumn);
    }

    [Fact]
    public void SortAscending_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.sortAscending", AdminLogsStorageKeys.SortAscending);
    }

    [Fact]
    public void EventTypeFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.eventType", AdminLogsStorageKeys.EventTypeFilter);
    }

    [Fact]
    public void EntityTypeFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.logs.entityType", AdminLogsStorageKeys.EntityTypeFilter);
    }
}
