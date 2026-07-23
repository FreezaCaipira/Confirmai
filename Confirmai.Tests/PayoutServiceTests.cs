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
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_WithValidSetup_SendsPayoutWithIdempotencyKey()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var group = TestDataFactory.CreateGroup("Test Group", enablePaymentGateways: true);
        db.Groups.Add(group);
        await db.SaveChangesAsync();

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
        payoutServiceMock
            .Setup(x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("E2E123456");
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
            x => x.SendPayoutAsync(50m, "organizador@email.com", It.IsAny<string>(), "payout-1-tx123"),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPayoutAsync_WhenAlreadySent_SkipsDuplicatePayout()
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

        var existingRecord = new PaymentRecord
        {
            PaymentId = "tx123",
            Amount = 50.75m,
            IsPaid = true,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            BaseAmount = 50m,
            FeeAmount = 0.75m,
            PayoutStatus = PayoutStatus.Sent,
            PayoutPixKey = "organizador@email.com",
            PayoutEndToEndId = "E2EEXISTING",
            PayoutSentAt = DateTime.UtcNow
        };

        db.Groups.Add(group);
        db.GroupPayoutAccounts.Add(payoutAccount);
        db.Users.Add(user);
        db.Events.Add(evt);
        db.EventConfirmations.Add(confirmation);
        db.Payments.Add(existingRecord);
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

        // Assert — must NOT send a second payout
        payoutServiceMock.Verify(
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_WhenPayoutFails_MarksAsFailedAndIncrementsRetry()
    {
        // Arrange
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var group = TestDataFactory.CreateGroup("Test Group", enablePaymentGateways: true);
        db.Groups.Add(group);
        await db.SaveChangesAsync();

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
        payoutServiceMock
            .Setup(x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Pix key invalid"));
        var logService = new LogService(dbFactory, Mock.Of<ILogger<LogService>>());

        var payoutService = new PayoutService(
            dbFactory,
            feeOptionsMock.Object,
            payoutServiceMock.Object,
            logService);

        // Act
        await payoutService.ProcessPayoutAsync(confirmation.Id, "tx123");

        // Assert
        var record = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == "tx123");
        Assert.NotNull(record);
        Assert.Equal(PayoutStatus.Failed, record.PayoutStatus);
        Assert.Equal(1, record.PayoutRetryCount);
        Assert.NotNull(record.PayoutErrorMessage);
        Assert.Contains("Pix key invalid", record.PayoutErrorMessage);
    }

    [Fact]
    public async Task RetryFailedPayoutsAsync_WhenMaxRetriesExceeded_LogsManualIntervention()
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

        var failedRecord = new PaymentRecord
        {
            PaymentId = "tx123",
            Amount = 50.75m,
            IsPaid = true,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            BaseAmount = 50m,
            FeeAmount = 0.75m,
            PayoutStatus = PayoutStatus.Failed,
            PayoutPixKey = "organizador@email.com",
            PayoutRetryCount = 5,
            PayoutSentAt = DateTime.UtcNow.AddDays(-1)
        };

        db.Groups.Add(group);
        db.Users.Add(user);
        db.Events.Add(evt);
        db.EventConfirmations.Add(confirmation);
        db.Payments.Add(failedRecord);
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
        await payoutService.RetryFailedPayoutsAsync();

        // Assert — should NOT attempt to send since max retries exceeded
        payoutServiceMock.Verify(
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RetryFailedPayoutsAsync_WhenBackoffNotElapsed_SkipsRetry()
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

        var failedRecord = new PaymentRecord
        {
            PaymentId = "tx123",
            Amount = 50.75m,
            IsPaid = true,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            BaseAmount = 50m,
            FeeAmount = 0.75m,
            PayoutStatus = PayoutStatus.Failed,
            PayoutPixKey = "organizador@email.com",
            PayoutRetryCount = 1,
            PayoutSentAt = DateTime.UtcNow // just failed — backoff not elapsed
        };

        db.Groups.Add(group);
        db.Users.Add(user);
        db.Events.Add(evt);
        db.EventConfirmations.Add(confirmation);
        db.Payments.Add(failedRecord);
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
        await payoutService.RetryFailedPayoutsAsync();

        // Assert — should NOT attempt since backoff (10min for retryCount=1) not elapsed
        payoutServiceMock.Verify(
            x => x.SendPayoutAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
