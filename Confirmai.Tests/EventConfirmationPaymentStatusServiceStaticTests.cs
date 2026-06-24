using Confirmai.Enums;
using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class EventConfirmationPaymentStatusServiceStaticTests
{
    [Fact]
    public void IsAllowedTransition_PendingToPaid_ReturnsTrue()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Pending, EventConfirmationPaymentStatus.Paid });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedTransition_PendingToFailed_ReturnsTrue()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Pending, EventConfirmationPaymentStatus.Failed });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedTransition_PendingToRefunded_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Pending, EventConfirmationPaymentStatus.Refunded });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedTransition_FailedToPending_ReturnsTrue()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Failed, EventConfirmationPaymentStatus.Pending });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedTransition_FailedToPaid_ReturnsTrue()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Failed, EventConfirmationPaymentStatus.Paid });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedTransition_FailedToRefunded_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Failed, EventConfirmationPaymentStatus.Refunded });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedTransition_PaidToRefunded_ReturnsTrue()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Paid, EventConfirmationPaymentStatus.Refunded });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedTransition_PaidToPending_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Paid, EventConfirmationPaymentStatus.Pending });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedTransition_PaidToFailed_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Paid, EventConfirmationPaymentStatus.Failed });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedTransition_RefundedToAny_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result1 = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Refunded, EventConfirmationPaymentStatus.Pending });
        var result2 = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Refunded, EventConfirmationPaymentStatus.Paid });
        var result3 = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Refunded, EventConfirmationPaymentStatus.Failed });

        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void IsAllowedTransition_SameStatus_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Pending, EventConfirmationPaymentStatus.Pending });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedTransition_ExpiredToAny_ReturnsFalse()
    {
        // Act
        var method = typeof(EventConfirmationPaymentStatusService).GetMethod("IsAllowedTransition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { EventConfirmationPaymentStatus.Expired, EventConfirmationPaymentStatus.Paid });

        // Assert
        Assert.False(result);
    }
}
