using Confirmai.Services.Events;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class EventNotificationServiceTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static (AppDbContext db, EventNotificationService svc, Mock<IEmailSender> emailMock)
        Build()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var emailMock     = new Mock<IEmailSender>();
        var svc           = new EventNotificationService(factory, emailMock.Object, NullLogger<EventNotificationService>.Instance);
        return (db, svc, emailMock);
    }

    /// <summary>Seeds the minimum graph needed: group + event + N confirmed users.</summary>
    private static (Group group, Event ev) SeedEvent(
        AppDbContext db,
        string organizerId,
        params string[] participantIds)
    {
        var group = new Group { Name = "Racha do Zé", Sport = Sport.Futsal };
        db.Groups.Add(group);
        db.SaveChanges();

        var ev = new Event
        {
            GroupId          = group.Id,
            Sport            = Sport.Futsal,
            Location         = "Arena Central",
            StartsAt         = DateTime.UtcNow.AddDays(3),
            MaxPlayers       = 10,
            CreatedByUserId  = organizerId,
            IsActive         = true,
        };
        db.Events.Add(ev);
        db.SaveChanges();

        // Add organizer as user (may or may not be a participant)
        var allIds = participantIds.Prepend(organizerId).Distinct();
        foreach (var uid in allIds)
        {
            if (!db.Users.Any(u => u.Id == uid))
            {
                db.Users.Add(new ApplicationUser { Id = uid, UserName = uid, Email = $"{uid}@test.com" });
            }
        }
        db.SaveChanges();

        foreach (var uid in participantIds)
        {
            db.EventConfirmations.Add(new EventConfirmation { EventId = ev.Id, UserId = uid });
        }
        db.SaveChanges();

        return (group, ev);
    }

    // ── NotifyEventCancelledAsync ─────────────────────────────────────────────

    [Fact]
    public async Task NotifyEventCancelledAsync_SendsMailbox_ToAllConfirmedParticipants()
    {
        var (db, svc, _) = Build();
        SeedEvent(db, organizerId: "org-1", "p1", "p2", "p3");
        var ev = db.Events.First();

        await svc.NotifyEventCancelledAsync(ev.Id, cancelledByUserId: "org-1");

        var messages = db.UserMailboxMessages.ToList();
        Assert.Equal(3, messages.Count);
        Assert.All(messages, m => Assert.Contains("[Confirmai] Cancelamento:", m.Subject));
        Assert.All(messages, m => Assert.Equal("org-1", m.SenderUserId));
        Assert.Contains(messages, m => m.RecipientUserId == "p1");
        Assert.Contains(messages, m => m.RecipientUserId == "p2");
        Assert.Contains(messages, m => m.RecipientUserId == "p3");
    }

    [Fact]
    public async Task NotifyEventCancelledAsync_ExcludesOrganizer_FromRecipients()
    {
        var (db, svc, _) = Build();
        // Organizer is also a confirmed participant
        SeedEvent(db, organizerId: "org-1", "org-1", "p1");
        var ev = db.Events.First();

        await svc.NotifyEventCancelledAsync(ev.Id, cancelledByUserId: "org-1");

        var messages = db.UserMailboxMessages.ToList();
        Assert.Single(messages);
        Assert.Equal("p1", messages[0].RecipientUserId);
        Assert.DoesNotContain(messages, m => m.RecipientUserId == "org-1");
    }

    [Fact]
    public async Task NotifyEventCancelledAsync_DoesNothing_WhenEventNotFound()
    {
        var (db, svc, emailMock) = Build();

        await svc.NotifyEventCancelledAsync(eventId: 9999, cancelledByUserId: "org-1");

        Assert.Empty(db.UserMailboxMessages);
        emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyEventCancelledAsync_DoesNothing_WhenNoParticipantsOtherThanOrganizer()
    {
        var (db, svc, _) = Build();
        SeedEvent(db, organizerId: "org-1"); // no other confirmed participants
        var ev = db.Events.First();

        await svc.NotifyEventCancelledAsync(ev.Id, cancelledByUserId: "org-1");

        Assert.Empty(db.UserMailboxMessages);
    }

    [Fact]
    public async Task NotifyEventCancelledAsync_SendsEmail_BestEffort()
    {
        var (db, svc, emailMock) = Build();
        SeedEvent(db, organizerId: "org-1", "p1");
        var ev = db.Events.First();

        await svc.NotifyEventCancelledAsync(ev.Id, cancelledByUserId: "org-1");

        emailMock.Verify(
            e => e.SendEmailAsync("p1@test.com", It.Is<string>(s => s.Contains("Cancelamento")), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyEventCancelledAsync_DoesNotThrow_WhenEmailFails()
    {
        var (db, svc, emailMock) = Build();
        SeedEvent(db, organizerId: "org-1", "p1");
        var ev = db.Events.First();

        emailMock
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP offline"));

        // Must not propagate the email exception
        var exception = await Record.ExceptionAsync(() =>
            svc.NotifyEventCancelledAsync(ev.Id, cancelledByUserId: "org-1"));

        Assert.Null(exception);
        // Mailbox message was still persisted before the email attempt
        Assert.Single(db.UserMailboxMessages);
    }

    // ── NotifyEventUpdatedAsync ───────────────────────────────────────────────

    [Fact]
    public async Task NotifyEventUpdatedAsync_SendsMessages_WhenDateChangedMoreThanOneMinute()
    {
        var (db, svc, _) = Build();
        SeedEvent(db, organizerId: "org-1", "p1", "p2");
        var ev = db.Events.First();

        var oldStartsAt = ev.StartsAt.AddHours(-2); // 2h before the current StartsAt

        await svc.NotifyEventUpdatedAsync(ev.Id, updatedByUserId: "org-1", oldStartsAt);

        var messages = db.UserMailboxMessages.ToList();
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Contains("[Confirmai] Atualização:", m.Subject));
    }

    [Fact]
    public async Task NotifyEventUpdatedAsync_SkipsNotification_WhenDateChangeIsUnderOneMinute()
    {
        var (db, svc, _) = Build();
        SeedEvent(db, organizerId: "org-1", "p1");
        var ev = db.Events.First();

        // oldStartsAt is only 30 seconds away from ev.StartsAt
        var oldStartsAt = ev.StartsAt.AddSeconds(-30);

        await svc.NotifyEventUpdatedAsync(ev.Id, updatedByUserId: "org-1", oldStartsAt);

        Assert.Empty(db.UserMailboxMessages);
    }

    [Fact]
    public async Task NotifyEventUpdatedAsync_DoesNothing_WhenEventNotFound()
    {
        var (db, svc, emailMock) = Build();

        await svc.NotifyEventUpdatedAsync(9999, "org-1", DateTime.UtcNow.AddDays(-1));

        Assert.Empty(db.UserMailboxMessages);
        emailMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyEventUpdatedAsync_ExcludesUpdater_FromRecipients()
    {
        var (db, svc, _) = Build();
        SeedEvent(db, organizerId: "org-1", "org-1", "p1");
        var ev = db.Events.First();

        await svc.NotifyEventUpdatedAsync(ev.Id, "org-1", ev.StartsAt.AddHours(-1));

        var messages = db.UserMailboxMessages.ToList();
        Assert.Single(messages);
        Assert.Equal("p1", messages[0].RecipientUserId);
    }

    [Fact]
    public async Task NotifyEventUpdatedAsync_MessageBody_ContainsOldAndNewDates()
    {
        var (db, svc, _) = Build();
        SeedEvent(db, organizerId: "org-1", "p1");
        var ev          = db.Events.First();
        var oldStartsAt = ev.StartsAt.AddDays(-7);

        await svc.NotifyEventUpdatedAsync(ev.Id, "org-1", oldStartsAt);

        var msg = db.UserMailboxMessages.First();
        Assert.Contains("Data anterior", msg.Body);
        Assert.Contains("Nova data", msg.Body);
    }

    // ── NotifyNewRecurringEventAsync ──────────────────────────────────────────

    /// <summary>Seeds a group + event + GroupMembers (non-creator) for recurring-event tests.</summary>
    private static (Group group, Event ev) SeedRecurringEvent(
        AppDbContext db,
        string creatorId,
        params string[] memberIds)
    {
        var group = new Group { Name = "Racha Recorrente", Sport = Sport.Futsal };
        db.Groups.Add(group);
        db.SaveChanges();

        var ev = new Event
        {
            GroupId         = group.Id,
            Sport           = Sport.Futsal,
            Location        = "Arena",
            StartsAt        = DateTime.UtcNow.AddDays(7),
            MaxPlayers      = 10,
            CreatedByUserId = creatorId,
            IsActive        = true,
        };
        db.Events.Add(ev);

        // Creator always added as group member
        if (!db.Users.Any(u => u.Id == creatorId))
            db.Users.Add(new ApplicationUser { Id = creatorId, UserName = creatorId, Email = $"{creatorId}@test.com" });

        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = creatorId, Role = GroupMemberRole.Admin });

        foreach (var uid in memberIds)
        {
            if (!db.Users.Any(u => u.Id == uid))
                db.Users.Add(new ApplicationUser { Id = uid, UserName = uid, Email = $"{uid}@test.com" });

            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = uid, Role = GroupMemberRole.Member });
        }

        db.SaveChanges();
        return (group, ev);
    }

    [Fact]
    public async Task NotifyNewRecurringEventAsync_SendsMailbox_ToGroupMembers()
    {
        var (db, svc, _) = Build();
        SeedRecurringEvent(db, creatorId: "creator-1", "member-1", "member-2");
        var ev = db.Events.First();

        await svc.NotifyNewRecurringEventAsync(ev.Id);

        var messages = db.UserMailboxMessages.ToList();
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Contains("[Confirmai] Nova partida:", m.Subject));
    }

    [Fact]
    public async Task NotifyNewRecurringEventAsync_ExcludesCreator_FromRecipients()
    {
        var (db, svc, _) = Build();
        SeedRecurringEvent(db, creatorId: "creator-1", "member-1");
        var ev = db.Events.First();

        await svc.NotifyNewRecurringEventAsync(ev.Id);

        var messages = db.UserMailboxMessages.ToList();
        Assert.Single(messages);
        Assert.Equal("member-1", messages[0].RecipientUserId);
        Assert.DoesNotContain(messages, m => m.RecipientUserId == "creator-1");
    }

    [Fact]
    public async Task NotifyNewRecurringEventAsync_DoesNothing_WhenEventNotFound()
    {
        var (db, svc, _) = Build();

        await svc.NotifyNewRecurringEventAsync(999999);

        Assert.Empty(db.UserMailboxMessages);
    }

    [Fact]
    public async Task NotifyNewRecurringEventAsync_DoesNothing_WhenNoOtherGroupMembers()
    {
        var (db, svc, _) = Build();
        SeedRecurringEvent(db, creatorId: "creator-only"); // no other members
        var ev = db.Events.First();

        await svc.NotifyNewRecurringEventAsync(ev.Id);

        Assert.Empty(db.UserMailboxMessages);
    }

    [Fact]
    public async Task NotifyNewRecurringEventAsync_MessageBody_ContainsEventDetails()
    {
        var (db, svc, _) = Build();
        SeedRecurringEvent(db, creatorId: "creator-1", "member-1");
        var ev = db.Events.First();

        await svc.NotifyNewRecurringEventAsync(ev.Id);

        var msg = db.UserMailboxMessages.First();
        Assert.Contains("Racha Recorrente", msg.Body);
        Assert.Contains("Confirmai", msg.Body);
    }
}
