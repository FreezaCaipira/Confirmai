using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Poker;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C38 F5 — a mesma regra do Pix do futsal vale para o poker: evento com
/// buy-in/valor > 0 exige recebedor Pix do grupo (ou gateway ligado), checado
/// no service. O jogador nao pode descobrir a falta de Pix na tela de pagar.
/// </summary>
public class PokerCreatePixPrerequisiteTests
{
    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly string _userId;
        public TestAuthStateProvider(string userId) => _userId = userId;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId) }, "Test"))));
    }

    private static (IDbContextFactory<AppDbContext> factory, PokerCreateService svc) Setup(string userId)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"poker-pix-{Guid.NewGuid()}");
        var collisionService = new EventCollisionService(factory);
        var svc = new PokerCreateService(
            factory,
            new TestAuthStateProvider(userId),
            collisionService,
            new UiTextService(new LanguagePreferenceService()));
        return (factory, svc);
    }

    private static async Task<Group> SeedGroupAsync(
        IDbContextFactory<AppDbContext> factory, string adminId,
        string? adminPixKey = null, bool gateways = false)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = adminId, UserName = "Admin", PixKey = adminPixKey });
        var group = new Group
        {
            Name = "Poker Group",
            Sport = Sport.Poker,
            CreatedByUserId = adminId,
            InviteCode = "XYZ789",
            EnablePaymentGateways = gateways,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { UserId = adminId, GroupId = group.Id, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();
        return group;
    }

    private static PokerCreateFormData Form(
        PokerEventType type = PokerEventType.Tournament,
        decimal buyIn = 0, decimal cashMin = 0) => new()
    {
        EventType = type,
        Name = "Liga",
        PokerHouseName = "Clube",
        City = "Muzambinho",
        StateCode = "MG",
        BuyInAmount = buyIn,
        CashMinBuyIn = cashMin,
        CashMaxBuyIn = cashMin > 0 ? cashMin * 10 : 0,
        MaxPlayers = 9,
    };

    [Fact]
    public async Task SaveAsync_TournamentWithBuyIn_GroupWithoutPix_Refused()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(buyIn: 50), group);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SaveAsync_FreeTournament_GroupWithoutPix_Succeeds()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(buyIn: 0), group);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task SaveAsync_CashGameWithMinBuyIn_GroupWithoutPix_Refused()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(PokerEventType.CashGame, cashMin: 100), group);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SaveAsync_HomeGame_GroupWithoutPix_Succeeds()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1");

        var result = await svc.SaveAsync(Form(PokerEventType.HomeGame), group);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task SaveAsync_PricedTournament_AdminWithPix_Succeeds()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1", adminPixKey: "admin@pix.com");

        var result = await svc.SaveAsync(Form(buyIn: 50), group);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task SaveAsync_PricedTournament_GatewaysEnabled_Succeeds()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1", gateways: true);

        var result = await svc.SaveAsync(Form(buyIn: 50), group);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task SaveAsync_CoAdminWithoutPix_OtherAdminHasPix_Succeeds()
    {
        var (factory, svc) = Setup("admin-sem-pix");
        var group = await SeedGroupAsync(factory, "admin-sem-pix");
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new ApplicationUser { Id = "admin-com-pix", UserName = "Admin2", PixKey = "11999998888" });
            db.GroupMembers.Add(new GroupMember { UserId = "admin-com-pix", GroupId = group.Id, Role = GroupMemberRole.Admin });
            await db.SaveChangesAsync();
        }

        var result = await svc.SaveAsync(Form(buyIn: 50), group);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task SaveAsync_InlineGroup_PricedEvent_CreatorWithoutPix_DoesNotPersistGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin" });
            await db.SaveChangesAsync();
        }

        var result = await svc.SaveAsync(Form(buyIn: 50), preselectedGroup: null);

        Assert.False(result.Success);
        await using var check = factory.CreateDbContext();
        Assert.Empty(await check.Groups.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_ReportsGroupHasPixKey()
    {
        var (factory, svc) = Setup("admin-1");
        var group = await SeedGroupAsync(factory, "admin-1", adminPixKey: "admin@pix.com");

        var withPix = await svc.InitializeAsync(group.Id, "", "", "");
        Assert.True(withPix.GroupHasPixKey);

        var (factory2, svc2) = Setup("admin-2");
        var group2 = await SeedGroupAsync(factory2, "admin-2");
        var noPix = await svc2.InitializeAsync(group2.Id, "", "", "");
        Assert.False(noPix.GroupHasPixKey);
    }
}
