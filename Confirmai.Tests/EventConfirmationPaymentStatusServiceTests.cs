using Confirmai.Services.Events;
using Confirmai.Services.Core;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.Extensions.Logging.Abstractions;

namespace Confirmai.Tests;

public class EventConfirmationPaymentStatusServiceTests
{
    [Fact]
    public async Task TransitionStatusAsync_PaidToRefunded_UpdatesStatusAndWritesAudit()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,
            PixTxId = "tx-refund-1",
            PaymentGatewayName = "EfiBank"
        });
        await db.SaveChangesAsync();

        var logService = new LogService(db, NullLogger<LogService>.Instance);
        var service = new EventConfirmationPaymentStatusService(factory, logService);

        var result = await service.TransitionStatusAsync(1, EventConfirmationPaymentStatus.Refunded, "admin-1", "cliente solicitou estorno");

        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, result.PreviousStatus);
        Assert.Equal(EventConfirmationPaymentStatus.Refunded, result.CurrentStatus);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.Equal(EventConfirmationPaymentStatus.Refunded, saved.PaymentStatus);
        Assert.False(saved.HasPaid);

        var audit = verifyDb.Logs.OrderByDescending(x => x.Id).First();
        Assert.Equal(AuditEvents.PaymentRefunded, audit.EventType);
        Assert.Equal("Payment", audit.EntityType);
        Assert.Equal("1", audit.EntityId);
    }

    [Fact]
    public async Task TransitionStatusAsync_PendingToRefunded_IsRejected()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 2,
            UserId = "user-2",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var logService = new LogService(db, NullLogger<LogService>.Instance);
        var service = new EventConfirmationPaymentStatusService(factory, logService);

        var result = await service.TransitionStatusAsync(1, EventConfirmationPaymentStatus.Refunded, "admin-1", "sem pagamento");

        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Contains("Transição inválida", result.Message, StringComparison.OrdinalIgnoreCase);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.Equal(EventConfirmationPaymentStatus.Pending, saved.PaymentStatus);
    }

    [Fact]
    public async Task TransitionStatusAsync_FailedToPending_AllowsRetry()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 3,
            UserId = "user-3",
            PaymentStatus = EventConfirmationPaymentStatus.Failed,
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var logService = new LogService(db, NullLogger<LogService>.Instance);
        var service = new EventConfirmationPaymentStatusService(factory, logService);

        var result = await service.TransitionStatusAsync(1, EventConfirmationPaymentStatus.Pending, "admin-1", "tentativa de retentativa");

        Assert.True(result.Found);
        Assert.True(result.Updated);

        await using var verifyDb = factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.Equal(EventConfirmationPaymentStatus.Pending, saved.PaymentStatus);
        Assert.False(saved.HasPaid);
    }
}
