using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminAuditLevelsTests
{
    [Fact]
    public void Success_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Info", AdminAuditLevels.Success);
    }

    [Fact]
    public void Refused_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Warning", AdminAuditLevels.Refused);
    }
}
