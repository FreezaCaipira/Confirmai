using Confirmai.Services.Events;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Enums;

namespace Confirmai.Tests;

public class EventCollisionServiceTests
{
    [Fact]
    public async Task HasGroupTimeCollisionAsync_ReturnsTrue_WhenSameGroupAndTimeExists()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var service = new EventCollisionService(factory);

        var group = new Group { Name = "Racha", Sport = Sport.Futsal, CreatedByUserId = "u1" };
        db.Groups.Add(group);
        db.SaveChanges();

        var startsAt = DateTime.SpecifyKind(new DateTime(2026, 5, 27, 20, 0, 0), DateTimeKind.Utc);
        db.Events.Add(new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra",
            StartsAt = startsAt,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        });
        db.SaveChanges();

        var collision = await service.HasGroupTimeCollisionAsync(group.Id, startsAt);

        Assert.True(collision);
    }

    [Fact]
    public async Task HasGroupTimeCollisionAsync_IgnoresExcludedEventId()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var service = new EventCollisionService(factory);

        var group = new Group { Name = "Racha", Sport = Sport.Futsal, CreatedByUserId = "u1" };
        db.Groups.Add(group);
        db.SaveChanges();

        var startsAt = DateTime.SpecifyKind(new DateTime(2026, 5, 27, 20, 0, 0), DateTimeKind.Utc);
        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra",
            StartsAt = startsAt,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var collision = await service.HasGroupTimeCollisionAsync(group.Id, startsAt, ev.Id);

        Assert.False(collision);
    }

    [Fact]
    public async Task HasGroupTimeCollisionAsync_ReturnsFalse_WhenDifferentGroupUsesSameTime()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var service = new EventCollisionService(factory);

        var group1 = new Group { Name = "G1", Sport = Sport.Futsal, CreatedByUserId = "u1" };
        var group2 = new Group { Name = "G2", Sport = Sport.Futsal, CreatedByUserId = "u2" };
        db.Groups.AddRange(group1, group2);
        db.SaveChanges();

        var startsAt = DateTime.SpecifyKind(new DateTime(2026, 5, 27, 20, 0, 0), DateTimeKind.Utc);
        db.Events.Add(new Event
        {
            GroupId = group1.Id,
            Sport = Sport.Futsal,
            Location = "Quadra",
            StartsAt = startsAt,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        });
        db.SaveChanges();

        var collision = await service.HasGroupTimeCollisionAsync(group2.Id, startsAt);

        Assert.False(collision);
    }

    [Fact]
    public void SaveChanges_Throws_WhenAddingTwoEventsSameGroupAndTime()
    {
        using var db = TestDataFactory.CreateDbContext();

        var group = new Group { Name = "Racha", Sport = Sport.Futsal, CreatedByUserId = "u1" };
        db.Groups.Add(group);
        db.SaveChanges();

        var startsAt = DateTime.SpecifyKind(new DateTime(2026, 5, 27, 20, 0, 0), DateTimeKind.Utc);
        db.Events.Add(new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra A",
            StartsAt = startsAt,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        });
        db.Events.Add(new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra B",
            StartsAt = startsAt,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        });

        var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("Já existe uma partida", ex.Message);
    }

    [Fact]
    public void SaveChanges_Throws_WhenEditingEventToCollidingSlot()
    {
        using var db = TestDataFactory.CreateDbContext();

        var group = new Group { Name = "Racha", Sport = Sport.Futsal, CreatedByUserId = "u1" };
        db.Groups.Add(group);
        db.SaveChanges();

        var startsAtA = DateTime.SpecifyKind(new DateTime(2026, 5, 27, 20, 0, 0), DateTimeKind.Utc);
        var startsAtB = DateTime.SpecifyKind(new DateTime(2026, 5, 28, 20, 0, 0), DateTimeKind.Utc);

        var evA = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra A",
            StartsAt = startsAtA,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        };

        var evB = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra B",
            StartsAt = startsAtB,
            MaxPlayers = 10,
            CreatedByUserId = "u1",
            IsActive = true,
        };

        db.Events.AddRange(evA, evB);
        db.SaveChanges();

        evB.StartsAt = startsAtA;

        var ex = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("Já existe uma partida", ex.Message);
    }
}
