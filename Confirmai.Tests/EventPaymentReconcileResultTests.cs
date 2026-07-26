using Confirmai.Services.Payment;

namespace Confirmai.Tests;

public class EventPaymentReconcileResultTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var found = true;
        var updated = true;
        var isPaid = true;
        var message = "Test message";
        var confirmationId = 1;
        var gatewayName = "TestGateway";

        // Act
        var result = new EventPaymentReconcileResult(
            found,
            updated,
            isPaid,
            message,
            confirmationId,
            gatewayName);

        // Assert
        Assert.Equal(found, result.Found);
        Assert.Equal(updated, result.Updated);
        Assert.Equal(isPaid, result.IsPaid);
        Assert.Equal(message, result.Message);
        Assert.Equal(confirmationId, result.ConfirmationId);
        Assert.Equal(gatewayName, result.GatewayName);
    }

    [Fact]
    public void Constructor_WithNullValues()
    {
        // Act
        var result = new EventPaymentReconcileResult(
            false,
            false,
            false,
            null!,
            null,
            null);

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
        Assert.False(result.IsPaid);
        Assert.Null(result.Message);
        Assert.Null(result.ConfirmationId);
        Assert.Null(result.GatewayName);
    }

    [Fact]
    public void EventPaymentReconciliationSweepResult_HadAnyWork_ReturnsTrueWhenConsideredGreaterThanZero()
    {
        // Arrange
        var result = new EventPaymentReconciliationSweepResult(
            Considered: 10,
            Updated: 5,
            StillPending: 3,
            NotFound: 2);

        // Act
        var hadWork = result.HadAnyWork;

        // Assert
        Assert.True(hadWork);
    }

    [Fact]
    public void EventPaymentReconciliationSweepResult_HadAnyWork_ReturnsFalseWhenConsideredIsZero()
    {
        // Arrange
        var result = new EventPaymentReconciliationSweepResult(
            Considered: 0,
            Updated: 0,
            StillPending: 0,
            NotFound: 0);

        // Act
        var hadWork = result.HadAnyWork;

        // Assert
        Assert.False(hadWork);
    }

    [Fact]
    public void EventPaymentReconciliationSweepResult_Constructor_SetsProperties()
    {
        // Arrange
        var considered = 100;
        var updated = 50;
        var stillPending = 30;
        var notFound = 20;

        // Act
        var result = new EventPaymentReconciliationSweepResult(
            Considered: considered,
            Updated: updated,
            StillPending: stillPending,
            NotFound: notFound);

        // Assert
        Assert.Equal(considered, result.Considered);
        Assert.Equal(updated, result.Updated);
        Assert.Equal(stillPending, result.StillPending);
        Assert.Equal(notFound, result.NotFound);
    }
}
