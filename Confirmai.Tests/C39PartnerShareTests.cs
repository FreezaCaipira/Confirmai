using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Services.Poker;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

/// <summary>
/// C39-C F12 (D4) — parceria de taxa: o jogador paga
/// `taxa × (1 - share/100)` carimbado em <c>PlayerFeeAmount</c>; a taxa
/// cheia (<c>PlatformFeeAmount</c>) segue acumulando para o repasse.
/// So o admin do sistema edita a parceria, com auditoria.
/// </summary>
public class C39PartnerShareTests
{
    private static (IDbContextFactory<AppDbContext> factory, PokerDetailService detail, PlatformFeePolicy policy) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"partner-{Guid.NewGuid()}");
        var policy = new PlatformFeePolicy(Options.Create(new FeeOptions
        {
            ManualPlatformFeeFixed = 0.75m,
            PokerFeePercentMin = 5m,
            PokerFeePercentMax = 10m,
        }));
        return (factory, new PokerDetailService(factory, policy), policy);
    }

    private static async Task<int> SeedTournamentAsync(
        IDbContextFactory<AppDbContext> factory, decimal sharePercent)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group
        {
            Name = "Parceiro", Sport = Sport.Poker,
            CreatedByUserId = "creator-1",
            PartnerFeeSharePercent = sharePercent,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = "player-1",
            Role = GroupMemberRole.Member, CreatedAt = DateTime.UtcNow,
        });
        var ev = new Event
        {
            GroupId = group.Id, Sport = Sport.Poker,
            PokerEventType = PokerEventType.Tournament,
            StartsAt = DateTime.UtcNow.AddDays(1),
            Price = 100m, PlatformFeePercent = 10m,
            CreatedByUserId = "creator-1", IsActive = true,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    private static async Task<(decimal? platform, decimal? player, decimal total)> ConfirmAndTotalAsync(
        IDbContextFactory<AppDbContext> factory, PokerDetailService detail, int eventId)
    {
        var result = await detail.ConfirmAsync(eventId, "player-1");
        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations
            .Include(c => c.Event).ThenInclude(e => e.Group)
            .SingleAsync(c => c.EventId == eventId);
        // mesmo calculo da tela de pagar: carimbo da parceria vence a taxa cheia
        var fee = PlatformFeePolicy.FeeChargedToPlayer(conf) ?? 0m;
        var total = ManualPlatformFee.TotalToPay(false, EventCharge.PriceOf(conf)!.Value, fee);
        return (conf.PlatformFeeAmount, conf.PlayerFeeAmount, total);
    }

    [Theory]
    [InlineData(0, 10, 110)]    // sem parceria: jogador paga a taxa toda
    [InlineData(50, 5, 105)]    // parceiro absorve metade
    [InlineData(100, 0, 100)]   // parceiro absorve tudo -> so a entrada
    public async Task Confirm_StampsPlayerFee_ByShare(
        double share, double expectedPlayerFee, double expectedTotal)
    {
        var (factory, detail, _) = Setup();
        var eventId = await SeedTournamentAsync(factory, (decimal)share);

        var (platform, player, total) = await ConfirmAndTotalAsync(factory, detail, eventId);

        Assert.Equal(10m, platform); // taxa cheia p/ o repasse, sempre 10% de 100
        Assert.Equal((decimal)expectedPlayerFee, player);
        Assert.Equal((decimal)expectedTotal, total);
    }

    [Fact]
    public async Task ChangingShare_DoesNotRewriteStampedConfirmation()
    {
        var (factory, detail, _) = Setup();
        var eventId = await SeedTournamentAsync(factory, 0m);
        var (_, player, _) = await ConfirmAndTotalAsync(factory, detail, eventId);
        Assert.Equal(10m, player);

        await using (var db = factory.CreateDbContext())
        {
            (await db.Groups.SingleAsync()).PartnerFeeSharePercent = 100m;
            await db.SaveChangesAsync();
        }

        await using var verify = factory.CreateDbContext();
        var conf = await verify.EventConfirmations.SingleAsync();
        Assert.Equal(10m, conf.PlayerFeeAmount); // carimbo protege quem ja deve
        Assert.Equal(10m, conf.PlatformFeeAmount);
    }

    [Fact]
    public void FeeChargedToPlayer_LegacyRow_FallsBackToFullFee()
    {
        var conf = new EventConfirmation
        {
            PlatformFeeAmount = 10m,
            PlayerFeeAmount = null, // carimbada antes do C39-C
        };
        Assert.Equal(10m, PlatformFeePolicy.FeeChargedToPlayer(conf));

        var partnered = new EventConfirmation
        {
            PlatformFeeAmount = 10m,
            PlayerFeeAmount = 4m,
        };
        Assert.Equal(4m, PlatformFeePolicy.FeeChargedToPlayer(partnered));
    }

    [Fact]
    public async Task ChangeTable_RestampsPlayerFee()
    {
        var (factory, detail, _) = Setup();
        var eventId = await SeedTournamentAsync(factory, 50m);

        await using (var db = factory.CreateDbContext())
        {
            var ev = await db.Events.SingleAsync(e => e.Id == eventId);
            ev.PokerEventType = PokerEventType.CashGame;
            ev.PriceOptions.Add(new EventPriceOption
            {
                Label = "M1", Price = 100m, PlatformFeePercent = 10m, SortOrder = 0, IsActive = true,
            });
            ev.PriceOptions.Add(new EventPriceOption
            {
                Label = "M2", Price = 200m, PlatformFeePercent = 10m, SortOrder = 1, IsActive = true,
            });
            await db.SaveChangesAsync();
        }

        await using (var db = factory.CreateDbContext())
        {
            var t1 = db.EventPriceOptions.OrderBy(o => o.SortOrder).First().Id;
            var t2 = db.EventPriceOptions.OrderBy(o => o.SortOrder).Skip(1).First().Id;
            await detail.ConfirmAsync(eventId, "player-1", t1);
            var result = await detail.ChangeTableAsync(eventId, "player-1", t2);
            Assert.Equal(PokerChangeTableStatus.Changed, result.Status);
        }

        await using var verify = factory.CreateDbContext();
        var conf = await verify.EventConfirmations.SingleAsync();
        Assert.Equal(20m, conf.PlatformFeeAmount); // 10% de 200
        Assert.Equal(10m, conf.PlayerFeeAmount);   // parceiro absorve metade
    }

    [Fact]
    public async Task SetPartnerFeeShareAsync_SysAdmin_SetsAndAudits()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var role = new IdentityRole { Name = "admin", NormalizedName = "ADMIN" };
        ctx.db.Roles.Add(role);
        var admin = TestDataFactory.CreateUserWithPixKey("admin-1", "Admin", "admin@test");
        ctx.db.Users.Add(admin);
        var group = TestDataFactory.CreateGroup("Parceiro", enablePaymentGateways: false);
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();
        ctx.db.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = role.Id });
        await ctx.db.SaveChangesAsync();

        var svc = new PlatformFeeWaiverService(
            ctx.factory,
            Options.Create(new FeeOptions()),
            new LogService(ctx.factory, NullLogger<LogService>.Instance));

        var result = await svc.SetPartnerFeeShareAsync(group.Id, admin.Id, 50m);

        Assert.True(result.Success);
        await using var verify = ctx.factory.CreateDbContext();
        Assert.Equal(50m, verify.Groups.Single(g => g.Id == group.Id).PartnerFeeSharePercent);
        var audit = verify.Logs.Single(l => l.EventType == AuditEvents.GroupPartnerFeeShareChanged);
        Assert.Equal(admin.Id, audit.UserId);
    }

    [Fact]
    public async Task SetPartnerFeeShareAsync_NonAdmin_Refused()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var user = TestDataFactory.CreateUserWithPixKey("user-1", "User", "u@test");
        ctx.db.Users.Add(user);
        var group = TestDataFactory.CreateGroup("G", enablePaymentGateways: false);
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var svc = new PlatformFeeWaiverService(
            ctx.factory,
            Options.Create(new FeeOptions()),
            new LogService(ctx.factory, NullLogger<LogService>.Instance));

        var result = await svc.SetPartnerFeeShareAsync(group.Id, user.Id, 50m);

        Assert.False(result.Success);
        Assert.Equal(PlatformFeeWaiverError.NotAuthorized, result.Error);
        await using var verify = ctx.factory.CreateDbContext();
        Assert.Equal(0m, verify.Groups.Single().PartnerFeeSharePercent);
    }

    [Fact]
    public async Task SetPartnerFeeShareAsync_OutOfRange_Refused()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var svc = new PlatformFeeWaiverService(
            ctx.factory,
            Options.Create(new FeeOptions()),
            new LogService(ctx.factory, NullLogger<LogService>.Instance));

        Assert.Equal(PlatformFeeWaiverError.InvalidPercent,
            (await svc.SetPartnerFeeShareAsync(1, "admin-1", -1m)).Error);
        Assert.Equal(PlatformFeeWaiverError.InvalidPercent,
            (await svc.SetPartnerFeeShareAsync(1, "admin-1", 101m)).Error);
    }
}
