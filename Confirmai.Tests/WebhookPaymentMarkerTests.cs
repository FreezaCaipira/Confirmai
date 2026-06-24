using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment.Shared;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Confirmai.Tests;

public class WebhookPaymentMarkerTests
{
    [Fact]
    public async Task MarkConfirmationPaidAsync_WithValidTxId_MarksAsPaid()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test Group");
        var ev = TestDataFactory.CreateEvent(group, "2024-01-01 10:00", 50m);
        var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
        var confirmation = new EventConfirmation
        {
            Id = 1,
            EventId = ev.Id,
            UserId = user.Id,
            PixTxId = "tx123",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false
        };
        db.Groups.Add(group);
        db.Events.Add(ev);
        db.Users.Add(user);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act
        var result = await marker.MarkConfirmationPaidAsync("tx123", "Pix", "TestGateway");

        // Assert
        Assert.True(result);
        var updatedConfirmation = await db.EventConfirmations.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, updatedConfirmation?.PaymentStatus);
        Assert.True(updatedConfirmation?.HasPaid);
        Assert.Equal("Pix", updatedConfirmation?.PaymentGatewayName);
    }

    [Fact]
    public async Task MarkConfirmationPaidAsync_WithInvalidTxId_ReturnsFalse()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act
        var result = await marker.MarkConfirmationPaidAsync("invalid-tx", "Pix", "TestGateway");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MarkConfirmationPaidAsync_WhenAlreadyPaid_ReturnsTrue()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test Group");
        var ev = TestDataFactory.CreateEvent(group, "2024-01-01 10:00", 50m);
        var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
        var confirmation = new EventConfirmation
        {
            Id = 1,
            EventId = ev.Id,
            UserId = user.Id,
            PixTxId = "tx123",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true
        };
        db.Groups.Add(group);
        db.Events.Add(ev);
        db.Users.Add(user);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act
        var result = await marker.MarkConfirmationPaidAsync("tx123", "Pix", "TestGateway");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MarkConfirmationPaidAsync_WhenRefunded_ReturnsTrue()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test Group");
        var ev = TestDataFactory.CreateEvent(group, "2024-01-01 10:00", 50m);
        var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
        var confirmation = new EventConfirmation
        {
            Id = 1,
            EventId = ev.Id,
            UserId = user.Id,
            PixTxId = "tx123",
            PaymentStatus = EventConfirmationPaymentStatus.Refunded,
            HasPaid = false
        };
        db.Groups.Add(group);
        db.Events.Add(ev);
        db.Users.Add(user);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act
        var result = await marker.MarkConfirmationPaidAsync("tx123", "Pix", "TestGateway");

        // Assert
        Assert.True(result);
        var updatedConfirmation = await db.EventConfirmations.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.Equal(EventConfirmationPaymentStatus.Refunded, updatedConfirmation?.PaymentStatus);
    }

    [Fact]
    public async Task MarkMarketplacePaymentPaidAsync_WithValidChargeId_MarksAsPaid()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var payment = TestDataFactory.SeedPayment(db, isPaid: false, amount: 50m, method: "Pix", paymentId: "charge123");
        await db.SaveChangesAsync();

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act
        await marker.MarkMarketplacePaymentPaidAsync("charge123", "TestGateway");

        // Assert
        var updatedPayment = await db.Payments.FirstOrDefaultAsync(p => p.Id == payment.Id);
        Assert.True(updatedPayment?.IsPaid);
        Assert.NotNull(updatedPayment?.PaidAt);
    }

    [Fact]
    public async Task MarkMarketplacePaymentPaidAsync_WithInvalidChargeId_DoesNotThrow()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act & Assert - should not throw
        await marker.MarkMarketplacePaymentPaidAsync("invalid-charge", "TestGateway");
    }

    [Fact]
    public async Task MarkMarketplacePaymentPaidAsync_WhenAlreadyPaid_DoesNotThrow()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var payment = TestDataFactory.SeedPayment(db, isPaid: true, amount: 50m, method: "Pix", paymentId: "charge123");
        await db.SaveChangesAsync();

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDbContextFactory<AppDbContext>>(dbFactory)
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILogger<LogService>>();
        var logService = new LogService(dbFactory, logger);
        var eventBus = new PaymentEventBus();
        var marker = new WebhookPaymentMarker(db, logService, eventBus);

        // Act & Assert - should not throw
        await marker.MarkMarketplacePaymentPaidAsync("charge123", "TestGateway");
    }
}
