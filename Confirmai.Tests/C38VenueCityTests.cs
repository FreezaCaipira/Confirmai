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

/// <summary>
/// C38 F4 — a lista de quadras e restrita a cidade/UF do grupo com
/// comparacao normalizada; SaveAsync nao confia no VenueId do cliente.
/// </summary>
public class C38VenueCityTests
{
    private static (IDbContextFactory<AppDbContext> factory, FutsalCreateService svc) Setup(string userId)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"futsal-c38-{Guid.NewGuid()}");
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test"))));
        var svc = new FutsalCreateService(
            factory,
            authMock.Object,
            new EventCollisionService(factory),
            new LogService(factory, NullLogger<LogService>.Instance),
            new UiTextService(new LanguagePreferenceService()));
        return (factory, svc);
    }

    private static async Task SeedAsync(
        IDbContextFactory<AppDbContext> factory,
        string userId,
        string groupCity,
        string groupState,
        params Venue[] venues)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "Admin", PixKey = "admin@pix" });
        var group = new Group
        {
            Id = 1, Name = "Grupo", Sport = Sport.Futsal,
            City = groupCity, StateCode = groupState,
            CreatedByUserId = userId, InviteCode = "ABC123",
        };
        db.Groups.Add(group);
        db.GroupMembers.Add(new GroupMember { UserId = userId, GroupId = 1, Role = GroupMemberRole.Admin });
        db.Venues.AddRange(venues);
        await db.SaveChangesAsync();
    }

    private static Venue Venue(int id, string name, string city, string state, bool active = true) =>
        new() { Id = id, Name = name, City = city, StateCode = state, Address = "Rua 1", IsActive = active };

    [Fact]
    public async Task Initialize_DoesNotList_VenueFromOtherCity()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedAsync(factory, "admin-1", "Muzambinho", "MG",
            Venue(1, "Quadra Muzambinho", "Muzambinho", "MG"),
            Venue(2, "Quadra Pouso Alegre", "Pouso Alegre", "MG"));

        var data = await svc.InitializeAsync(1);

        Assert.Single(data.Venues);
        Assert.Equal("Quadra Muzambinho", data.Venues[0].Name);
    }

    [Fact]
    public async Task Initialize_MatchesCity_IgnoringAccentAndCase()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedAsync(factory, "admin-1", "São Paulo", "SP",
            Venue(1, "Quadra Mooca", "sao paulo", "SP"),
            Venue(2, "Quadra Rio", "Rio de Janeiro", "RJ"));

        var data = await svc.InitializeAsync(1);

        Assert.Single(data.Venues);
        Assert.Equal("Quadra Mooca", data.Venues[0].Name);
    }

    [Fact]
    public async Task Initialize_CityWithoutVenue_ReturnsEmptyList()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedAsync(factory, "admin-1", "Guaranesia", "MG",
            Venue(1, "Quadra Muzambinho", "Muzambinho", "MG"));

        var data = await svc.InitializeAsync(1);

        Assert.Empty(data.Venues);
    }

    [Fact]
    public async Task Initialize_ForeignGroup_MatchesOnCityOnly()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedAsync(factory, "admin-1", "Lisboa", "EX",
            Venue(1, "Campo Lisboa", "lisboa", "EX"),
            Venue(2, "Quadra SP", "São Paulo", "SP"));

        var data = await svc.InitializeAsync(1);

        Assert.Single(data.Venues);
        Assert.Equal("Campo Lisboa", data.Venues[0].Name);
    }

    [Fact]
    public async Task SaveAsync_Rejects_VenueOutsideGroupCity()
    {
        var (factory, svc) = Setup("admin-1");
        await SeedAsync(factory, "admin-1", "Muzambinho", "MG",
            Venue(1, "Quadra Muzambinho", "Muzambinho", "MG"),
            Venue(2, "Quadra Pouso Alegre", "Pouso Alegre", "MG"));

        await using var db = factory.CreateDbContext();
        var group = await db.Groups.FirstAsync();
        var outsideVenue = await db.Venues.FirstAsync(v => v.Id == 2);
        var form = new CreateMatchFormData
        {
            VenueId = 2,
            Date = new DateOnly(2026, 8, 15),
            Time = new TimeOnly(20, 0),
            MaxPlayers = 10,
            Price = 0,
        };

        var result = await svc.SaveAsync(form, new List<Venue> { outsideVenue }, group, "futsal");

        Assert.False(result.Success);
        Assert.Equal(0, await db.Events.CountAsync());
    }

    [Fact]
    public void VenuesForGroup_KeepsCurrentVenue_ForLegacyEdit()
    {
        var group = new Group { City = "Muzambinho", StateCode = "MG" };
        var legacyVenue = Venue(2, "Quadra Pouso Alegre", "Pouso Alegre", "MG");
        var venues = new List<Venue>
        {
            Venue(1, "Quadra Muzambinho", "Muzambinho", "MG"),
            legacyVenue,
        };

        var filtered = CityNormalizer.VenuesForGroup(venues, group, alwaysInclude: legacyVenue);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, v => v.Id == 2);
    }

    [Fact]
    public void CityNormalizer_Normalizes_CaseAccentAndSpaces()
    {
        Assert.Equal(CityNormalizer.Normalize("São Paulo"), CityNormalizer.Normalize("  SAO  PAULO "));
        Assert.True(CityNormalizer.SameCity("Muzambinho", "MUZAMBINHO"));
        Assert.False(CityNormalizer.SameCity("Muzambinho", "Pouso Alegre"));
    }
}
