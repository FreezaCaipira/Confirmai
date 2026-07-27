using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.Extensions.Logging.Abstractions;

namespace Confirmai.Tests;

public class AdminConfirmationServiceTests
{
    [Fact]
    public async Task TogglePaidAsync_PendingToPaid_SucceedsAndRecordsAdmin()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.TogglePaidAsync(confirmationId, "admin-1");

        // Assert
        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, result.NewStatus);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.Equal(EventConfirmationPaymentStatus.Paid, saved.PaymentStatus);
        Assert.True(saved.HasPaid);
        Assert.Equal("admin-1", saved.MarkedPaidByUserId);
        Assert.NotNull(saved.MarkedPaidAt);

        var audit = verifyDb.Logs.OrderByDescending(x => x.Id).First();
        Assert.Equal(AuditEvents.EventConfirmationPaidManual, audit.EventType);
    }

    [Fact]
    public async Task TogglePaidAsync_PaidToPending_SucceedsAndClearsAdminMetadata()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,
            Position = FutsalPosition.Outfield,
            MarkedPaidByUserId = "admin-1",
            MarkedPaidAt = DateTime.UtcNow
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.TogglePaidAsync(confirmationId, "admin-1");

        // Assert
        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, result.NewStatus);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.Equal(EventConfirmationPaymentStatus.Pending, saved.PaymentStatus);
        Assert.False(saved.HasPaid);
        Assert.Null(saved.MarkedPaidByUserId);
        Assert.Null(saved.MarkedPaidAt);

        var audit = verifyDb.Logs.OrderByDescending(x => x.Id).First();
        Assert.Equal(AuditEvents.EventConfirmationUnpaidManual, audit.EventType);
    }

    [Fact]
    public async Task TogglePaidAsync_PaidWithGatewayPayment_RejectsUnmarking()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,
            Position = FutsalPosition.Outfield,
            PaymentGatewayName = "EfiBank",
            PixTxId = "tx-123"
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.TogglePaidAsync(confirmationId, "admin-1");

        // Assert
        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.NotNull(result.Message);
        Assert.Contains("gateway", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.True(saved.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, saved.PaymentStatus);
    }

    [Fact]
    public async Task TogglePaidAsync_PendingWithGatewayName_AllowsToggleToPaid()
    {
        // Arrange - Pending with gateway name should still be toggleable
        // (can happen if gateway payment was initiated but not yet confirmed)
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield,
            PaymentGatewayName = "EfiBank"  // Gateway initiated but not confirmed
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.TogglePaidAsync(confirmationId, "admin-1");

        // Assert - should succeed because HasPaid is false
        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, result.NewStatus);
    }

    [Fact]
    public async Task TogglePaidAsync_ConfirmationNotFound_ReturnsFalse()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var service = new AdminConfirmationService(factory);

        // Act
        var result = await service.TogglePaidAsync(999, "admin-1");

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
    }

    [Fact]
    public async Task TogglePaidAsync_WithoutLogService_SkipsAudit()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var service = new AdminConfirmationService(factory, logService: null);

        // Act
        var result = await service.TogglePaidAsync(confirmationId, "admin-1");

        // Assert
        Assert.True(result.Found);
        Assert.True(result.Updated);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.True(saved.HasPaid);
        Assert.Equal("admin-1", saved.MarkedPaidByUserId);
        // No logs created
        var logCount = verifyDb.Logs.Count();
        Assert.Equal(0, logCount);
    }

    [Fact]
    public async Task RemoveConfirmationAsync_Success_RemovesAndAudits()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.RemoveConfirmationAsync(confirmationId, "admin-1");

        // Assert
        Assert.True(result.Found);
        Assert.True(result.Updated);

        await using var verifyDb = factory.CreateDbContext();
        var count = verifyDb.EventConfirmations.Count();
        Assert.Equal(0, count);

        var audit = verifyDb.Logs.OrderByDescending(x => x.Id).First();
        Assert.Equal(AuditEvents.EventConfirmationRemoved, audit.EventType);
    }

    [Fact]
    public async Task RemoveConfirmationAsync_WithGatewayPayment_Rejects()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,
            Position = FutsalPosition.Outfield,
            PaymentGatewayName = "EfiBank",
            PixTxId = "tx-123"
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var service = new AdminConfirmationService(factory);

        // Act
        var result = await service.RemoveConfirmationAsync(confirmationId, "admin-1");

        // Assert
        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.NotNull(result.Message);
        Assert.Contains("gateway", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var verifyDb = factory.CreateDbContext();
        var count = verifyDb.EventConfirmations.Count();
        Assert.Equal(1, count);  // Still exists
    }

    [Fact]
    public async Task RemoveConfirmationAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var service = new AdminConfirmationService(factory);

        // Act
        var result = await service.RemoveConfirmationAsync(999, "admin-1");

        // Assert
        Assert.False(result.Found);
        Assert.False(result.Updated);
    }

    [Fact]
    public async Task TogglePaidAsync_WithMultipleConfirmations_UpdatesOnlyTarget()
    {
        // Arrange - verify isolation: only update the targeted confirmation
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf1 = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        var conf2 = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-2",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(conf1);
        db.EventConfirmations.Add(conf2);
        await db.SaveChangesAsync();

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.TogglePaidAsync(conf1.Id, "admin-1");

        // Assert
        Assert.True(result.Updated);

        await using var verifyDb = factory.CreateDbContext();
        var saved1 = verifyDb.EventConfirmations.Single(c => c.Id == conf1.Id);
        var saved2 = verifyDb.EventConfirmations.Single(c => c.Id == conf2.Id);

        Assert.True(saved1.HasPaid);
        Assert.False(saved2.HasPaid);
    }

    [Fact]
    public async Task TogglePaidAsync_AuditMetadataIsComplete()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var conf = new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        var confirmationId = conf.Id;

        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var service = new AdminConfirmationService(factory, logService);

        // Act
        var result = await service.TogglePaidAsync(confirmationId, "admin-1");

        // Assert
        await using var verifyDb = factory.CreateDbContext();
        var audit = verifyDb.Logs.OrderByDescending(x => x.Id).First();

        Assert.Equal(AuditEntities.EventConfirmation, audit.EntityType);
        Assert.Equal(confirmationId.ToString(), audit.EntityId);
        Assert.Equal("admin-1", audit.UserId);
        Assert.Equal("AdminConfirmation", audit.Source);
        Assert.NotNull(audit.MetadataJson);
        Assert.Contains("NewStatus", audit.MetadataJson);
        Assert.Contains("Paid", audit.MetadataJson);
    }
}
