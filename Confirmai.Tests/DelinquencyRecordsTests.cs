using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class DelinquencyRecordsTests
{
    [Fact]
    public void DelinquencyEntry_Constructor_SetsProperties()
    {
        // Arrange
        var confirmationId = 1;
        var eventId = 100;
        var eventDate = DateTime.UtcNow;
        var eventPrice = 50.00m;
        var eventHref = "/futsal/100";
        var hasProof = true;

        // Act
        var entry = new DelinquencyEntry(
            confirmationId,
            eventId,
            eventDate,
            eventPrice,
            eventHref,
            hasProof);

        // Assert
        Assert.Equal(confirmationId, entry.ConfirmationId);
        Assert.Equal(eventId, entry.EventId);
        Assert.Equal(eventDate, entry.EventDate);
        Assert.Equal(eventPrice, entry.EventPrice);
        Assert.Equal(eventHref, entry.EventHref);
        Assert.Equal(hasProof, entry.HasProof);
    }

    [Fact]
    public void UserDelinquency_Constructor_SetsProperties()
    {
        // Arrange
        var userId = "user123";
        var userName = "Test User";
        var entries = new List<DelinquencyEntry>
        {
            new DelinquencyEntry(1, 100, DateTime.UtcNow, 50.00m, "/futsal/100", false),
            new DelinquencyEntry(2, 101, DateTime.UtcNow, 30.00m, "/futsal/101", true)
        };

        // Act
        var userDelinquency = new UserDelinquency(userId, userName, entries);

        // Assert
        Assert.Equal(userId, userDelinquency.UserId);
        Assert.Equal(userName, userDelinquency.UserName);
        Assert.Equal(2, userDelinquency.Entries.Count);
        Assert.Equal(80.00m, userDelinquency.TotalAmount);
    }

    [Fact]
    public void UserDelinquency_TotalAmount_CalculatesCorrectly()
    {
        // Arrange
        var entries = new List<DelinquencyEntry>
        {
            new DelinquencyEntry(1, 100, DateTime.UtcNow, 25.50m, "/futsal/100", false),
            new DelinquencyEntry(2, 101, DateTime.UtcNow, 74.50m, "/futsal/101", true)
        };
        var userDelinquency = new UserDelinquency("user123", "Test User", entries);

        // Act
        var total = userDelinquency.TotalAmount;

        // Assert
        Assert.Equal(100.00m, total);
    }

    [Fact]
    public void UserDelinquency_TotalAmount_WithEmptyEntries()
    {
        // Arrange
        var entries = new List<DelinquencyEntry>();
        var userDelinquency = new UserDelinquency("user123", "Test User", entries);

        // Act
        var total = userDelinquency.TotalAmount;

        // Assert
        Assert.Equal(0m, total);
    }

    [Fact]
    public void PaymentHistoryEntry_Constructor_SetsProperties()
    {
        // Arrange
        var userName = "Test User";
        var eventDate = DateTime.UtcNow;
        var eventPrice = 50.00m;
        var eventHref = "/futsal/100";
        var adminName = "Admin User";
        var markedAt = DateTime.UtcNow;

        // Act
        var entry = new PaymentHistoryEntry(
            userName,
            eventDate,
            eventPrice,
            eventHref,
            adminName,
            markedAt);

        // Assert
        Assert.Equal(userName, entry.UserName);
        Assert.Equal(eventDate, entry.EventDate);
        Assert.Equal(eventPrice, entry.EventPrice);
        Assert.Equal(eventHref, entry.EventHref);
        Assert.Equal(adminName, entry.AdminName);
        Assert.Equal(markedAt, entry.MarkedAt);
    }

    [Fact]
    public void PendingProofEntry_Constructor_SetsProperties()
    {
        // Arrange
        var confirmationId = 1;
        var userId = "user123";
        var userName = "Test User";
        var eventId = 100;
        var eventName = "Test Event";
        var eventDate = DateTime.UtcNow;
        var eventPrice = 50.00m;
        var eventHref = "/futsal/100";
        var proofUploadedAt = DateTime.UtcNow;

        // Act
        var entry = new PendingProofEntry(
            confirmationId,
            userId,
            userName,
            eventId,
            eventName,
            eventDate,
            eventPrice,
            eventHref,
            proofUploadedAt);

        // Assert
        Assert.Equal(confirmationId, entry.ConfirmationId);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(userName, entry.UserName);
        Assert.Equal(eventId, entry.EventId);
        Assert.Equal(eventName, entry.EventName);
        Assert.Equal(eventDate, entry.EventDate);
        Assert.Equal(eventPrice, entry.EventPrice);
        Assert.Equal(eventHref, entry.EventHref);
        Assert.Equal(proofUploadedAt, entry.ProofUploadedAt);
    }
}
