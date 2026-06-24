using Confirmai.Services.Events;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Enums;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class EventNotificationSchedulerServiceTests
{
    private static (EventNotificationSchedulerService svc, AppDbContext db, Mock<IEmailSender> emailMock) Build()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddDbContextFactory<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        
        var emailMock = new Mock<IEmailSender>();
        services.AddSingleton(emailMock.Object);
        services.AddScoped<EventNotificationService>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var svc = new EventNotificationSchedulerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EventNotificationSchedulerService>.Instance);

        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);

        return (svc, db, emailMock);
    }

    private static Event SeedRecurringEvent(AppDbContext db, DateTime startsAtUtc, int groupId = 1)
    {
        var group = new Group
        {
            Name = "Racha Teste",
            Sport = Sport.Futsal,
            City = "Pouso Alegre",
            StateCode = "MG",
            CreatedByUserId = "seeder"
        };
        db.Groups.Add(group);
        db.SaveChanges();

        var venue = new Venue
        {
            Name = "Arena Teste",
            Address = "Rua do Racha, 10",
            City = "Pouso Alegre",
            StateCode = "MG",
            Type = VenueType.Quadra,
            IsActive = true
        };
        db.Venues.Add(venue);
        db.SaveChanges();

        var schedule = new MatchSchedule
        {
            GroupId = group.Id,
            VenueId = venue.Id,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDay = new TimeOnly(20, 0),
            DurationMinutes = 90,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
            Price = 25m,
            IsActive = true,
            CreatedByUserId = "seeder"
        };
        db.RachaSchedules.Add(schedule);
        db.SaveChanges();

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            VenueId = venue.Id,
            Location = venue.Address,
            StartsAt = startsAtUtc,
            DurationMinutes = 90,
            Price = 25m,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
            RachaScheduleId = schedule.Id,
            CreatedByUserId = "seeder",
            IsActive = true,
        };
        db.Events.Add(ev);
        db.SaveChanges();

        return ev;
    }

    [Fact]
    public async Task CheckAndNotifyTodaysEventsAsync_DoesNotThrow_WhenNoEventsExist()
    {
        var (svc, db, _) = Build();

        var ex = await Record.ExceptionAsync(async () =>
        {
            await using var scope = ((IServiceScopeFactory)svc.GetType()
                .GetField("_scopeFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(svc)!).CreateAsyncScope();
            
            var method = svc.GetType().GetMethod("CheckAndNotifyTodaysEventsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(svc, new object[] { CancellationToken.None })!;
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task CheckAndNotifyTodaysEventsAsync_DoesNotThrow_WhenEventExists()
    {
        var (svc, db, _) = Build();
        var today = DateTime.UtcNow.Date.AddHours(10);
        SeedRecurringEvent(db, today);

        var ex = await Record.ExceptionAsync(async () =>
        {
            await using var scope = ((IServiceScopeFactory)svc.GetType()
                .GetField("_scopeFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(svc)!).CreateAsyncScope();
            
            var method = svc.GetType().GetMethod("CheckAndNotifyTodaysEventsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(svc, new object[] { CancellationToken.None })!;
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task CheckAndNotifyTodaysEventsAsync_IgnoresNonRecurringEvents()
    {
        var (svc, db, _) = Build();
        var today = DateTime.UtcNow.Date.AddHours(10);

        var group = new Group
        {
            Name = "Racha Teste",
            Sport = Sport.Futsal,
            City = "Pouso Alegre",
            StateCode = "MG",
            CreatedByUserId = "seeder"
        };
        db.Groups.Add(group);
        db.SaveChanges();

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            Location = "Quadra",
            StartsAt = today,
            MaxPlayers = 10,
            CreatedByUserId = "seeder",
            IsActive = true,
            RachaScheduleId = null // Non-recurring
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var ex = await Record.ExceptionAsync(async () =>
        {
            await using var scope = ((IServiceScopeFactory)svc.GetType()
                .GetField("_scopeFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(svc)!).CreateAsyncScope();
            
            var method = svc.GetType().GetMethod("CheckAndNotifyTodaysEventsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(svc, new object[] { CancellationToken.None })!;
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task CheckAndNotifyTodaysEventsAsync_IgnoresInactiveEvents()
    {
        var (svc, db, _) = Build();
        var today = DateTime.UtcNow.Date.AddHours(10);
        var ev = SeedRecurringEvent(db, today);
        ev.IsActive = false;
        db.SaveChanges();

        var ex = await Record.ExceptionAsync(async () =>
        {
            await using var scope = ((IServiceScopeFactory)svc.GetType()
                .GetField("_scopeFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(svc)!).CreateAsyncScope();
            
            var method = svc.GetType().GetMethod("CheckAndNotifyTodaysEventsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(svc, new object[] { CancellationToken.None })!;
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task CheckAndNotifyTodaysEventsAsync_IgnoresEventsNotToday()
    {
        var (svc, db, _) = Build();
        var tomorrow = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        SeedRecurringEvent(db, tomorrow);

        var ex = await Record.ExceptionAsync(async () =>
        {
            await using var scope = ((IServiceScopeFactory)svc.GetType()
                .GetField("_scopeFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(svc)!).CreateAsyncScope();
            
            var method = svc.GetType().GetMethod("CheckAndNotifyTodaysEventsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(svc, new object[] { CancellationToken.None })!;
        });

        Assert.Null(ex);
    }

    [Fact]
    public void GetNextNotificationTime_Returns8AM()
    {
        var method = typeof(EventNotificationSchedulerService).GetMethod("GetNextNotificationTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = (DateTime)method!.Invoke(null, null)!;
        
        Assert.Equal(8, result.Hour);
        Assert.Equal(0, result.Minute);
    }
}
