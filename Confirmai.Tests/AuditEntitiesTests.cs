using Confirmai.Services.Core;

namespace Confirmai.Tests;

public class AuditEntitiesTests
{
    [Fact]
    public void User_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("User", AuditEntities.User);
    }

    [Fact]
    public void Product_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Product", AuditEntities.Product);
    }

    [Fact]
    public void Payment_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Payment", AuditEntities.Payment);
    }

    [Fact]
    public void Setting_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Setting", AuditEntities.Setting);
    }

    [Fact]
    public void Event_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Event", AuditEntities.Event);
    }

    [Fact]
    public void Group_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Group", AuditEntities.Group);
    }

    [Fact]
    public void EventConfirmation_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("EventConfirmation", AuditEntities.EventConfirmation);
    }

    [Fact]
    public void PageView_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("PageView", AuditEntities.PageView);
    }
}
