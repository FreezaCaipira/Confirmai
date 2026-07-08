using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;

namespace Confirmai.Tests;

public class GroupMetricsServiceTests
{
    private static Mock<IDbContextFactory<AppDbContext>> CreateFactoryMock(AppDbContext db)
    {
        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        factoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        return factoryMock;
    }

    [Fact]
    public async Task GetSnapshot_WithEventsAndMembers_CalculatesCorrectly()
    {
        var db = TestDataFactory.CreateDbContext();
        var group = new Group
        {
            Name = "Grupo Teste",
            Sport = Sport.Futsal,
            City = "Sao Paulo",
            StateCode = "SP",
            IsActive = true
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = $"user-{i}",
                Role = GroupMemberRole.Member
            });
        }

        var pastEvent1 = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(-10),
            MaxPlayers = 10,
            MaxGoalkeepers = 1,
            IsActive = true
        };
        var pastEvent2 = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(-5),
            MaxPlayers = 10,
            MaxGoalkeepers = 1,
            IsActive = true
        };
        var futureEvent = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(5),
            MaxPlayers = 10,
            MaxGoalkeepers = 1,
            IsActive = true
        };
        db.Events.AddRange(pastEvent1, pastEvent2, futureEvent);
        await db.SaveChangesAsync();

        for (int i = 0; i < 7; i++)
        {
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = pastEvent1.Id,
                UserId = $"user-{i}",
                PaymentStatus = i < 5 ? EventConfirmationPaymentStatus.Paid : EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new GroupMetricsService(factoryMock.Object);

        var snapshot = await service.GetSnapshotAsync(group.Id);

        Assert.Equal(5, snapshot.TotalMembers);
        Assert.Equal(3, snapshot.TotalEvents);
        Assert.Equal(1, snapshot.UpcomingEvents);
        Assert.Equal(7, snapshot.TotalConfirmations);
        Assert.Equal(5, snapshot.PaidConfirmations);
        Assert.True(snapshot.PaymentCompletionRate > 0);
    }

    [Fact]
    public async Task GetSnapshot_GroupNotFound_ReturnsEmptySnapshot()
    {
        var db = TestDataFactory.CreateDbContext();
        var factoryMock = CreateFactoryMock(db);
        var service = new GroupMetricsService(factoryMock.Object);

        var snapshot = await service.GetSnapshotAsync(999);

        Assert.Equal(0, snapshot.TotalMembers);
        Assert.Equal(0, snapshot.TotalEvents);
        Assert.Equal(0, snapshot.UpcomingEvents);
    }

    [Fact]
    public async Task GetSnapshot_EmptyGroup_ReturnsZeros()
    {
        var db = TestDataFactory.CreateDbContext();
        var group = new Group
        {
            Name = "Grupo Vazio",
            Sport = Sport.Futsal,
            City = "BH",
            StateCode = "MG",
            IsActive = true
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new GroupMetricsService(factoryMock.Object);

        var snapshot = await service.GetSnapshotAsync(group.Id);

        Assert.Equal(0, snapshot.TotalMembers);
        Assert.Equal(0, snapshot.TotalEvents);
        Assert.Equal(0, snapshot.UpcomingEvents);
        Assert.Equal(0, snapshot.TotalConfirmations);
        Assert.Equal(0, snapshot.PaidConfirmations);
        Assert.Equal(0, snapshot.PaymentCompletionRate);
    }

    [Fact]
    public async Task GetMultipleSnapshots_ReturnsAllGroups()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var group1 = new Group { Name = "G1", Sport = Sport.Futsal, City = "A", StateCode = "SP", IsActive = true };
        var group2 = new Group { Name = "G2", Sport = Sport.Futsal, City = "B", StateCode = "MG", IsActive = true };
        db.Groups.AddRange(group1, group2);
        await db.SaveChangesAsync();
        await db.DisposeAsync();

        var service = new GroupMetricsService(factory);

        var snapshots = await service.GetMultipleSnapshotsAsync(new[] { group1.Id, group2.Id });

        Assert.Equal(2, snapshots.Count);
        Assert.True(snapshots.ContainsKey(group1.Id));
        Assert.True(snapshots.ContainsKey(group2.Id));
    }

    [Fact]
    public async Task GetSnapshot_PaymentRate_CalculatesCorrectly()
    {
        var db = TestDataFactory.CreateDbContext();
        var group = new Group
        {
            Name = "Grupo Pag",
            Sport = Sport.Futsal,
            City = "SP",
            StateCode = "SP",
            IsActive = true
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var evt = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            MaxPlayers = 20,
            MaxGoalkeepers = 2,
            IsActive = true
        };
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        for (int i = 0; i < 10; i++)
        {
            db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = evt.Id,
                UserId = $"user-{i}",
                PaymentStatus = i < 7 ? EventConfirmationPaymentStatus.Paid : EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new GroupMetricsService(factoryMock.Object);

        var snapshot = await service.GetSnapshotAsync(group.Id);

        Assert.Equal(10, snapshot.TotalConfirmations);
        Assert.Equal(7, snapshot.PaidConfirmations);
        Assert.Equal(70m, snapshot.PaymentCompletionRate);
    }
}
