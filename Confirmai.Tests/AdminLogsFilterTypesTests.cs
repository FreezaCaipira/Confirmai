using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogsFilterTypesTests
{
    [Fact]
    public void AdminLogsQuickRangePreset_EnumValues()
    {
        // Act & Assert
        Assert.Equal(0, (int)AdminLogsQuickRangePreset.None);
        Assert.Equal(1, (int)AdminLogsQuickRangePreset.Today);
        Assert.Equal(2, (int)AdminLogsQuickRangePreset.Last7Days);
        Assert.Equal(3, (int)AdminLogsQuickRangePreset.Last30Days);
        Assert.Equal(4, (int)AdminLogsQuickRangePreset.CurrentMonth);
        Assert.Equal(5, (int)AdminLogsQuickRangePreset.Custom);
    }

    [Fact]
    public void AdminLogsAuditQuickFilter_EnumValues()
    {
        // Act & Assert
        Assert.Equal(0, (int)AdminLogsAuditQuickFilter.All);
        Assert.Equal(1, (int)AdminLogsAuditQuickFilter.SecurityPolicy);
        Assert.Equal(2, (int)AdminLogsAuditQuickFilter.PaymentPanelStale);
    }

    [Fact]
    public void AdminLogsQuickRangePreset_DefaultValue()
    {
        // Arrange & Act
        var preset = default(AdminLogsQuickRangePreset);

        // Assert
        Assert.Equal(AdminLogsQuickRangePreset.None, preset);
    }

    [Fact]
    public void AdminLogsAuditQuickFilter_DefaultValue()
    {
        // Arrange & Act
        var filter = default(AdminLogsAuditQuickFilter);

        // Assert
        Assert.Equal(AdminLogsAuditQuickFilter.All, filter);
    }
}
