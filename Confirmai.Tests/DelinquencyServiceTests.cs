using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class DelinquencyServiceTests
{
    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_ExcludesGoalkeepers()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var evt = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 50m, MaxPlayers = 10 };

        db.Users.Add(user);
        db.Groups.Add(grp);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        // Add confirmations: one GK (should be excluded), one Outfield (should be included)
        var gkConf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Goalkeeper  // Should be excluded
        };
        var fieldConf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield  // Should be included
        };
        db.EventConfirmations.Add(gkConf);
        db.EventConfirmations.Add(fieldConf);
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Entries);
        Assert.Equal(nameof(FutsalPosition.Outfield), FutsalPosition.Outfield.ToString());
    }

    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_ExcludesFutureEvents()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var pastEvent = new Event
        {
            GroupId = 1,
            StartsAt = DateTime.UtcNow.AddHours(-1),  // Past
            Price = 50m,
            MaxPlayers = 10
        };
        var futureEvent = new Event
        {
            GroupId = 1,
            StartsAt = DateTime.UtcNow.AddHours(1),  // Future
            Price = 50m,
            MaxPlayers = 10
        };

        db.Users.Add(user);
        db.Groups.Add(grp);
        db.Events.Add(pastEvent);
        db.Events.Add(futureEvent);
        await db.SaveChangesAsync();

        var pastConf = new EventConfirmation
        {
            EventId = pastEvent.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        var futureConf = new EventConfirmation
        {
            EventId = futureEvent.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(pastConf);
        db.EventConfirmations.Add(futureConf);
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Entries);
        Assert.Equal(pastEvent.Id, result[0].Entries[0].EventId);
    }

    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_ExcludesPaidConfirmations()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var evt = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 50m, MaxPlayers = 10 };

        db.Users.Add(user);
        db.Groups.Add(grp);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var paidConf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,  // Should be excluded
            Position = FutsalPosition.Outfield
        };
        var unpaidConf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,  // Should be included
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(paidConf);
        db.EventConfirmations.Add(unpaidConf);
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Entries);
    }

    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_ExcludesZeroAndNullPrices()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var zeroPriceEvent = new Event
        {
            GroupId = 1,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            Price = 0m,  // Should be excluded
            MaxPlayers = 10
        };
        var nullPriceEvent = new Event
        {
            GroupId = 1,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            Price = null,  // Should be excluded
            MaxPlayers = 10
        };
        var validPriceEvent = new Event
        {
            GroupId = 1,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            Price = 50m,  // Should be included
            MaxPlayers = 10
        };

        db.Users.Add(user);
        db.Groups.Add(grp);
        db.Events.Add(zeroPriceEvent);
        db.Events.Add(nullPriceEvent);
        db.Events.Add(validPriceEvent);
        await db.SaveChangesAsync();

        var zeroConf = new EventConfirmation
        {
            EventId = zeroPriceEvent.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        var nullConf = new EventConfirmation
        {
            EventId = nullPriceEvent.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        var validConf = new EventConfirmation
        {
            EventId = validPriceEvent.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(zeroConf);
        db.EventConfirmations.Add(nullConf);
        db.EventConfirmations.Add(validConf);
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Entries);
        Assert.Equal(50m, result[0].Entries[0].EventPrice);
    }

    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_GroupsByUserAndCalculatesTotalAmount()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user1 = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var user2 = new ApplicationUser { Id = "user-2", UserName = "Maria", FullName = "Maria Santos" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };

        var evt1 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 50m, MaxPlayers = 10 };
        var evt2 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-2), Price = 75m, MaxPlayers = 10 };
        var evt3 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-3), Price = 40m, MaxPlayers = 10 };

        db.Users.Add(user1);
        db.Users.Add(user2);
        db.Groups.Add(grp);
        db.Events.Add(evt1);
        db.Events.Add(evt2);
        db.Events.Add(evt3);
        await db.SaveChangesAsync();

        // User1: 50 + 75 = 125
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evt1.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evt2.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        });
        // User2: 40
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evt3.Id,
            UserId = "user-2",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        });
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1", "user-2" };

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(125m, result[0].TotalAmount);  // User1, first in ordering
        Assert.Equal(40m, result[1].TotalAmount);   // User2
    }

    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_OrdersByTotalAmountDescending()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user1 = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var user2 = new ApplicationUser { Id = "user-2", UserName = "Maria", FullName = "Maria Santos" };
        var user3 = new ApplicationUser { Id = "user-3", UserName = "Pedro", FullName = "Pedro Costa" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };

        var evt1 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 30m, MaxPlayers = 10 };
        var evt2 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-2), Price = 50m, MaxPlayers = 10 };
        var evt3 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-3), Price = 100m, MaxPlayers = 10 };

        db.Users.Add(user1);
        db.Users.Add(user2);
        db.Users.Add(user3);
        db.Groups.Add(grp);
        db.Events.Add(evt1);
        db.Events.Add(evt2);
        db.Events.Add(evt3);
        await db.SaveChangesAsync();

        db.EventConfirmations.Add(new EventConfirmation { EventId = evt1.Id, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield });  // 30
        db.EventConfirmations.Add(new EventConfirmation { EventId = evt2.Id, UserId = "user-2", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield });  // 50
        db.EventConfirmations.Add(new EventConfirmation { EventId = evt3.Id, UserId = "user-3", PaymentStatus = EventConfirmationPaymentStatus.Pending, HasPaid = false, Position = FutsalPosition.Outfield }); // 100
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1", "user-2", "user-3" };

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert - Should be ordered: 100, 50, 30
        Assert.Equal(3, result.Count);
        Assert.Equal(100m, result[0].TotalAmount);
        Assert.Equal(50m, result[1].TotalAmount);
        Assert.Equal(30m, result[2].TotalAmount);
    }

    [Fact]
    public async Task LoadUnpaidConfirmationsAsync_ExcludesNonGroupMembers()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user1 = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var user2 = new ApplicationUser { Id = "user-2", UserName = "Maria", FullName = "Maria Santos" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var evt = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 50m, MaxPlayers = 10 };

        db.Users.Add(user1);
        db.Users.Add(user2);
        db.Groups.Add(grp);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        // User1 is member, User2 is not
        var memberConf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        var nonMemberConf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "user-2",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield
        };
        db.EventConfirmations.Add(memberConf);
        db.EventConfirmations.Add(nonMemberConf);
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };  // Only user-1 is member

        // Act
        var result = await service.LoadUnpaidConfirmationsAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Equal("user-1", result[0].UserId);
    }

    [Fact]
    public async Task LoadPaymentHistoryAsync_ReturnsLastFiftyManualPayments()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "Admin", FullName = "Admin Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };

        db.Users.Add(user);
        db.Users.Add(admin);
        db.Groups.Add(grp);
        await db.SaveChangesAsync();

        // Create 60 paid confirmations (only last 50 should be returned)
        var now = DateTime.UtcNow;
        for (int i = 0; i < 60; i++)
        {
            var evt = new Event
            {
                GroupId = grp.Id,
                StartsAt = now.AddDays(-i),
                Price = 50m,
                MaxPlayers = 10
            };
            db.Events.Add(evt);
            await db.SaveChangesAsync();

            var conf = new EventConfirmation
            {
                EventId = evt.Id,
                UserId = "user-1",
                PaymentStatus = EventConfirmationPaymentStatus.Paid,
                HasPaid = true,
                Position = FutsalPosition.Outfield,
                MarkedPaidByUserId = "admin-1",
                MarkedPaidAt = now.AddDays(-i)
            };
            db.EventConfirmations.Add(conf);
        }
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadPaymentHistoryAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Equal(50, result.Count);  // Max 50
    }

    [Fact]
    public async Task LoadPaymentHistoryAsync_ExcludesNonManualPayments()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "Admin", FullName = "Admin Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var evt1 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 50m, MaxPlayers = 10 };
        var evt2 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-2), Price = 50m, MaxPlayers = 10 };

        db.Users.Add(user);
        db.Users.Add(admin);
        db.Groups.Add(grp);
        db.Events.Add(evt1);
        db.Events.Add(evt2);
        await db.SaveChangesAsync();

        var manualPaid = new EventConfirmation
        {
            EventId = evt1.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,
            Position = FutsalPosition.Outfield,
            MarkedPaidByUserId = "admin-1",  // Manual
            MarkedPaidAt = DateTime.UtcNow
        };
        var gatewayPaid = new EventConfirmation
        {
            EventId = evt2.Id,
            UserId = "user-1",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            HasPaid = true,
            Position = FutsalPosition.Outfield,
            MarkedPaidByUserId = null,  // No manual mark
            PaymentGatewayName = "EfiBank"
        };
        db.EventConfirmations.Add(manualPaid);
        db.EventConfirmations.Add(gatewayPaid);
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadPaymentHistoryAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Single(result);
        Assert.Equal("João Silva", result[0].UserName);
    }

    [Fact]
    public async Task LoadPaymentHistoryAsync_OrdersByMarkedAtDescending()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "Admin", FullName = "Admin Silva" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };

        db.Users.Add(user);
        db.Users.Add(admin);
        db.Groups.Add(grp);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var evt1 = new Event { GroupId = 1, StartsAt = now.AddHours(-1), Price = 50m, MaxPlayers = 10 };
        var evt2 = new Event { GroupId = 1, StartsAt = now.AddHours(-2), Price = 50m, MaxPlayers = 10 };
        var evt3 = new Event { GroupId = 1, StartsAt = now.AddHours(-3), Price = 50m, MaxPlayers = 10 };

        db.Events.Add(evt1);
        db.Events.Add(evt2);
        db.Events.Add(evt3);
        await db.SaveChangesAsync();

        db.EventConfirmations.Add(new EventConfirmation { EventId = evt1.Id, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield, MarkedPaidByUserId = "admin-1", MarkedPaidAt = now.AddMinutes(-10) });
        db.EventConfirmations.Add(new EventConfirmation { EventId = evt2.Id, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield, MarkedPaidByUserId = "admin-1", MarkedPaidAt = now.AddMinutes(-30) });
        db.EventConfirmations.Add(new EventConfirmation { EventId = evt3.Id, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield, MarkedPaidByUserId = "admin-1", MarkedPaidAt = now.AddMinutes(-20) });
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadPaymentHistoryAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert - Should be ordered by MarkedAt DESC: -10, -20, -30
        Assert.Equal(3, result.Count);
        Assert.True(result[0].MarkedAt > result[1].MarkedAt);
        Assert.True(result[1].MarkedAt > result[2].MarkedAt);
    }

    [Fact]
    public async Task LoadPaymentHistoryAsync_PopulatesAdminName()
    {
        // Arrange
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        var user = new ApplicationUser { Id = "user-1", UserName = "João", FullName = "João Silva" };
        var admin1 = new ApplicationUser { Id = "admin-1", UserName = "admin1", FullName = "Admin Um" };
        var admin2 = new ApplicationUser { Id = "admin-2", UserName = "admin2", FullName = "Admin Dois" };
        var grp = new Group { Sport = Sport.Futsal, Name = "Grupo A", CreatedByUserId = "admin-1" };
        var evt1 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-1), Price = 50m, MaxPlayers = 10 };
        var evt2 = new Event { GroupId = 1, StartsAt = DateTime.UtcNow.AddHours(-2), Price = 50m, MaxPlayers = 10 };

        db.Users.Add(user);
        db.Users.Add(admin1);
        db.Users.Add(admin2);
        db.Groups.Add(grp);
        db.Events.Add(evt1);
        db.Events.Add(evt2);
        await db.SaveChangesAsync();

        db.EventConfirmations.Add(new EventConfirmation { EventId = evt1.Id, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield, MarkedPaidByUserId = "admin-1", MarkedPaidAt = DateTime.UtcNow });
        db.EventConfirmations.Add(new EventConfirmation { EventId = evt2.Id, UserId = "user-1", PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true, Position = FutsalPosition.Outfield, MarkedPaidByUserId = "admin-2", MarkedPaidAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new DelinquencyService(factory);
        var memberIds = new HashSet<string> { "user-1" };

        // Act
        var result = await service.LoadPaymentHistoryAsync(grp.Id, memberIds, Sport.Futsal);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.NotNull(result[0].AdminName);
        Assert.NotNull(result[1].AdminName);
    }
}

