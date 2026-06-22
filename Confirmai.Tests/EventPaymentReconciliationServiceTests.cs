using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Interfaces;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Confirmai.Tests;

public class EventPaymentReconciliationServiceTests
{
    [Fact]
    public async Task ReconcileByChargeIdAsync_MarksConfirmationPaid_WhenGatewayReportsPaid()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        db.Gateways.Add(new GatewayInfo { Name = "FakeGateway", Enabled = true });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PixTxId = "charge-1",
            PaymentGatewayName = "FakeGateway",
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var gateway = new FakeEventPaymentGateway("FakeGateway", paidChargeId: "charge-1");
        var gatewayFactory = new EventPaymentGatewayFactory(new[] { gateway }, new GatewayService(factory));
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"reconciliation-{Guid.NewGuid()}");
        var service = new EventPaymentReconciliationService(factory, gatewayFactory, new LogService(dbFactory, NullLogger<LogService>.Instance));

        var result = await service.ReconcileByChargeIdAsync("charge-1", actorUserId: "admin-1");

        Assert.True(result.Found);
        Assert.True(result.Updated);
        Assert.True(result.IsPaid);
        Assert.Equal(1, result.ConfirmationId);

        await using var verifyDb = factory.CreateDbContext();
        var saved = await verifyDb.EventConfirmations.SingleAsync();
        Assert.True(saved.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, saved.PaymentStatus);
        Assert.Equal("FakeGateway", saved.PaymentGatewayName);
    }

    [Fact]
    public async Task ReconcileByChargeIdAsync_DoesNotUpdate_WhenGatewayStillPending()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        db.Gateways.Add(new GatewayInfo { Name = "FakeGateway", Enabled = true });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PixTxId = "charge-2",
            PaymentGatewayName = "FakeGateway",
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var gateway = new FakeEventPaymentGateway("FakeGateway", paidChargeId: null);
        var gatewayFactory = new EventPaymentGatewayFactory(new[] { gateway }, new GatewayService(factory));
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"reconciliation-{Guid.NewGuid()}");
        var service = new EventPaymentReconciliationService(factory, gatewayFactory, new LogService(dbFactory, NullLogger<LogService>.Instance));

        var result = await service.ReconcileByChargeIdAsync("charge-2", actorUserId: "admin-1");

        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.False(result.IsPaid);

        await using var verifyDb = factory.CreateDbContext();
        var saved = await verifyDb.EventConfirmations.SingleAsync();
        Assert.False(saved.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, saved.PaymentStatus);
    }

    [Fact]
    public async Task ReconcilePendingConfirmationsAsync_UpdatesOnlyChargesReportedAsPaid()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        db.Gateways.Add(new GatewayInfo { Name = "FakeGateway", Enabled = true });
        db.EventConfirmations.AddRange(
            new EventConfirmation
            {
                EventId = 1,
                UserId = "user-1",
                PixTxId = "charge-1",
                PaymentGatewayName = "FakeGateway",
                HasPaid = false
            },
            new EventConfirmation
            {
                EventId = 2,
                UserId = "user-2",
                PixTxId = "charge-2",
                PaymentGatewayName = "FakeGateway",
                HasPaid = false
            });
        await db.SaveChangesAsync();

        var gateway = new FakeEventPaymentGateway("FakeGateway", paidChargeId: "charge-1");
        var gatewayFactory = new EventPaymentGatewayFactory(new[] { gateway }, new GatewayService(factory));
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"reconciliation-{Guid.NewGuid()}");
        var service = new EventPaymentReconciliationService(factory, gatewayFactory, new LogService(dbFactory, NullLogger<LogService>.Instance));

        var result = await service.ReconcilePendingConfirmationsAsync();

        Assert.Equal(2, result.Considered);
        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.StillPending);
        Assert.Equal(0, result.NotFound);

        await using var verifyDb = factory.CreateDbContext();
        var confirmations = await verifyDb.EventConfirmations.OrderBy(c => c.Id).ToListAsync();

        Assert.True(confirmations[0].HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, confirmations[0].PaymentStatus);
        Assert.False(confirmations[1].HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, confirmations[1].PaymentStatus);
    }

    [Fact]
    public async Task ReconcileByChargeIdAsync_DoesNotUpdate_WhenConfirmationIsRefunded()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        db.Gateways.Add(new GatewayInfo { Name = "FakeGateway", Enabled = true });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1,
            UserId = "user-1",
            PixTxId = "charge-refunded",
            PaymentGatewayName = "FakeGateway",
            PaymentStatus = EventConfirmationPaymentStatus.Refunded,
            HasPaid = false
        });
        await db.SaveChangesAsync();

        var gateway = new FakeEventPaymentGateway("FakeGateway", paidChargeId: "charge-refunded");
        var gatewayFactory = new EventPaymentGatewayFactory(new[] { gateway }, new GatewayService(factory));
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"reconciliation-{Guid.NewGuid()}");
        var service = new EventPaymentReconciliationService(factory, gatewayFactory, new LogService(dbFactory, NullLogger<LogService>.Instance));

        var result = await service.ReconcileByChargeIdAsync("charge-refunded", actorUserId: "admin-1");

        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.False(result.IsPaid);
        Assert.Contains("não é elegível", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── ExpireStalePixChargesAsync ────────────────────────────────────────────

    [Fact]
    public async Task ExpireStalePixChargesAsync_MarksOldPendingChargesAsExpired()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        db.EventConfirmations.AddRange(
            new EventConfirmation { EventId = 1, UserId = "u1", PixTxId = "TX_OLD",
                ConfirmedAt = DateTime.UtcNow.AddHours(-3), PaymentStatus = EventConfirmationPaymentStatus.Pending },
            new EventConfirmation { EventId = 1, UserId = "u2", PixTxId = "TX_RECENT",
                ConfirmedAt = DateTime.UtcNow.AddMinutes(-30), PaymentStatus = EventConfirmationPaymentStatus.Pending });
        await db.SaveChangesAsync();

        var service = BuildReconciliationService(db, factory);
        var expired = await service.ExpireStalePixChargesAsync(TimeSpan.FromHours(2));

        Assert.Equal(1, expired);

        await using var verifyDb = factory.CreateDbContext();
        var confirmations = await verifyDb.EventConfirmations.OrderBy(c => c.Id).ToListAsync();
        Assert.Equal(EventConfirmationPaymentStatus.Expired,  confirmations[0].PaymentStatus);
        Assert.Equal(EventConfirmationPaymentStatus.Pending,  confirmations[1].PaymentStatus);
    }

    [Fact]
    public async Task ExpireStalePixChargesAsync_IgnoresAlreadyPaidConfirmations()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1, UserId = "u1", PixTxId = "TX_PAID",
            ConfirmedAt = DateTime.UtcNow.AddHours(-3),
            PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true
        });
        await db.SaveChangesAsync();

        var service = BuildReconciliationService(db, factory);
        var expired = await service.ExpireStalePixChargesAsync(TimeSpan.FromHours(2));

        Assert.Equal(0, expired);
    }

    [Fact]
    public async Task ExpireStalePixChargesAsync_IgnoresConfirmationsWithNoPixTxId()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();

        // No PixTxId → not a Pix charge (e.g., free event)
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1, UserId = "u1", PixTxId = null,
            ConfirmedAt = DateTime.UtcNow.AddHours(-3),
            PaymentStatus = EventConfirmationPaymentStatus.Pending
        });
        await db.SaveChangesAsync();

        var service = BuildReconciliationService(db, factory);
        var expired = await service.ExpireStalePixChargesAsync(TimeSpan.FromHours(2));

        Assert.Equal(0, expired);
    }

    private static EventPaymentReconciliationService BuildReconciliationService(
        AppDbContext db, IDbContextFactory<AppDbContext> factory)
    {
        var gateway     = new FakeEventPaymentGateway("FakeGateway", paidChargeId: null);
        var gwFactory   = new EventPaymentGatewayFactory(new[] { gateway }, new GatewayService(factory));
        var dbFactory   = TestDbContextFactory.CreateInMemoryFactory($"reconciliation-{Guid.NewGuid()}");
        var log         = new LogService(dbFactory, NullLogger<LogService>.Instance);
        return new EventPaymentReconciliationService(factory, gwFactory, log);
    }

    private sealed class FakeEventPaymentGateway : IEventPaymentGateway
    {
        private readonly string _paidChargeId;

        public FakeEventPaymentGateway(string name, string? paidChargeId)
        {
            Name = name;
            DisplayName = name;
            _paidChargeId = paidChargeId ?? string.Empty;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public bool IsAvailable => true;

        public Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId)
            => Task.FromResult(new EventPaymentChargeResult("fake-charge", "fake-brcode"));

        public Task<bool> IsChargePaidAsync(string chargeId)
            => Task.FromResult(string.Equals(chargeId, _paidChargeId, StringComparison.OrdinalIgnoreCase));
    }
}
