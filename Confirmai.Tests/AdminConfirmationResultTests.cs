using Confirmai.Enums;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminConfirmationResultTests
{
    [Fact]
    public void AdminTogglePaidResult_Constructor_SetsProperties()
    {
        // Act
        var result = new AdminTogglePaidResult
        {
            Found = true,
            Updated = true,
            DenyReason = AdminMutationDenyReason.None,
            NewStatus = EventConfirmationPaymentStatus.Paid
        };

        // Assert
        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.Equal(AdminMutationDenyReason.None, result.DenyReason);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, result.NewStatus);
    }

    [Fact]
    public void AdminTogglePaidResult_WithDefaultValues()
    {
        // Act
        var result = new AdminTogglePaidResult();

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.None, result.DenyReason);
        Assert.Null(result.NewStatus);
    }

    [Fact]
    public void AdminTogglePaidResult_Denied_CarriesReason()
    {
        // Act
        var result = new AdminTogglePaidResult
        {
            Found = true,
            Updated = false,
            DenyReason = AdminMutationDenyReason.Forbidden
        };

        // Assert
        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.Forbidden, result.DenyReason);
    }

    [Fact]
    public void AdminRemoveConfirmationResult_Constructor_SetsProperties()
    {
        // Act
        var result = new AdminRemoveConfirmationResult
        {
            Found = true,
            Updated = true
        };

        // Assert
        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.Equal(AdminMutationDenyReason.None, result.DenyReason);
    }

    [Fact]
    public void AdminRemoveConfirmationResult_WithDefaultValues()
    {
        // Act
        var result = new AdminRemoveConfirmationResult();

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.None, result.DenyReason);
    }

    [Fact]
    public void AdminRemoveConfirmationResult_Denied_CarriesReason()
    {
        // Act
        var result = new AdminRemoveConfirmationResult
        {
            Found = true,
            Updated = false,
            DenyReason = AdminMutationDenyReason.GatewayPayment
        };

        // Assert
        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.GatewayPayment, result.DenyReason);
    }
}
