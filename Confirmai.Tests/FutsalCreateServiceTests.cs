using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

public class FutsalCreateServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, FutsalCreateService svc) Setup(string? userId = null)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"futsal-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var collisionService = new EventCollisionService(factory);
        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = userId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")
            : new ClaimsIdentity();
        authMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));
        var svc = new FutsalCreateService(factory, authMock.Object, collisionService, logService, new UiTextService(new LanguagePreferenceService()));
        return (factory, svc);
    }

    private static async Task SeedVenueAndGroupAsync(IDbContextFactory<AppDbContext> factory, string userId)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "Admin", PixKey = "admin@pix" });
        db.Venues.Add(new Venue { Id = 1, Name = "Quadra Test", City = "SP", StateCode = "SP", Address = "Rua 1", IsActive = true });
        var group = new Group { Id = 1, Name = "Grupo Test", Sport = Sport.Futsal, CreatedByUserId = userId, InviteCode = "ABC123" };
        db.Groups.Add(group);
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
        var data = await svc.InitializeAsync(null);
        Assert.Null(data.PreselectedGroup);
        Assert.Empty(data.Venues);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsGroupAndVenues_WhenAdminOfGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedVenueAndGroupAsync(factory, "admin-1");

        var data = await svc.InitializeAsync(1);

        Assert.NotNull(data.PreselectedGroup);
        Assert.NotEmpty(data.Venues);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsEmpty_WhenNotAdminOfGroup()
    {
        var (factory, svc) = Setup("regular-user");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "regular-user", UserName = "Regular" });
        db.Groups.Add(new Group { Id = 1, Name = "Group", Sport = Sport.Futsal, CreatedByUserId = "other" });
        await db.SaveChangesAsync();

        var data = await svc.InitializeAsync(1);

        Assert.Null(data.PreselectedGroup);
    }

    [Fact]
    public async Task SaveAsync_ReturnsError_WhenNotAuthenticated()
    {
        var (factory, svc) = Setup();
        var form = new CreateMatchFormData { VenueId = 1 };
        var result = await svc.SaveAsync(form, new List<Venue>(), null, "futsal");
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SaveAsync_ReturnsError_WhenVenueNotFound()
    {
        var (factory, svc) = Setup("admin-1");
        var form = new CreateMatchFormData { VenueId = 999 };
        var result = await svc.SaveAsync(form, new List<Venue>(), null, "futsal");
        Assert.False(result.Success);
        Assert.Contains("Quadra", result.Error!);
    }

    [Fact]
    public async Task SaveAsync_CreatesEvent_WithPreselectedGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedVenueAndGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();
        var venue = await db.Venues.FirstAsync();

        var form = new CreateMatchFormData
        {
            VenueId = venue.Id,
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 10,
            Price = 10m
        };

        var result = await svc.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewGroup_WhenNoPreselectedGroup()
    {
        var (factory, svc) = Setup("admin-1");
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin", PixKey = "admin@pix" });
        var venue = new Venue { Id = 1, Name = "Quadra", City = "SP", StateCode = "SP", Address = "Rua 1", IsActive = true };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            GroupName = "New Group",
            VenueId = 1,
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 10,
            Price = 10m
        };

        var result = await svc.SaveAsync(form, new List<Venue> { venue }, null, "futsal");

        Assert.True(result.Success);
        Assert.NotNull(result.EventId);
    }

    [Fact]
    public async Task SaveAsync_ReturnsCollisionError_WhenTimeConflictExists()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedVenueAndGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();
        var venue = await db.Venues.FirstAsync();
        var startsAt = DateTime.SpecifyKind(new DateOnly(2026, 8, 15).ToDateTime(new TimeOnly(20, 0)), DateTimeKind.Utc);
        db.Events.Add(new Event { GroupId = group.Id, Sport = Sport.Futsal, StartsAt = startsAt, Location = "Loc", MaxPlayers = 10 });
        await db.SaveChangesAsync();

        var form = new CreateMatchFormData
        {
            VenueId = venue.Id,
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 10,
            Price = 10m
        };

        var result = await svc.SaveAsync(form, new List<Venue> { venue }, group, "futsal");

        Assert.False(result.Success);
        Assert.NotNull(result.CollisionHref);
    }

    [Fact]
    public async Task SaveAsync_SetsPlayersPerSide_BasedOnSubFormat()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedVenueAndGroupAsync(factory, "admin-1");
        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();
        var venue = await db.Venues.FirstAsync();

        var form = new CreateMatchFormData
        {
            VenueId = venue.Id,
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 14,
            Price = 10m
        };

        var result = await svc.SaveAsync(form, new List<Venue> { venue }, group, "society");

        Assert.True(result.Success);
        await using var db2 = factory.CreateDbContext();
        var ev = await db2.Events.FindAsync(result.EventId);
        Assert.Equal(7, ev!.PlayersPerSide);
    }
}
