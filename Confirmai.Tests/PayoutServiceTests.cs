using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

public class PayoutServiceTests
{
    [Fact]
    public async Task ProcessPayoutAsync_WithoutGroupPayoutAccount_SkipsPayout()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        
        var group = TestDataFactory.CreateGroup("Test Group", enablePaymentGateways: true);
        var user = TestDataFactory.CreateUserWithPixKey("user1", "Test User", "user@email.com");
        var evt = TestDataFactory.CreateEvent(group, "2024-01-01 10:00", 50m);
        var confirmation = new EventConfirmation
        {
            Id = 1,
            EventId = evt.Id,
            Event = evt,
            UserId = user.Id,
            User = user,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            PaymentGatewayName = "EfiBank",
            PixTxId = "tx123"
        };

        db.Groups.Add(group);
        db.Users.Add(user);
        db.Events.Add(evt);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 0.50m,
            GatewayFeeFixed = 0.25m,
            SupportedGateways = new[] { "EfiBank" }
        };

        var feeOptionsMock = new Mock<IOptions<FeeOptions>>();
        feeOptionsMock.Setup(x => x.Value).Returns(feeOptions);

        var payoutServiceMock = new Mock<IPixPayoutService>();
        var logService = new LogService(dbFactory, Mock.Of<ILogger<LogService>>());

        var payoutService = new PayoutService(
            dbFactory,
            feeOptionsMock.Object,
            payoutServiceMock.Object,
            logService);

        // Act
        await payoutService.ProcessPayoutAsync(confirmation.Id, "tx123");

        // Assert
        payoutServiceMock.Verify(
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_WithUnsupportedGateway_SkipsPayout()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        
        var group = TestDataFactory.CreateGroup("Test Group", enablePaymentGateways: true);
        var payoutAccount = TestDataFactory.CreateGroupPayoutAccount(group.Id, "organizador@email.com");
        var user = TestDataFactory.CreateUserWithPixKey("user1", "Test User", "user@email.com");
        var evt = TestDataFactory.CreateEvent(group, "2024-01-01 10:00", 50m);
        var confirmation = new EventConfirmation
        {
            Id = 1,
            EventId = evt.Id,
            Event = evt,
            UserId = user.Id,
            User = user,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            PaymentGatewayName = "AbacatePay",
            PixTxId = "tx123"
        };

        db.Groups.Add(group);
        db.GroupPayoutAccounts.Add(payoutAccount);
        db.Users.Add(user);
        db.Events.Add(evt);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var feeOptions = new FeeOptions
        {
            Enabled = true,
            AppFeeFixed = 0.50m,
            GatewayFeeFixed = 0.25m,
            SupportedGateways = new[] { "EfiBank" }
        };

        var feeOptionsMock = new Mock<IOptions<FeeOptions>>();
        feeOptionsMock.Setup(x => x.Value).Returns(feeOptions);

        var payoutServiceMock = new Mock<IPixPayoutService>();
        var logService = new LogService(dbFactory, Mock.Of<ILogger<LogService>>());

        var payoutService = new PayoutService(
            dbFactory,
            feeOptionsMock.Object,
            payoutServiceMock.Object,
            logService);

        // Act
        await payoutService.ProcessPayoutAsync(confirmation.Id, "tx123");

        // Assert
        payoutServiceMock.Verify(
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_WhenFeeNotConfigured_SkipsPayout()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        
        var group = TestDataFactory.CreateGroup("Test Group", enablePaymentGateways: true);
        var payoutAccount = TestDataFactory.CreateGroupPayoutAccount(group.Id, "organizador@email.com");
        var user = TestDataFactory.CreateUserWithPixKey("user1", "Test User", "user@email.com");
        var evt = TestDataFactory.CreateEvent(group, "2024-01-01 10:00", 50m);
        var confirmation = new EventConfirmation
        {
            Id = 1,
            EventId = evt.Id,
            Event = evt,
            UserId = user.Id,
            User = user,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            PaymentGatewayName = "EfiBank",
            PixTxId = "tx123"
        };

        db.Groups.Add(group);
        db.GroupPayoutAccounts.Add(payoutAccount);
        db.Users.Add(user);
        db.Events.Add(evt);
        db.EventConfirmations.Add(confirmation);
        await db.SaveChangesAsync();

        var feeOptions = new FeeOptions
        {
            Enabled = false,
            AppFeeFixed = 0.50m,
            GatewayFeeFixed = 0.25m,
            SupportedGateways = new[] { "EfiBank" }
        };

        var feeOptionsMock = new Mock<IOptions<FeeOptions>>();
        feeOptionsMock.Setup(x => x.Value).Returns(feeOptions);

        var payoutServiceMock = new Mock<IPixPayoutService>();
        var logService = new LogService(dbFactory, Mock.Of<ILogger<LogService>>());

        var payoutService = new PayoutService(
            dbFactory,
            feeOptionsMock.Object,
            payoutServiceMock.Object,
            logService);

        // Act
        await payoutService.ProcessPayoutAsync(confirmation.Id, "tx123");

        // Assert
        payoutServiceMock.Verify(
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
