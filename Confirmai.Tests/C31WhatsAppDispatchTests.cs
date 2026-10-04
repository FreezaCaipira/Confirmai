using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C31 Fase 2 — dispatch guards: allowlist, idempotency via the unique
/// (EventId, MessageKind) row, cancelled/past events, failure isolation.
/// </summary>
public class C31WhatsAppDispatchTests
{
    private static WhatsAppOptions Opts(bool enabled = true, string allowed = "111@g.us") =>
        new() { Enabled = enabled, DryRun = true, AllowedGroupJids = allowed };

    private static (IDbContextFactory<AppDbContext> factory, WhatsAppDispatchService svc, RecordingWhatsAppSender sender)
        Setup(WhatsAppOptions options)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"wad-{Guid.NewGuid()}");
        var sender = new RecordingWhatsAppSender();
        var svc = new WhatsAppDispatchService(factory, sender, Options.Create(options),
            NullLogger<WhatsAppDispatchService>.Instance);
        return (factory, svc, sender);
    }

    private static async Task<int> SeedEventAsync(
        IDbContextFactory<AppDbContext> factory,
        string? jid = "111@g.us",
        bool isActive = true,
        DateTime? startsAt = null)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "G", WhatsAppGroupJid = jid };
        db.Groups.Add(group);
        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = startsAt ?? DateTime.UtcNow.AddHours(3),
            IsActive = isActive
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    [Fact]
    public async Task Dispatch_Enabled_AllowedJid_SendsAndRecords()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        var (jid, text) = Assert.Single(sender.Calls);
        Assert.Equal("111@g.us", jid);
        Assert.Equal("lembrete", text);

        await using var verify = factory.CreateDbContext();
        var row = await verify.WhatsAppDispatches.SingleAsync();
        Assert.Equal(WhatsAppDispatchStatus.Sent, row.Status);
        Assert.Equal("DayReminder", row.MessageKind);
        Assert.NotNull(row.SentAtUtc);
    }

    [Fact]
    public async Task Dispatch_SameKindTwice_SecondIsSkipped()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");
        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        Assert.Single(sender.Calls);
        await using var verify = factory.CreateDbContext();
        Assert.Single(await verify.WhatsAppDispatches.ToListAsync());
    }

    [Fact]
    public async Task Dispatch_DifferentKinds_BothSend()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "dia");
        await svc.DispatchAsync(eventId, WhatsAppMessageKind.HourReminder, "1h");

        Assert.Equal(2, sender.Calls.Count);
    }

    [Fact]
    public async Task Dispatch_JidOutsideAllowlist_NeverSends()
    {
        var (factory, svc, sender) = Setup(Opts(allowed: "999@g.us"));
        var eventId = await SeedEventAsync(factory);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        Assert.Empty(sender.Calls);
    }

    [Fact]
    public async Task Dispatch_GroupWithoutJid_NeverSends()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory, jid: null);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        Assert.Empty(sender.Calls);
    }

    [Fact]
    public async Task Dispatch_FeatureDisabled_NeverSends()
    {
        var (factory, svc, sender) = Setup(Opts(enabled: false));
        var eventId = await SeedEventAsync(factory);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        Assert.Empty(sender.Calls);
    }

    [Fact]
    public async Task Dispatch_CancelledEvent_OnlyCancellationKindSends()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory, isActive: false);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");
        await svc.DispatchAsync(eventId, WhatsAppMessageKind.EventCancelled, "cancelada");

        var call = Assert.Single(sender.Calls);
        Assert.Equal("cancelada", call.Text);
    }

    [Fact]
    public async Task Dispatch_PastEvent_SkipsAllButCancellation()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory, startsAt: DateTime.UtcNow.AddHours(-2));

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.HourReminder, "1h");
        Assert.Empty(sender.Calls);

        await svc.DispatchAsync(eventId, WhatsAppMessageKind.EventCancelled, "cancelada");
        Assert.Single(sender.Calls);
    }

    [Fact]
    public async Task Dispatch_SendFailure_RowStaysFailed_AndRetries()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory);

        sender.NextStatus = WhatsAppSendStatus.Failed;
        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        await using (var verify = factory.CreateDbContext())
        {
            var row = await verify.WhatsAppDispatches.SingleAsync();
            Assert.Equal(WhatsAppDispatchStatus.Failed, row.Status);
            Assert.Equal(1, row.Attempts);
        }

        sender.NextStatus = WhatsAppSendStatus.DryRun;
        await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        Assert.Equal(2, sender.Calls.Count);
        await using var verify2 = factory.CreateDbContext();
        var row2 = await verify2.WhatsAppDispatches.SingleAsync();
        Assert.Equal(WhatsAppDispatchStatus.Sent, row2.Status);
        Assert.Equal(2, row2.Attempts);
    }

    [Fact]
    public async Task Dispatch_ExhaustedAttempts_GivesUp()
    {
        var (factory, svc, sender) = Setup(Opts());
        var eventId = await SeedEventAsync(factory);

        sender.NextStatus = WhatsAppSendStatus.Failed;
        for (var i = 0; i < 6; i++)
            await svc.DispatchAsync(eventId, WhatsAppMessageKind.DayReminder, "lembrete");

        Assert.Equal(5, sender.Calls.Count); // MaxAttempts
    }
}
