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
/// C39-B F8 — o cash cobra por mesa: `EventPriceOption` criadas no
/// `PokerCreateService`, % de cada mesa validado na faixa do FeeOptions,
/// Pix exigido quando alguma mesa tem preco, `Event.Price` segue null e
/// CashMin/CashMax viram informativos derivados das mesas.
/// </summary>
public class C39CashTableCreateTests
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
        var factory = TestDbContextFactory.CreateInMemoryFactory($"poker-cash-{Guid.NewGuid()}");
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

    private static async Task<Group> SeedGroupAsync(
        IDbContextFactory<AppDbContext> factory, string adminId, string? pixKey = "admin@pix.com")
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = adminId, UserName = "Admin", PixKey = pixKey });
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

    private static PokerCreateFormData CashForm(params CashTableInput[] tables) => new()
    {
        EventType = PokerEventType.CashGame,
        Name = "Cash da Galera",
        PokerHouseName = "Clube",
        City = "Muzambinho",
        StateCode = "MG",
        MaxPlayers = 0,
        CashTables = tables.ToList(),
    };

    [Fact]
    public async Task SaveAsync_CashWithTables_CreatesPriceOptions()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(CashForm(
            new CashTableInput("Mesa 1/2", 200m, 5m),
            new CashTableInput("Mesa 2/5", 500m, 10m)), group);

        Assert.True(result.Success, result.Error);
        await using var db = factory.CreateDbContext();
        var ev = await db.Events.Include(e => e.PriceOptions).SingleAsync();
        Assert.Null(ev.Price); // cash cobra por mesa, nao por Event.Price
        Assert.Equal(2, ev.PriceOptions.Count);
        var t1 = ev.PriceOptions.OrderBy(o => o.SortOrder).First();
        Assert.Equal("Mesa 1/2", t1.Label);
        Assert.Equal(200m, t1.Price);
        Assert.Equal(5m, t1.PlatformFeePercent);
        Assert.True(t1.IsActive);
        // CashMin/CashMax derivados das mesas (informativos)
        Assert.Equal(200m, ev.CashMinBuyIn);
        Assert.Equal(500m, ev.CashMaxBuyIn);
    }

    [Fact]
    public async Task SaveAsync_CashTableFeeOutOfRange_Refused()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var low = await svc.SaveAsync(CashForm(new CashTableInput("M1", 200m, 1m)), group);
        var high = await svc.SaveAsync(CashForm(new CashTableInput("M1", 200m, 50m)), group);

        Assert.False(low.Success);
        Assert.False(high.Success);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.Events.CountAsync());
    }

    [Fact]
    public async Task SaveAsync_PricedTableWithoutPix_Refused()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1", pixKey: null);

        var result = await svc.SaveAsync(CashForm(new CashTableInput("M1", 200m, 5m)), group);

        Assert.False(result.Success);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.Events.CountAsync());
    }

    [Fact]
    public async Task SaveAsync_FreeCashWithoutPix_Allowed()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1", pixKey: null);

        var noTables = await svc.SaveAsync(CashForm(), group);
        var freeTableForm = CashForm(new CashTableInput("Gratis", 0m, 5m));
        freeTableForm.Time = new TimeOnly(22, 0);
        var freeTable = await svc.SaveAsync(freeTableForm, group);

        Assert.True(noTables.Success, noTables.Error);
        Assert.True(freeTable.Success, freeTable.Error);
    }

    [Fact]
    public async Task SaveAsync_TableWithoutLabel_Refused()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(CashForm(new CashTableInput("  ", 200m, 5m)), group);

        Assert.False(result.Success);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.Events.CountAsync());
    }
}
