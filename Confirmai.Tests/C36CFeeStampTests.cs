using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

/// <summary>
/// C36-C Fase 0 — ressalva de dinheiro do C34 (decisao do Robson): a taxa de
/// plataforma e resolvida UMA vez, na criacao da confirmacao. QR, resumo e
/// stamp leem o valor carimbado; isencao concedida depois nao retroage e a
/// expirada depois nao surpreende. Legados sem carimbo resolvem por ConfirmedAt.
/// </summary>
public class C36CFeeStampTests
{
    private const decimal Fee = 0.75m;

    private static PlatformFeePolicy NewPolicy(decimal fee = Fee)
        => new(Options.Create(new FeeOptions { ManualPlatformFeeFixed = fee }));

    private static EventDetailService NewDetailSvc(IDbContextFactory<AppDbContext> f)
        => new(f,
               new LogService(f, NullLogger<LogService>.Instance),
               new EventNotificationService(f, Mock.Of<IEmailSender>(),
                   NullLogger<EventNotificationService>.Instance),
               NewPolicy());

    private static PlatformFeeLedgerService NewLedger(IDbContextFactory<AppDbContext> f)
        => new(f, NewPolicy());

    private static async Task<(int groupId, int eventId)> SeedWaivableEventAsync(
        AppDbContext db, DateTime? waivedUntil = null, Sport sport = Sport.Futsal,
        DateTime? waivedFrom = null)
    {
        var group = new Group
        {
            Name = "G", CreatedByUserId = "org-1", Sport = sport,
            EnablePaymentGateways = false, PlatformFeeWaivedUntil = waivedUntil,
            PlatformFeeWaivedFrom = waivedFrom,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        var ev = new Event
        {
            GroupId = group.Id, Sport = sport, Price = 15m,
            StartsAt = DateTime.UtcNow.AddDays(1), MaxPlayers = 10,
            CreatedByUserId = "org-1",
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return (group.Id, ev.Id);
    }

    // ---- Carimbo na criacao ----

    [Fact]
    public async Task ConfirmPresence_WaivedGroup_StampsZeroAtCreation()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var (_, eventId) = await SeedWaivableEventAsync(db, DateTime.UtcNow.AddDays(7));

        var result = await NewDetailSvc(factory).ConfirmPresenceAsync(eventId, "p1", FutsalPosition.Outfield);

        Assert.True(result.Success);
        var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
        Assert.Equal(0m, conf.PlatformFeeAmount);
    }

    [Fact]
    public async Task ConfirmPresence_NonWaivedGroup_StampsConfiguredFee()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var (_, eventId) = await SeedWaivableEventAsync(db);

        await NewDetailSvc(factory).ConfirmPresenceAsync(eventId, "p1", FutsalPosition.Outfield);

        var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
        Assert.Equal(Fee, conf.PlatformFeeAmount);
    }

    [Fact]
    public async Task ConfirmPresence_GatewaysGroup_NoStamp()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var (groupId, eventId) = await SeedWaivableEventAsync(db);
        (await db.Groups.FindAsync(groupId))!.EnablePaymentGateways = true;
        await db.SaveChangesAsync();

        await NewDetailSvc(factory).ConfirmPresenceAsync(eventId, "p1", FutsalPosition.Outfield);

        var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
        Assert.Null(conf.PlatformFeeAmount);
    }

    [Fact]
    public async Task WaiveAfterConfirmation_DoesNotRetroact_StampKeepsFullFee()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var (groupId, eventId) = await SeedWaivableEventAsync(db);
        await NewDetailSvc(factory).ConfirmPresenceAsync(eventId, "p1", FutsalPosition.Outfield);

        // Isencao concedida DEPOIS da confirmacao: o carimbo nao muda.
        (await db.Groups.FindAsync(groupId))!.PlatformFeeWaivedUntil = DateTime.UtcNow.AddDays(7);
        var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        await db.SaveChangesAsync();

        var stamped = await NewLedger(factory).StampFeeOnPaidAsync(conf.Id);

        Assert.False(stamped); // ja carimbada — idempotente
        Assert.Equal(Fee, conf.PlatformFeeAmount);
    }

    // ---- Legado sem carimbo: resolve por ConfirmedAt, nao por UtcNow ----

    [Fact]
    public async Task Stamp_Legacy_ConfirmedDuringWaiver_PaidAfterExpiry_StampsZero()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        // Janela de isencao JA fechada (-3d..-1d), mas a confirmacao foi criada dentro dela.
        var (_, eventId) = await SeedWaivableEventAsync(db,
            waivedUntil: DateTime.UtcNow.AddDays(-1), waivedFrom: DateTime.UtcNow.AddDays(-3));
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = eventId, UserId = "p1",
            ConfirmedAt = DateTime.UtcNow.AddDays(-2), // dentro do prazo
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            PlatformFeeAmount = null, // legado
        });
        await db.SaveChangesAsync();
        var confId = db.EventConfirmations.Single(c => c.EventId == eventId).Id;

        var stamped = await NewLedger(factory).StampFeeOnPaidAsync(confId);

        Assert.True(stamped);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Equal(0m, verify.EventConfirmations.Find(confId)!.PlatformFeeAmount);
    }

    [Fact]
    public async Task Stamp_Legacy_ConfirmedBeforeWaiver_StampsFullFee()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        // Isencao concedida ONTEM (-1d..+7d); a confirmacao nasceu antes da concessao.
        var (_, eventId) = await SeedWaivableEventAsync(db,
            waivedUntil: DateTime.UtcNow.AddDays(7), waivedFrom: DateTime.UtcNow.AddDays(-1));
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = eventId, UserId = "p1",
            ConfirmedAt = DateTime.UtcNow.AddDays(-30),
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            PlatformFeeAmount = null,
        });
        await db.SaveChangesAsync();
        var confId = db.EventConfirmations.Single(c => c.EventId == eventId).Id;

        var stamped = await NewLedger(factory).StampFeeOnPaidAsync(confId);

        Assert.True(stamped);
        await using var verify2 = await factory.CreateDbContextAsync();
        Assert.Equal(Fee, verify2.EventConfirmations.Find(confId)!.PlatformFeeAmount);
    }

    // ---- Waitlist promotion carimba tambem ----

    [Fact]
    public async Task WaitlistPromotion_StampsFeeAtPromotion()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var (_, eventId) = await SeedWaivableEventAsync(db, DateTime.UtcNow.AddDays(7));
        var ev = await db.Events.FindAsync(eventId);
        ev!.MaxPlayers = 1; ev.MaxGoalkeepers = 0;
        await db.SaveChangesAsync();
        var svc = NewDetailSvc(factory);

        var r1 = await svc.ConfirmPresenceAsync(eventId, "p1", FutsalPosition.Outfield);
        var r2 = await svc.ConfirmPresenceAsync(eventId, "p2", FutsalPosition.Outfield);
        Assert.True(r1.Success && !r1.AddedToWaitlist);
        Assert.True(r2.Success && r2.AddedToWaitlist);

        await svc.CancelConfirmationAsync(eventId, "p1");

        var promoted = await db.EventConfirmations.SingleAsync(c => c.UserId == "p2");
        Assert.Equal(0m, promoted.PlatformFeeAmount);
    }
}
