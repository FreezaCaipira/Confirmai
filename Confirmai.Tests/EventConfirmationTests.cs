using Confirmai.Models;
using Confirmai.Enums;

namespace Confirmai.Tests;

public class EventConfirmationTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { Id = 1 };
        Assert.Equal(1, confirmation.Id);
    }

    [Fact]
    public void EventId_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { EventId = 5 };
        Assert.Equal(5, confirmation.EventId);
    }

    [Fact]
    public void UserId_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { UserId = "user-123" };
        Assert.Equal("user-123", confirmation.UserId);
    }

    [Fact]
    public void UserId_DefaultsToEmptyString()
    {
        var confirmation = new EventConfirmation();
        Assert.Equal(string.Empty, confirmation.UserId);
    }

    [Fact]
    public void Position_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { Position = FutsalPosition.Goalkeeper };
        Assert.Equal(FutsalPosition.Goalkeeper, confirmation.Position);
    }

    [Fact]
    public void Position_CanBeNull()
    {
        var confirmation = new EventConfirmation { Position = null };
        Assert.Null(confirmation.Position);
    }

    [Fact]
    public void TeamId_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { TeamId = 0 };
        Assert.Equal(0, confirmation.TeamId);
    }

    [Fact]
    public void TeamId_CanBeNull()
    {
        var confirmation = new EventConfirmation { TeamId = null };
        Assert.Null(confirmation.TeamId);
    }

    [Fact]
    public void ConfirmedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var confirmation = new EventConfirmation { ConfirmedAt = now };
        Assert.Equal(now, confirmation.ConfirmedAt);
    }

    [Fact]
    public void ConfirmedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var confirmation = new EventConfirmation();
        var after = DateTime.UtcNow;
        Assert.InRange(confirmation.ConfirmedAt, before, after);
    }

    [Fact]
    public void PaymentStatus_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { PaymentStatus = EventConfirmationPaymentStatus.Paid };
        Assert.Equal(EventConfirmationPaymentStatus.Paid, confirmation.PaymentStatus);
    }

    [Fact]
    public void PaymentStatus_DefaultsToPending()
    {
        var confirmation = new EventConfirmation();
        Assert.Equal(EventConfirmationPaymentStatus.Pending, confirmation.PaymentStatus);
    }

    [Fact]
    public void HasPaid_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { HasPaid = true };
        Assert.True(confirmation.HasPaid);
    }

    [Fact]
    public void HasPaid_DefaultsToFalse()
    {
        var confirmation = new EventConfirmation();
        Assert.False(confirmation.HasPaid);
    }

    [Fact]
    public void PixTxId_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { PixTxId = "tx123" };
        Assert.Equal("tx123", confirmation.PixTxId);
    }

    [Fact]
    public void PixTxId_CanBeNull()
    {
        var confirmation = new EventConfirmation { PixTxId = null };
        Assert.Null(confirmation.PixTxId);
    }

    [Fact]
    public void PixBrCode_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { PixBrCode = "br-code" };
        Assert.Equal("br-code", confirmation.PixBrCode);
    }

    [Fact]
    public void PixBrCode_CanBeNull()
    {
        var confirmation = new EventConfirmation { PixBrCode = null };
        Assert.Null(confirmation.PixBrCode);
    }

    [Fact]
    public void PaymentGatewayName_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { PaymentGatewayName = "EfiBank" };
        Assert.Equal("EfiBank", confirmation.PaymentGatewayName);
    }

    [Fact]
    public void PaymentGatewayName_CanBeNull()
    {
        var confirmation = new EventConfirmation { PaymentGatewayName = null };
        Assert.Null(confirmation.PaymentGatewayName);
    }

    [Fact]
    public void MarkedPaidByUserId_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { MarkedPaidByUserId = "admin-123" };
        Assert.Equal("admin-123", confirmation.MarkedPaidByUserId);
    }

    [Fact]
    public void MarkedPaidByUserId_CanBeNull()
    {
        var confirmation = new EventConfirmation { MarkedPaidByUserId = null };
        Assert.Null(confirmation.MarkedPaidByUserId);
    }

    [Fact]
    public void MarkedPaidAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var confirmation = new EventConfirmation { MarkedPaidAt = now };
        Assert.Equal(now, confirmation.MarkedPaidAt);
    }

    [Fact]
    public void MarkedPaidAt_CanBeNull()
    {
        var confirmation = new EventConfirmation { MarkedPaidAt = null };
        Assert.Null(confirmation.MarkedPaidAt);
    }

    [Fact]
    public void PixProofImageData_CanBeSetAndGet()
    {
        var data = new byte[] { 1, 2, 3 };
        var confirmation = new EventConfirmation { PixProofImageData = data };
        Assert.Equal(data, confirmation.PixProofImageData);
    }

    [Fact]
    public void PixProofImageData_CanBeNull()
    {
        var confirmation = new EventConfirmation { PixProofImageData = null };
        Assert.Null(confirmation.PixProofImageData);
    }

    [Fact]
    public void PixProofContentType_CanBeSetAndGet()
    {
        var confirmation = new EventConfirmation { PixProofContentType = "image/jpeg" };
        Assert.Equal("image/jpeg", confirmation.PixProofContentType);
    }

    [Fact]
    public void PixProofContentType_CanBeNull()
    {
        var confirmation = new EventConfirmation { PixProofContentType = null };
        Assert.Null(confirmation.PixProofContentType);
    }

    [Fact]
    public void PixProofUploadedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var confirmation = new EventConfirmation { PixProofUploadedAt = now };
        Assert.Equal(now, confirmation.PixProofUploadedAt);
    }

    [Fact]
    public void PixProofUploadedAt_CanBeNull()
    {
        var confirmation = new EventConfirmation { PixProofUploadedAt = null };
        Assert.Null(confirmation.PixProofUploadedAt);
    }

    [Fact]
    public void IsPaymentPending_ReturnsTrue_WhenPaymentStatusIsPending()
    {
        var confirmation = new EventConfirmation { PaymentStatus = EventConfirmationPaymentStatus.Pending };
        Assert.True(confirmation.IsPaymentPending);
    }

    [Fact]
    public void IsPaymentPending_ReturnsFalse_WhenPaymentStatusIsPaid()
    {
        var confirmation = new EventConfirmation { PaymentStatus = EventConfirmationPaymentStatus.Paid };
        Assert.False(confirmation.IsPaymentPending);
    }

    [Fact]
    public void IsPaymentPaid_ReturnsTrue_WhenPaymentStatusIsPaid()
    {
        var confirmation = new EventConfirmation { PaymentStatus = EventConfirmationPaymentStatus.Paid };
        Assert.True(confirmation.IsPaymentPaid);
    }

    [Fact]
    public void IsPaymentPaid_ReturnsFalse_WhenPaymentStatusIsPending()
    {
        var confirmation = new EventConfirmation { PaymentStatus = EventConfirmationPaymentStatus.Pending };
        Assert.False(confirmation.IsPaymentPaid);
    }
}
