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
    public void Server_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Server", AuditEntities.Server);
    }

    [Fact]
    public void ItemOffer_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ItemOffer", AuditEntities.ItemOffer);
    }

    [Fact]
    public void Webhook_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Webhook", AuditEntities.Webhook);
    }

    [Fact]
    public void ApiKey_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ApiKey", AuditEntities.ApiKey);
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
}
