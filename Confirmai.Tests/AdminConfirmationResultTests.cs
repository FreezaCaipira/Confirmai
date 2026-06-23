using Confirmai.Enums;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminConfirmationResultTests
{
    [Fact]
    public void AdminTogglePaidResult_Constructor_SetsProperties()
    {
        // Arrange
        var found = true;
        var updated = true;
        var message = "Test message";
        var newStatus = EventConfirmationPaymentStatus.Paid;

        // Act
        var result = new AdminTogglePaidResult
        {
            Found = found,
            Updated = updated,
            Message = message,
            NewStatus = newStatus
        };

        // Assert
        Assert.Equal(found, result.Found);
        Assert.Equal(updated, result.Updated);
        Assert.Equal(message, result.Message);
        Assert.Equal(newStatus, result.NewStatus);
    }

    [Fact]
    public void AdminTogglePaidResult_WithDefaultValues()
    {
        // Act
        var result = new AdminTogglePaidResult();

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
        Assert.Null(result.Message);
        Assert.Null(result.NewStatus);
    }

    [Fact]
    public void AdminTogglePaidResult_WithNullMessage()
    {
        // Act
        var result = new AdminTogglePaidResult
        {
            Found = true,
            Updated = false,
            Message = null,
            NewStatus = null
        };

        // Assert
        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Null(result.Message);
        Assert.Null(result.NewStatus);
    }

    [Fact]
    public void AdminRemoveConfirmationResult_Constructor_SetsProperties()
    {
        // Arrange
        var found = true;
        var updated = true;
        var message = "Test message";

        // Act
        var result = new AdminRemoveConfirmationResult
        {
            Found = found,
            Updated = updated,
            Message = message
        };

        // Assert
        Assert.Equal(found, result.Found);
        Assert.Equal(updated, result.Updated);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public void AdminRemoveConfirmationResult_WithDefaultValues()
    {
        // Act
        var result = new AdminRemoveConfirmationResult();

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
        Assert.Null(result.Message);
    }

    [Fact]
    public void AdminRemoveConfirmationResult_WithNullMessage()
    {
        // Act
        var result = new AdminRemoveConfirmationResult
        {
            Found = true,
            Updated = false,
            Message = null
        };

        // Assert
        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Null(result.Message);
    }
}
