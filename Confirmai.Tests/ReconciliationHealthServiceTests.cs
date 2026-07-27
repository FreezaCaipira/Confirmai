using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Confirmai.Tests;

public class ReconciliationHealthServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, ReconciliationHealthService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"recon-{Guid.NewGuid()}");
        var svc = new ReconciliationHealthService(factory);
        return (factory, svc);
    }

    private static async Task SeedPendingConfirmationsAsync(IDbContextFactory<AppDbContext> factory, int withChargeId, int withoutChargeId)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group { Id = 1, Name = "Group", Sport = Sport.Futsal };
        db.Groups.Add(group);
        var ev = new Event { Id = 1, GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow, Location = "Loc", MaxPlayers = 10 };
        db.Events.Add(ev);
        for (int i = 0; i < withChargeId; i++)
        {
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = 1, UserId = $"u-charge-{i}", PaymentStatus = EventConfirmationPaymentStatus.Pending,
                PixTxId = $"tx{i:D32}", ConfirmedAt = DateTime.UtcNow
            });
        }
        for (int i = 0; i < withoutChargeId; i++)
        {
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = 1, UserId = $"u-nocharge-{i}", PaymentStatus = EventConfirmationPaymentStatus.Pending,
                ConfirmedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedSweepLogAsync(IDbContextFactory<AppDbContext> factory, string origin, int stillPending, DateTime timestamp)
    {
        await using var db = factory.CreateDbContext();
        db.Logs.Add(new AppLog
        {
            EventType = AuditEvents.PaymentReconciliationSweep,
            Timestamp = timestamp,
            MetadataJson = JsonSerializer.Serialize(new { origin, stillPending })
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LoadAsync_ReturnsZeroPending_WhenNoConfirmations()
    {
        var (factory, svc) = Setup();
        var data = await svc.LoadAsync(5);
        Assert.Equal(0, data.PendingWithChargeId);
        Assert.Equal(0, data.StalePending);
    }

    [Fact]
    public async Task LoadAsync_CountsPendingWithChargeId()
    {
        var (factory, svc) = Setup();
        await SeedPendingConfirmationsAsync(factory, 3, 2);

        var data = await svc.LoadAsync(5);

        Assert.Equal(3, data.PendingWithChargeId);
    }

    [Fact]
    public async Task LoadAsync_CountsStalePending()
    {
        var (factory, svc) = Setup();
        await using var db = factory.CreateDbContext();
        db.Groups.Add(new Group { Id = 1, Name = "Group", Sport = Sport.Futsal });
        db.Events.Add(new Event { Id = 1, GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow, Location = "Loc", MaxPlayers = 10 });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1, UserId = "u1", PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixTxId = "tx1", ConfirmedAt = DateTime.UtcNow.AddMinutes(-60)
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadAsync(5);

        Assert.Equal(1, data.StalePending);
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultSweepLabel_WhenNoSweepLogs()
    {
        var (factory, svc) = Setup();
        var data = await svc.LoadAsync(5);
        Assert.Contains("varredura", data.LastAutoSweepLabel.ToLowerInvariant());
    }

    [Fact]
    public async Task LoadAsync_ReturnsDefaultTrendLabel_WhenNoSweepLogs()
    {
        var (factory, svc) = Setup();
        var data = await svc.LoadAsync(5);
        Assert.Contains("tendencia", data.PendingTrendLabel.ToLowerInvariant());
    }

    [Fact]
    public async Task LoadAsync_ReturnsSweepLabel_WhenSweepLogExists()
    {
        var (factory, svc) = Setup();
        await SeedSweepLogAsync(factory, "worker.reconciliation", 5, DateTime.UtcNow.AddHours(-1));

        var data = await svc.LoadAsync(5);

        Assert.DoesNotContain("varredura", data.LastAutoSweepLabel.ToLowerInvariant());
        Assert.Matches(@"^\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}$", data.LastAutoSweepLabel);
    }

    [Fact]
    public async Task LoadAsync_ReturnsTrendLabel_WhenSweepLogExists()
    {
        var (factory, svc) = Setup();
        await SeedSweepLogAsync(factory, "worker.reconciliation", 5, DateTime.UtcNow.AddHours(-1));

        var data = await svc.LoadAsync(5);

        Assert.DoesNotContain("tendencia", data.PendingTrendLabel.ToLowerInvariant());
    }

    [Fact]
    public async Task LoadAsync_IgnoresSweepLogs_FromOtherOrigins()
    {
        var (factory, svc) = Setup();
        await SeedSweepLogAsync(factory, "manual.sweep", 5, DateTime.UtcNow);

        var data = await svc.LoadAsync(5);

        Assert.Contains("varredura", data.LastAutoSweepLabel.ToLowerInvariant());
    }

    [Fact]
    public async Task LoadAsync_SetsPendingTrendWarning_WhenDeltaExceedsThreshold()
    {
        var (factory, svc) = Setup();
        await SeedSweepLogAsync(factory, "worker.reconciliation", 10, DateTime.UtcNow);
        await SeedSweepLogAsync(factory, "worker.reconciliation", 2, DateTime.UtcNow.AddHours(-12));

        var data = await svc.LoadAsync(5);

        Assert.True(data.PendingTrendWarning);
    }

    [Fact]
    public async Task LoadAsync_DoesNotSetPendingTrendWarning_WhenDeltaBelowThreshold()
    {
        var (factory, svc) = Setup();
        await SeedSweepLogAsync(factory, "worker.reconciliation", 5, DateTime.UtcNow);
        await SeedSweepLogAsync(factory, "worker.reconciliation", 3, DateTime.UtcNow.AddHours(-12));

        var data = await svc.LoadAsync(5);

        Assert.False(data.PendingTrendWarning);
    }
}
