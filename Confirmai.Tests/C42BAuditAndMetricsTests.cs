using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

/// <summary>
/// C42-B: furos de auditoria + eventos de metrica de produto.
/// Cobre os 6 furos do passo 3c e a normalizacao de rota do page.view.
/// </summary>
public class C42BAuditAndMetricsTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
        => TestDbContextFactory.CreateInMemoryFactory($"c42b-{Guid.NewGuid()}");

    private static LogService Log(IDbContextFactory<AppDbContext> factory)
        => TestLogService.Create(factory);

    // ── PageRoutes.Normalize ────────────────────────────────────────────────

    [Theory]
    [InlineData("/", "/")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    [InlineData("/grupos", "/grupos")]
    [InlineData("/grupo/123", "/grupo/{id}")]
    [InlineData("/futsal/45", "/futsal/{id}")]
    [InlineData("/pagamento/evento/987", "/pagamento/evento/{id}")]
    [InlineData("/convite/AB12CD34", "/convite/{code}")]
    [InlineData("/convite/ABCDEFAB", "/convite/{code}")]       // codigo hex so com letras
    [InlineData("/grupo/123?tab=membros", "/grupo/{id}")]      // query fora
    [InlineData("/perfil/a@b.com", "/perfil/{email}")]          // email nunca vaza
    [InlineData("/x/3fa85f64-5717-4562-b3fc-2c963f66afa6", "/x/{guid}")]
    public void Normalize_MapsPathToRouteTemplate_WithoutIdsOrPii(string? path, string expected)
    {
        Assert.Equal(expected, PageRoutes.Normalize(path));
    }

    // ── Furo 1: confirmar presenca audita ───────────────────────────────────

    [Fact]
    public async Task FutsalConfirm_WritesConfirmationAddedAudit()
    {
        var factory = CreateFactory();
        var log = Log(factory);
        var notif = new EventNotificationService(factory, Mock.Of<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(), NullLogger<EventNotificationService>.Instance);
        var fee = new PlatformFeePolicy(Options.Create(new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = 0.75m }));
        var svc = new EventDetailService(factory, log, notif, fee);

        int eventId;
        await using (var db = factory.CreateDbContext())
        {
            db.Groups.Add(new Group { Name = "G" });
            await db.SaveChangesAsync();
            db.Events.Add(new Event { GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow.AddDays(1), MaxPlayers = 10, Price = 15m });
            await db.SaveChangesAsync();
            eventId = (await db.Events.FirstAsync()).Id;
        }

        var result = await svc.ConfirmPresenceAsync(eventId, "user-1", FutsalPosition.Outfield);

        Assert.True(result.Success);
        await using var db2 = factory.CreateDbContext();
        var row = Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.EventConfirmationAdded).ToListAsync());
        Assert.Equal("user-1", row.UserId);
        Assert.Contains("\"chargedPrice\":15", row.MetadataJson);
    }

    [Fact]
    public async Task FutsalConfirm_OnFullEvent_WritesWaitlistJoinedAudit()
    {
        var factory = CreateFactory();
        var log = Log(factory);
        var notif = new EventNotificationService(factory, Mock.Of<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(), NullLogger<EventNotificationService>.Instance);
        var fee = new PlatformFeePolicy(Options.Create(new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = 0.75m }));
        var svc = new EventDetailService(factory, log, notif, fee);

        int eventId;
        await using (var db = factory.CreateDbContext())
        {
            db.Groups.Add(new Group { Name = "G" });
            await db.SaveChangesAsync();
            db.Events.Add(new Event { GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow.AddDays(1), MaxPlayers = 1, MaxGoalkeepers = null, Price = 0 });
            await db.SaveChangesAsync();
            eventId = (await db.Events.FirstAsync()).Id;
        }

        await svc.ConfirmPresenceAsync(eventId, "user-1", FutsalPosition.Outfield);
        var waitlisted = await svc.ConfirmPresenceAsync(eventId, "user-2", FutsalPosition.Outfield);
        Assert.True(waitlisted.AddedToWaitlist);

        // Cancelamento do dono da vaga promove quem esperava.
        var promoted = await svc.CancelConfirmationAsync(eventId, "user-1");

        await using var db2 = factory.CreateDbContext();
        Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.EventWaitlistJoined).ToListAsync());
        Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.EventWaitlistPromoted).ToListAsync());
    }

    // ── Furo 4: placar e voto de MVP auditam ────────────────────────────────

    [Fact]
    public async Task SaveScore_WritesScoreRegisteredAudit()
    {
        var factory = CreateFactory();
        var svc = new EscalacaoService(factory, Log(factory));

        await using (var db = factory.CreateDbContext())
        {
            db.Groups.Add(new Group { Name = "G" });
            await db.SaveChangesAsync();
            db.Events.Add(new Event { GroupId = 1, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow.AddDays(-1) });
            await db.SaveChangesAsync();
        }

        await svc.SaveScoreAsync(1, 3, 2, "admin-1");

        await using var db2 = factory.CreateDbContext();
        var row = Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.EventScoreRegistered).ToListAsync());
        Assert.Contains("\"scoreA\":3", row.MetadataJson);
    }

    [Fact]
    public async Task CastVote_WritesVoteCastAudit()
    {
        var factory = CreateFactory();
        var svc = new EscalacaoService(factory, Log(factory));

        await svc.CastVoteAsync(7, "voter-1", "voted-1");

        await using var db = factory.CreateDbContext();
        var row = Assert.Single(await db.Logs.Where(l => l.EventType == AuditEvents.EventPostMatchVoteCast).ToListAsync());
        Assert.Equal("voter-1", row.UserId);
        Assert.Contains("\"votedForUserId\":\"voted-1\"", row.MetadataJson);
    }

    // ── Furo 2: poker confirmar e trocar de mesa auditam ────────────────────

    [Fact]
    public async Task PokerConfirm_WritesConfirmationAddedAudit()
    {
        var factory = CreateFactory();
        var policy = new PlatformFeePolicy(Options.Create(new Confirmai.Configuration.FeeOptions { PokerFeePercentMin = 5m, PokerFeePercentMax = 10m }));
        var svc = new Services.Poker.PokerDetailService(factory, policy, Log(factory));

        await using (var db = factory.CreateDbContext())
        {
            db.Groups.Add(new Group { Name = "PG", Sport = Sport.Poker, IsPrivate = false });
            await db.SaveChangesAsync();
            db.Events.Add(new Event { GroupId = 1, Sport = Sport.Poker, PokerEventType = PokerEventType.Tournament, StartsAt = DateTime.UtcNow.AddDays(1), MaxPlayers = 9, Price = 50m, PlatformFeePercent = 5m });
            await db.SaveChangesAsync();
        }

        var result = await svc.ConfirmAsync(1, "player-1");

        Assert.Equal(Services.Poker.PokerConfirmStatus.Confirmed, result.Status);
        await using var db2 = factory.CreateDbContext();
        Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.EventConfirmationAdded).ToListAsync());
    }

    [Fact]
    public async Task PokerChangeTable_WritesTableChangedAudit()
    {
        var factory = CreateFactory();
        var policy = new PlatformFeePolicy(Options.Create(new Confirmai.Configuration.FeeOptions { PokerFeePercentMin = 5m, PokerFeePercentMax = 10m }));
        var svc = new Services.Poker.PokerDetailService(factory, policy, Log(factory));

        int option2;
        await using (var db = factory.CreateDbContext())
        {
            db.Groups.Add(new Group { Name = "PG", Sport = Sport.Poker, IsPrivate = false });
            await db.SaveChangesAsync();
            db.Events.Add(new Event { GroupId = 1, Sport = Sport.Poker, PokerEventType = PokerEventType.CashGame, StartsAt = DateTime.UtcNow.AddDays(1), MaxPlayers = 9 });
            await db.SaveChangesAsync();
            db.EventPriceOptions.Add(new EventPriceOption { EventId = 1, Label = "Mesa 50", Price = 50m, PlatformFeePercent = 5m, IsActive = true, SortOrder = 0 });
            db.EventPriceOptions.Add(new EventPriceOption { EventId = 1, Label = "Mesa 100", Price = 100m, PlatformFeePercent = 5m, IsActive = true, SortOrder = 1 });
            await db.SaveChangesAsync();
            option2 = (await db.EventPriceOptions.OrderBy(o => o.SortOrder).LastAsync()).Id;
        }

        var confirm = await svc.ConfirmAsync(1, "player-1", (await FirstOptionIdAsync(factory)));
        Assert.Equal(Services.Poker.PokerConfirmStatus.Confirmed, confirm.Status);

        var changed = await svc.ChangeTableAsync(1, "player-1", option2);

        Assert.Equal(Services.Poker.PokerChangeTableStatus.Changed, changed.Status);
        await using var db2 = factory.CreateDbContext();
        var row = Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.EventConfirmationTableChanged).ToListAsync());
        Assert.Contains("\"newPrice\":100", row.MetadataJson);
    }

    private static async Task<int> FirstOptionIdAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var db = factory.CreateDbContext();
        return (await db.EventPriceOptions.OrderBy(o => o.SortOrder).FirstAsync()).Id;
    }

    // ── Furo 5: politica de seguranca vira AuditAsync ───────────────────────

    [Fact]
    public async Task SecurityPolicy_WritesAuditEvent_WithBeforeAfter()
    {
        var factory = CreateFactory();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.EnvironmentName).Returns("Production");
        var svc = new Services.Admin.AdminSecurityPolicyService(factory, env.Object, Log(factory));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1"), new Claim(ClaimTypes.Role, "admin") }, "test"));

        var ok = await svc.SetRuntimePolicyForAdminAsync(admin, new Services.Admin.RuntimeSecurityPolicy(false, 5, 15));

        Assert.True(ok);
        await using var db = factory.CreateDbContext();
        var row = Assert.Single(await db.Logs.Where(l => l.EventType == AuditEvents.AdminSecurityPolicyChanged).ToListAsync());
        Assert.Equal("admin-1", row.UserId);
        Assert.Contains("requireConfirmedEmail", row.MetadataJson);
        Assert.Contains("\"after\":false", row.MetadataJson);
    }

    // ── Metricas de produto: funil convite -> membro ────────────────────────

    [Fact]
    public async Task JoinWithCode_WritesMemberJoinedMetric_WithViaInvite()
    {
        var factory = CreateFactory();
        var log = Log(factory);
        var notif = new EventNotificationService(factory, Mock.Of<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(), NullLogger<EventNotificationService>.Instance);
        var fee = new PlatformFeePolicy(Options.Create(new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = 0.75m }));
        var eventDetail = new EventDetailService(factory, log, notif, fee);
        var svc = new Services.Groups.GroupDetailService(factory,
            Mock.Of<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(),
            log, eventDetail);

        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new ApplicationUser { Id = "u-1", UserName = "u1" });
            db.Groups.Add(new Group { Name = "G", InviteCode = "ABC12345" });
            await db.SaveChangesAsync();
        }

        var result = await svc.JoinWithCodeAsync(1, "u-1", "ABC12345");

        Assert.Equal(Services.Groups.JoinWithCodeResult.Joined, result);
        await using var db2 = factory.CreateDbContext();
        Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.GroupMemberAdded).ToListAsync());
        var metric = Assert.Single(await db2.Logs.Where(l => l.EventType == AuditEvents.GroupMemberJoined).ToListAsync());
        Assert.Contains("\"via\":\"invite\"", metric.MetadataJson);
    }

    // ── /admin/metrics: so admin ────────────────────────────────────────────

    [Fact]
    public void MetricsPage_RequiresAdminRole()
    {
        var attr = typeof(Pages.Admin.Metrics)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal("admin", attr!.Roles);
    }
}
