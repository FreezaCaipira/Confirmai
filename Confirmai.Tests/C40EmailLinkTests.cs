using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

/// <summary>
/// C40 F1 — os 5 emails de notificacao saem com CTA para a partida/grupo
/// (link absoluto de <c>App__PublicBaseUrl</c>/<c>WhatsApp__PublicBaseUrl</c>);
/// sem base configurada nenhum link sai — nunca relativo. Mailbox ganha a
/// linha do link quando ha base.
/// </summary>
public class C40EmailLinkTests
{
    private const string Base = "https://app.test";

    private static (AppDbContext db, EventNotificationService svc, Mock<IEmailSender> emailMock)
        Build(string? baseUrl = Base)
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var emailMock = new Mock<IEmailSender>();
        var uiText = new UiTextService(new LanguagePreferenceService());
        var svc = new EventNotificationService(
            factory, emailMock.Object, NullLogger<EventNotificationService>.Instance,
            emailTemplate: new EmailTemplateService(uiText),
            uiText: uiText,
            links: new AppLinks(
                Options.Create(new AppOptions { PublicBaseUrl = baseUrl }),
                Options.Create(new WhatsAppOptions())));
        return (db, svc, emailMock);
    }

    private static Event SeedConfirmedEvent(AppDbContext db, string organizerId, params string[] participantIds)
    {
        var group = new Group { Name = "Racha", Sport = Sport.Futsal };
        db.Groups.Add(group);
        var ev = new Event
        {
            Group = group, Sport = Sport.Futsal, Location = "Arena",
            StartsAt = DateTime.UtcNow.AddDays(3), MaxPlayers = 10,
            CreatedByUserId = organizerId, IsActive = true,
        };
        db.Events.Add(ev);
        foreach (var uid in participantIds.Prepend(organizerId).Distinct())
            db.Users.Add(new ApplicationUser { Id = uid, UserName = uid, Email = $"{uid}@test.com" });
        foreach (var uid in participantIds)
            db.EventConfirmations.Add(new EventConfirmation { Event = ev, UserId = uid });
        db.SaveChanges();
        return ev;
    }

    private static string CapturedHtml(Mock<IEmailSender> emailMock)
    {
        var html = emailMock.Invocations
            .Where(i => i.Arguments.Count == 3)
            .Select(i => i.Arguments[2] as string)
            .FirstOrDefault(h => h is not null);
        Assert.NotNull(html);
        return html!;
    }

    // ── AppLinks ─────────────────────────────────────────────────────────────

    [Fact]
    public void AppLinks_EventUrl_ResolvesSportPath()
    {
        var links = new AppLinks(
            Options.Create(new AppOptions { PublicBaseUrl = $"{Base}/" }), // trailing slash trims
            Options.Create(new WhatsAppOptions()));

        Assert.Equal($"{Base}/futsal/5",
            links.EventUrl(new Event { Id = 5, Sport = Sport.Futsal }));
        Assert.Equal($"{Base}/poker/7",
            links.EventUrl(new Event { Id = 7, Sport = Sport.Poker }));
        Assert.Equal($"{Base}/grupo/3", links.GroupUrl(3));
        Assert.Equal($"{Base}/grupo/3/pagamentos", links.GroupPaymentsUrl(3));
    }

    [Fact]
    public void AppLinks_WhatsAppBase_WinsOverAppBase()
    {
        var links = new AppLinks(
            Options.Create(new AppOptions { PublicBaseUrl = "https://app.example" }),
            Options.Create(new WhatsAppOptions { PublicBaseUrl = "https://wa-config.example" }));

        Assert.Equal("https://wa-config.example/grupo/1", links.GroupUrl(1));
    }

    [Fact]
    public void AppLinks_NoBase_AllNull()
    {
        var links = new AppLinks(
            Options.Create(new AppOptions()), Options.Create(new WhatsAppOptions()));

        Assert.Null(links.EventUrl(new Event { Id = 1, Sport = Sport.Futsal }));
        Assert.Null(links.GroupUrl(1));
        Assert.Null(links.GroupPaymentsUrl(1));
    }

    // ── CTA nos 5 emails ────────────────────────────────────────────────────

    [Fact]
    public async Task Cancelled_EmailAndMailbox_LinkToGroup()
    {
        var (db, svc, emailMock) = Build();
        var ev = SeedConfirmedEvent(db, "org-1", "p1");

        await svc.NotifyEventCancelledAsync(ev.Id, "org-1");

        var html = CapturedHtml(emailMock);
        Assert.Contains($"href=\"{Base}/grupo/{ev.GroupId}\"", html);
        Assert.Contains("Abrir o grupo", html);
        Assert.Contains($"{Base}/grupo/{ev.GroupId}", db.UserMailboxMessages.First().Body);
    }

    [Fact]
    public async Task Updated_Email_LinksToEvent()
    {
        var (db, svc, emailMock) = Build();
        var ev = SeedConfirmedEvent(db, "org-1", "p1");

        await svc.NotifyEventUpdatedAsync(ev.Id, "org-1", ev.StartsAt.AddHours(-2));

        Assert.Contains($"href=\"{Base}/futsal/{ev.Id}\"", CapturedHtml(emailMock));
    }

    [Fact]
    public async Task RecurringCreated_Email_LinksToEvent()
    {
        var (db, svc, emailMock) = Build();
        var group = new Group { Name = "Racha", Sport = Sport.Futsal };
        db.Groups.Add(group);
        var ev = new Event
        {
            Group = group, Sport = Sport.Poker, Location = "Club",
            StartsAt = DateTime.UtcNow.AddDays(7), CreatedByUserId = "creator-1", IsActive = true,
        };
        db.Events.Add(ev);
        db.Users.Add(new ApplicationUser { Id = "creator-1", UserName = "c", Email = "c@test.com" });
        db.Users.Add(new ApplicationUser { Id = "m1", UserName = "m1", Email = "m1@test.com" });
        db.GroupMembers.Add(new GroupMember { Group = group, UserId = "m1", Role = GroupMemberRole.Member });
        db.SaveChanges();

        await svc.NotifyNewRecurringEventAsync(ev.Id);

        // poker links to /poker/{id}
        Assert.Contains($"href=\"{Base}/poker/{ev.Id}\"", CapturedHtml(emailMock));
        Assert.Contains($"{Base}/poker/{ev.Id}", db.UserMailboxMessages.First().Body);
    }

    [Fact]
    public async Task WaitlistPromoted_Email_LinksToEvent()
    {
        var (db, svc, emailMock) = Build();
        var group = new Group { Name = "Racha", Sport = Sport.Futsal };
        db.Groups.Add(group);
        var ev = new Event
        {
            Group = group, Sport = Sport.Futsal, Location = "Arena",
            StartsAt = DateTime.UtcNow.AddDays(3), CreatedByUserId = "org-1", IsActive = true,
        };
        db.Events.Add(ev);
        db.Users.Add(new ApplicationUser { Id = "org-1", UserName = "o", Email = "o@test.com" });
        db.Users.Add(new ApplicationUser { Id = "p1", UserName = "p1", Email = "p1@test.com" });
        db.SaveChanges();

        await svc.NotifyWaitlistPromotedAsync(ev.Id, "p1");

        Assert.Contains($"href=\"{Base}/futsal/{ev.Id}\"", CapturedHtml(emailMock));
    }

    [Fact]
    public async Task Delinquency_Email_LinksToGroupPayments()
    {
        var (db, svc, emailMock) = Build();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "admin" });
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1", Email = "u1@test.com" });
        db.SaveChanges();

        await svc.NotifyDelinquencyAsync("admin-1", "u1", "Racha", 42,
            new List<(DateTime, decimal)> { (DateTime.UtcNow.AddDays(-2), 15m) });

        var html = CapturedHtml(emailMock);
        Assert.Contains($"href=\"{Base}/grupo/42/pagamentos\"", html);
        Assert.Contains("Ver meus pagamentos", html);
        Assert.Contains($"{Base}/grupo/42/pagamentos", db.UserMailboxMessages.First().Body);
    }

    // ── sem base: nenhum link sai ────────────────────────────────────────────

    [Fact]
    public async Task NoBase_EmailAndMailbox_HaveNoLink()
    {
        var (db, svc, emailMock) = Build(baseUrl: null);
        var ev = SeedConfirmedEvent(db, "org-1", "p1");

        await svc.NotifyEventCancelledAsync(ev.Id, "org-1");

        var html = CapturedHtml(emailMock);
        Assert.DoesNotContain("href=", html);
        Assert.DoesNotContain("grupo/", html);
        Assert.DoesNotContain("futsal/", html);
        Assert.DoesNotContain("http", db.UserMailboxMessages.First().Body);
    }

    [Fact]
    public async Task EmailUrl_CarriesOnlyTheId_NoPii()
    {
        var (db, svc, emailMock) = Build();
        var ev = SeedConfirmedEvent(db, "org-1", "p1");

        await svc.NotifyEventUpdatedAsync(ev.Id, "org-1", ev.StartsAt.AddHours(-2));

        var html = CapturedHtml(emailMock);
        // a URL do CTA e exatamente base+path+id: nenhum token, email ou nome viaja nela
        Assert.Matches($"href=\"{Base.Replace(".", "\\.")}/futsal/{ev.Id}\"", html);
        Assert.DoesNotContain("p1@test.com", html.Split("href=")[1]);
    }
}
