using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Payment;
using Confirmai.Services.Poker;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C39-A F3 — o torneio cobra pelo app: Event.Price = BuyInAmount e
/// PlatformFeePercent validado na faixa do FeeOptions no service.
/// </summary>
public class C39TournamentChargeTests
{
    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly string _userId;
        public TestAuthStateProvider(string userId) => _userId = userId;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId) }, "Test"))));
    }

    private static (IDbContextFactory<AppDbContext> factory, PokerCreateService svc) Setup(
        string userId, decimal feeMin = 5m, decimal feeMax = 10m)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"poker-charge-{Guid.NewGuid()}");
        var policy = new PlatformFeePolicy(Options.Create(new FeeOptions
        {
            ManualPlatformFeeFixed = 0.75m,
            PokerFeePercentMin = feeMin,
            PokerFeePercentMax = feeMax,
        }));
        var svc = new PokerCreateService(
            factory,
            new TestAuthStateProvider(userId),
            new EventCollisionService(factory),
            new UiTextService(new Confirmai.Services.User.LanguagePreferenceService()),
            policy);
        return (factory, svc);
    }

    private static async Task<Group> SeedGroupAsync(IDbContextFactory<AppDbContext> factory, string adminId)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = adminId, UserName = "Admin", PixKey = "admin@pix.com" });
        var group = new Group
        {
            Name = "Poker Group", Sport = Sport.Poker,
            CreatedByUserId = adminId, InviteCode = "XYZ789",
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { UserId = adminId, GroupId = group.Id, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();
        return group;
    }

    private static PokerCreateFormData Form(
        PokerEventType type = PokerEventType.Tournament,
        decimal buyIn = 0, decimal feePercent = 5m) => new()
    {
        EventType = type,
        Name = "Liga",
        PokerHouseName = "Clube",
        City = "Muzambinho",
        StateCode = "MG",
        BuyInAmount = buyIn,
        PlatformFeePercent = feePercent,
        MaxPlayers = 9,
    };

    [Fact]
    public async Task SaveAsync_Tournament_SetsPrice_FromBuyIn()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(buyIn: 200, feePercent: 5m), group);

        Assert.True(result.Success, result.Error);
        await using var db = factory.CreateDbContext();
        var ev = await db.Events.SingleAsync();
        Assert.Equal(200m, ev.Price);
        Assert.Equal(5m, ev.PlatformFeePercent);
    }

    [Fact]
    public async Task SaveAsync_Tournament_FeePercentOutOfRange_Refused()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var low = await svc.SaveAsync(Form(buyIn: 200, feePercent: 1m), group);
        var high = await svc.SaveAsync(Form(buyIn: 200, feePercent: 50m), group);

        Assert.False(low.Success);
        Assert.False(high.Success);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.Events.CountAsync());
    }

    [Fact]
    public async Task SaveAsync_FreeTournament_PriceStaysNull()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(buyIn: 0, feePercent: 5m), group);

        Assert.True(result.Success, result.Error);
        await using var db = factory.CreateDbContext();
        var ev = await db.Events.SingleAsync();
        Assert.Null(ev.Price);
        Assert.Null(ev.PlatformFeePercent);
    }

    [Fact]
    public async Task SaveAsync_CashGame_PriceStaysNull()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var form = Form(PokerEventType.CashGame);
        form.CashMinBuyIn = 100;
        form.CashMaxBuyIn = 500;
        var result = await svc.SaveAsync(form, group);

        Assert.True(result.Success, result.Error);
        await using var db = factory.CreateDbContext();
        var ev = await db.Events.SingleAsync();
        Assert.Null(ev.Price); // cash cobra por mesa (C39-B), nao por Event.Price
    }

    [Fact]
    public async Task SaveAsync_HomeGame_PriceStaysNull()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(PokerEventType.HomeGame), group);

        Assert.True(result.Success, result.Error);
        await using var db = factory.CreateDbContext();
        Assert.Null((await db.Events.SingleAsync()).Price);
    }
}
