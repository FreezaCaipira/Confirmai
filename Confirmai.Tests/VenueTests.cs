using Confirmai.Models;
using Confirmai.Enums;

namespace Confirmai.Tests;

public class VenueTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var venue = new Venue { Id = 1 };
        Assert.Equal(1, venue.Id);
    }

    [Fact]
    public void Name_CanBeSetAndGet()
    {
        var venue = new Venue { Name = "Test Venue" };
        Assert.Equal("Test Venue", venue.Name);
    }

    [Fact]
    public void Name_DefaultsToEmptyString()
    {
        var venue = new Venue();
        Assert.Equal(string.Empty, venue.Name);
    }

    [Fact]
    public void Type_CanBeSetAndGet()
    {
        var venue = new Venue { Type = VenueType.Quadra };
        Assert.Equal(VenueType.Quadra, venue.Type);
    }

    [Fact]
    public void Address_CanBeSetAndGet()
    {
        var venue = new Venue { Address = "123 Main St" };
        Assert.Equal("123 Main St", venue.Address);
    }

    [Fact]
    public void Address_DefaultsToEmptyString()
    {
        var venue = new Venue();
        Assert.Equal(string.Empty, venue.Address);
    }

    [Fact]
    public void City_CanBeSetAndGet()
    {
        var venue = new Venue { City = "São Paulo" };
        Assert.Equal("São Paulo", venue.City);
    }

    [Fact]
    public void City_DefaultsToEmptyString()
    {
        var venue = new Venue();
        Assert.Equal(string.Empty, venue.City);
    }

    [Fact]
    public void StateCode_CanBeSetAndGet()
    {
        var venue = new Venue { StateCode = "SP" };
        Assert.Equal("SP", venue.StateCode);
    }

    [Fact]
    public void StateCode_DefaultsToEmptyString()
    {
        var venue = new Venue();
        Assert.Equal(string.Empty, venue.StateCode);
    }

    [Fact]
    public void IsActive_CanBeSetAndGet()
    {
        var venue = new Venue { IsActive = false };
        Assert.False(venue.IsActive);
    }

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        var venue = new Venue();
        Assert.True(venue.IsActive);
    }

    [Fact]
    public void CreatedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var venue = new Venue { CreatedAt = now };
        Assert.Equal(now, venue.CreatedAt);
    }

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var venue = new Venue();
        var after = DateTime.UtcNow;
        Assert.InRange(venue.CreatedAt, before, after);
    }

    [Fact]
    public void CreatedByUserId_CanBeSetAndGet()
    {
        var venue = new Venue { CreatedByUserId = "user-123" };
        Assert.Equal("user-123", venue.CreatedByUserId);
    }

    [Fact]
    public void CreatedByUserId_CanBeNull()
    {
        var venue = new Venue { CreatedByUserId = null };
        Assert.Null(venue.CreatedByUserId);
    }

    [Fact]
    public void VenueAdminUserId_CanBeSetAndGet()
    {
        var venue = new Venue { VenueAdminUserId = "admin-123" };
        Assert.Equal("admin-123", venue.VenueAdminUserId);
    }

    [Fact]
    public void VenueAdminUserId_CanBeNull()
    {
        var venue = new Venue { VenueAdminUserId = null };
        Assert.Null(venue.VenueAdminUserId);
    }

    [Fact]
    public void Events_DefaultsToEmptyList()
    {
        var venue = new Venue();
        Assert.NotNull(venue.Events);
        Assert.Empty(venue.Events);
    }

    [Fact]
    public void Schedules_DefaultsToEmptyList()
    {
        var venue = new Venue();
        Assert.NotNull(venue.Schedules);
        Assert.Empty(venue.Schedules);
    }
}
