using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Events;
using Confirmai.Services.Notification;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C31 F3 itens 1-4 — end-to-end hooks: cancel/reschedule via
/// EventNotificationService, lineup via EscalacaoService, and the
/// day/hour reminder sweep.
/// </summary>
public class C31WhatsAppFlowsTests
{
    private static (IDbContextFactory<AppDbContext> factory, WhatsAppDispatchService dispatch,
        RecordingWhatsAppSender sender) Setup(bool enabled = true)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"waf-{Guid.NewGuid()}");
        var sender = new RecordingWhatsAppSender();
        var dispatch = new WhatsAppDispatchService(factory, sender,
            Options.Create(new WhatsAppOptions
            {
                Enabled = enabled,
                DryRun = true,
                AllowedGroupJids = "111@g.us"
            }),
            NullLogger<WhatsAppDispatchService>.Instance);
        return (factory, dispatch, sender);
    }

    private static EventNotificationService Notif(
        IDbContextFactory<AppDbContext> factory, WhatsAppDispatchService dispatch) =>
        new(factory, Mock.Of<IEmailSender>(),
            NullLogger<EventNotificationService>.Instance, dispatch);

    private static async Task<int> SeedEventAsync(
        IDbContextFactory<AppDbContext> factory,
        DateTime startsAt, string? jid = "111@g.us", bool isActive = true)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "Pelada", WhatsAppGroupJid = jid };
        db.Groups.Add(group);
        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = startsAt,
            IsActive = isActive
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    // ── item 4: cancel + reschedule ──────────────────────────────────────────

    [Fact]
    public async Task CancelledEvent_NotifiesGroup_WithCancellationText()
    {
        var (factory, dispatch, sender) = Setup();
        var eventId = await SeedEventAsync(factory, DateTime.UtcNow.AddDays(1));

        await Notif(factory, dispatch).NotifyEventCancelledAsync(eventId, "admin-1");

        var (jid, text) = Assert.Single(sender.Calls);
        Assert.Equal("111@g.us", jid);
        Assert.Contains("cancelada", text);
        Assert.Contains("Pelada", text);
    }

    [Fact]
    public async Task RescheduledEvent_NotifiesGroup_WithOldAndNewTimes()
    {
        var (factory, dispatch, sender) = Setup();
        var oldStart = DateTime.UtcNow.AddDays(1);
        var eventId = await SeedEventAsync(factory, oldStart.AddHours(2));

        await Notif(factory, dispatch).NotifyEventUpdatedAsync(eventId, "admin-1", oldStart);

        var (_, text) = Assert.Single(sender.Calls);
        Assert.Contains("Horário alterado", text);
        Assert.Contains("Era:", text);
        Assert.Contains("Agora:", text);
    }

    [Fact]
    public async Task UpdateWithoutDateChange_DoesNotNotify()
    {
        var (factory, dispatch, sender) = Setup();
        var start = DateTime.UtcNow.AddDays(1);
        var eventId = await SeedEventAsync(factory, start);

        await Notif(factory, dispatch).NotifyEventUpdatedAsync(eventId, "admin-1", start);

        Assert.Empty(sender.Calls);
    }

    [Fact]
    public async Task CancelledEvent_StillNotifies_EvenWithNoConfirmedParticipants()
    {
        var (factory, dispatch, sender) = Setup();
        var eventId = await SeedEventAsync(factory, DateTime.UtcNow.AddDays(1));

        // Zero confirmations — group message must still go out.
        await Notif(factory, dispatch).NotifyEventCancelledAsync(eventId, "admin-1");

        Assert.Single(sender.Calls);
    }

    // ── item 1: lineup confirmed ─────────────────────────────────────────────

    [Fact]
    public async Task ConfirmedLineup_SendsFormattedEscalacao()
    {
        var (factory, dispatch, sender) = Setup();
        var eventId = await SeedEventAsync(factory, DateTime.UtcNow.AddDays(1));
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", FullName = "João" });
            db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "u1" });
            await db.SaveChangesAsync();
        }

        var svc = new Services.Futsal.EscalacaoService(factory, dispatch);
        await svc.ConfirmLineupAsync(eventId, new Dictionary<string, int?> { ["u1"] = 0 });

        var (_, text) = Assert.Single(sender.Calls);
        Assert.Contains("Escalacao", text);
        Assert.Contains("João", text);
    }

    // ── items 2-3: reminder sweep ────────────────────────────────────────────

    [Fact]
    public async Task Sweep_HourReminder_WhenEventStartsIn60To90Min()
    {
        var (factory, dispatch, sender) = Setup();
        var now = DateTime.UtcNow;
        await SeedEventAsync(factory, now.AddMinutes(75));

        await dispatch.RunReminderSweepAsync(now);

        var (_, text) = Assert.Single(sender.Calls);
        Assert.Contains("1 hora", text);
    }

    [Fact]
    public async Task Sweep_HourReminder_IncludesLineup_WhenConfirmed()
    {
        var (factory, dispatch, sender) = Setup();
        var now = DateTime.UtcNow;
        var eventId = await SeedEventAsync(factory, now.AddMinutes(75));
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", FullName = "João" });
            db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "u1", TeamId = 0 });
            (await db.Events.FindAsync(eventId))!.LineupConfirmedAt = now;
            await db.SaveChangesAsync();
        }

        await dispatch.RunReminderSweepAsync(now);

        var (_, text) = Assert.Single(sender.Calls);
        Assert.Contains("1 hora", text);
        Assert.Contains("TIME A", text);
        Assert.Contains("João", text);
    }

    [Fact]
    public async Task Sweep_NoHourReminder_OutsideWindow()
    {
        var (factory, dispatch, sender) = Setup();
        var now = DateTime.UtcNow;
        await SeedEventAsync(factory, now.AddMinutes(120));
        await SeedEventAsync(factory, now.AddMinutes(30));

        var attempts = await dispatch.RunReminderSweepAsync(now);

        // Both may get a DayReminder only when local hour >= 7 — the hour
        // reminder specifically must not fire outside (60,90] min.
        Assert.DoesNotContain(sender.Calls, c => c.Text.Contains("1 hora"));
    }

    [Fact]
    public async Task Sweep_DayReminder_ForEventLaterToday()
    {
        var (factory, dispatch, sender) = Setup();
        // Pick a "now" whose local hour is safely inside [7, 22] on any TZ.
        var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
            15, 0, 0, DateTimeKind.Utc);
        var localNow = now.ToLocalTime();
        if (localNow.Hour < 7) now = now.AddHours(8); // UTC+9..14 machines
        await SeedEventAsync(factory, now.AddHours(3));

        var attempts = await dispatch.RunReminderSweepAsync(now);

        Assert.Contains(sender.Calls, c => c.Text.Contains("Hoje tem"));
    }

    [Fact]
    public async Task Sweep_SecondRun_ResendsNothing()
    {
        var (factory, dispatch, sender) = Setup();
        var now = DateTime.UtcNow;
        await SeedEventAsync(factory, now.AddMinutes(75));

        await dispatch.RunReminderSweepAsync(now);
        await dispatch.RunReminderSweepAsync(now.AddMinutes(15));

        Assert.Single(sender.Calls);
    }

    [Fact]
    public async Task Sweep_SkipsCancelledAndJidlessEvents()
    {
        var (factory, dispatch, sender) = Setup();
        var now = DateTime.UtcNow;
        await SeedEventAsync(factory, now.AddMinutes(75), isActive: false);
        await SeedEventAsync(factory, now.AddMinutes(75), jid: null);

        var attempts = await dispatch.RunReminderSweepAsync(now);

        Assert.Empty(sender.Calls);
    }

    [Fact]
    public async Task Sweep_Disabled_IsNoop()
    {
        var (factory, dispatch, sender) = Setup(enabled: false);
        var now = DateTime.UtcNow;
        await SeedEventAsync(factory, now.AddMinutes(75));

        Assert.Equal(0, await dispatch.RunReminderSweepAsync(now));
        Assert.Empty(sender.Calls);
    }
}
