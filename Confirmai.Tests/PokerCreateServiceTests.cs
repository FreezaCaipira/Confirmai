using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Events;
using Confirmai.Services.Poker;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

public class PokerCreateServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, PokerCreateService svc) Setup(string? userId = null)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"poker-{Guid.NewGuid()}");
        var collisionService = new EventCollisionService(factory);
        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = userId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")
            : new ClaimsIdentity();
        authMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));
        var svc = new PokerCreateService(factory, authMock.Object, collisionService);
        return (factory, svc);
    }

    private static async Task SeedGroupAsync(IDbContextFactory<AppDbContext> factory, string userId)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "Admin" });
        db.Groups.Add(new Group { Id = 1, Name = "Poker Group", Sport = Sport.Poker, CreatedByUserId = userId, InviteCode = "XYZ789" });
        db.GroupMembers.Add(new GroupMember { UserId = userId, GroupId = 1, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ReturnsUserId_WhenAuthenticated()
    {
        var (factory, svc) = Setup("user-1");
        var userId = await svc.GetCurrentUserIdAsync();
        Assert.Equal("user-1", userId);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ReturnsNull_WhenNotAuthenticated()
    {
        var (factory, svc) = Setup();
        var userId = await svc.GetCurrentUserIdAsync();
        Assert.Null(userId);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsEmpty_WhenNoGroupId()
    {
        var (factory, svc) = Setup("user-1");
        var data = await svc.InitializeAsync(null, "", "", "");
        Assert.Null(data.PreselectedGroup);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsGroup_WhenAdmin()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedGroupAsync(factory, "admin-1");

        var data = await svc.InitializeAsync(1, "", "", "");

        Assert.NotNull(data.PreselectedGroup);
    }

    [Fact]
    public async Task SaveAsync_ReturnsError_WhenNotAuthenticated()
    {
        var (factory, svc) = Setup();
        var form = new PokerCreateFormData { Name = "Test", MaxPlayers = 10 };
        var result = await svc.SaveAsync(form, null);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SaveAsync_CreatesTournamentEvent_WithPreselectedGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();

        var form = new PokerCreateFormData
        {
            EventType = PokerEventType.Tournament,
            Name = "Friday Tournament",
            PokerHouseName = "Poker House",
            Address = "Rua 1",
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(19, 0),
            MaxPlayers = 30,
            BuyInAmount = 100m,
            GTD = 500m,
            Modality = PokerModality.Vanilla
        };

        var result = await svc.SaveAsync(form, group);

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
        await using var db2 = factory.CreateDbContext();
        var ev = await db2.Events.FindAsync(result.EventId);
        Assert.Equal(PokerEventType.Tournament, ev!.PokerEventType);
        Assert.Equal(100m, ev.BuyInAmount);
    }

    [Fact]
    public async Task SaveAsync_CreatesCashGameEvent_WithPreselectedGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();

        var form = new PokerCreateFormData
        {
            EventType = PokerEventType.CashGame,
            Name = "Cash Game",
            Address = "Rua 1",
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 20,
            CashMinBuyIn = 50m,
            CashMaxBuyIn = 500m,
            CashIncludes = "Cerveja e pizza"
        };

        var result = await svc.SaveAsync(form, group);

        Assert.True(result.Success);
        await using var db2 = factory.CreateDbContext();
        var ev = await db2.Events.FindAsync(result.EventId);
        Assert.Equal(PokerEventType.CashGame, ev!.PokerEventType);
        Assert.Equal(50m, ev.CashMinBuyIn);
    }

    [Fact]
    public async Task SaveAsync_CreatesHomeGameEvent_WithHomeGameCode()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();

        var form = new PokerCreateFormData
        {
            EventType = PokerEventType.HomeGame,
            Name = "Home Game",
            Address = "Rua 1",
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 10
        };

        var result = await svc.SaveAsync(form, group);

        Assert.True(result.Success);
        await using var db2 = factory.CreateDbContext();
        var ev = await db2.Events.FindAsync(result.EventId);
        Assert.Equal(PokerEventType.HomeGame, ev!.PokerEventType);
        Assert.NotNull(ev.HomeGameCode);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewGroup_WhenNoPreselectedGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin" });
        await db.SaveChangesAsync();

        var form = new PokerCreateFormData
        {
            Name = "New Poker Group",
            City = "SP",
            StateCode = "SP",
            Address = "Rua 1",
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(19, 0),
            MaxPlayers = 10
        };

        var result = await svc.SaveAsync(form, null);

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
    }

    [Fact]
    public async Task SaveAsync_ReturnsCollisionError_WhenTimeConflictExists()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();
        var startsAt = DateTime.SpecifyKind(new DateOnly(2026, 8, 15).ToDateTime(new TimeOnly(19, 0)), DateTimeKind.Utc);
        db.Events.Add(new Event { GroupId = group.Id, Sport = Sport.Poker, StartsAt = startsAt, Location = "Loc", MaxPlayers = 10 });
        await db.SaveChangesAsync();

        var form = new PokerCreateFormData
        {
            Name = "Test",
            Address = "Rua 1",
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(19, 0),
            MaxPlayers = 10
        };

        var result = await svc.SaveAsync(form, group);

        Assert.False(result.Success);
        Assert.NotNull(result.CollisionHref);
    }

    [Fact]
    public void GenerateHomeGameCode_ReturnsNonEmptyString()
    {
        var code = PokerCreateService.GenerateHomeGameCode();
        Assert.NotEmpty(code);
        Assert.Contains("-", code);
    }

    [Fact]
    public void GenerateHomeGameCode_ReturnsConsistentFormat()
    {
        for (int i = 0; i < 20; i++)
        {
            var code = PokerCreateService.GenerateHomeGameCode();
            Assert.Matches(@"^[A-Z]{2}[0-9]-[0-9]{3}$", code);
        }
    }
}
