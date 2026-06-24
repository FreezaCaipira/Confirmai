using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminAuditSourcesTests
{
    [Fact]
    public void SecurityPolicy_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("AdminSecurityPolicy", AdminAuditSources.SecurityPolicy);
    }

    [Fact]
    public void Webhook_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Webhook", AdminAuditSources.Webhook);
    }

    [Fact]
    public void ServerIntegration_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ServerIntegration", AdminAuditSources.ServerIntegration);
    }

    [Fact]
    public void SignalR_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("SignalR", AdminAuditSources.SignalR);
    }

    [Fact]
    public void Identity_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Identity", AdminAuditSources.Identity);
    }

    [Fact]
    public void Products_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Products", AdminAuditSources.Products);
    }

    [Fact]
    public void Servers_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Servers", AdminAuditSources.Servers);
    }

    [Fact]
    public void ServerMembers_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ServerMembers", AdminAuditSources.ServerMembers);
    }

    [Fact]
    public void ServerRegistrations_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ServerRegistrations", AdminAuditSources.ServerRegistrations);
    }

    [Fact]
    public void ItemOffers_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ItemOffers", AdminAuditSources.ItemOffers);
    }

    [Fact]
    public void Payments_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Payments", AdminAuditSources.Payments);
    }

    [Fact]
    public void ApiKeys_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("ApiKeys", AdminAuditSources.ApiKeys);
    }

    [Fact]
    public void AdminSettings_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("AdminSettings", AdminAuditSources.AdminSettings);
    }
}
