using Confirmai.Services.Events;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class RachaSchedulerServiceTests
{
    // ──────────────────────────────────────────────
    // Test infrastructure helpers
    // ──────────────────────────────────────────────

    private static (RachaSchedulerService svc, AppDbContext db) Build()
    {
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddDbContextFactory<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<IEmailSender>(new Mock<IEmailSender>().Object);
        services.AddScoped<EventNotificationService>();
        services.AddScoped<EventCollisionService>();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var svc = new RachaSchedulerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RachaSchedulerService>.Instance);

        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);

        return (svc, db);
    }

    private static MatchSchedule SeedSchedule(
        AppDbContext db,
        DayOfWeek dayOfWeek,
        bool isActive = true,
        TimeOnly? time = null,
        int maxPlayers = 10,
        decimal price = 25m,
        int? maxGoalkeepers = 2)
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
            DayOfWeek = dayOfWeek,
            TimeOfDay = time ?? new TimeOnly(20, 0),
            DurationMinutes = 90,
            MaxPlayers = maxPlayers,
            MaxGoalkeepers = maxGoalkeepers,
            Price = price,
            IsActive = isActive,
            CreatedByUserId = "seeder"
        };
        db.RachaSchedules.Add(schedule);
        db.SaveChanges();

        return schedule;
    }

    /// <summary>Returns the next occurrence of <paramref name="dayOfWeek"/> from <paramref name="from"/>,
    /// returning <paramref name="from"/> itself when it already matches.</summary>
    private static DateTime GetNextOrCurrentDayOfWeek(DateTime from, DayOfWeek dayOfWeek)
    {
        int daysUntil = ((int)dayOfWeek - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(daysUntil);
    }

    // ──────────────────────────────────────────────
    // GetOccurrences – pure-function tests
    // ──────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_IncludesFromDate_WhenFromAlreadyMatchesDayOfWeek()
    {
        var monday = GetNextOrCurrentDayOfWeek(DateTime.UtcNow.Date, DayOfWeek.Monday);
        var windowEnd = monday.AddDays(28);

        var results = RachaSchedulerService
            .GetOccurrences(monday, DayOfWeek.Monday, new TimeOnly(20, 0), windowEnd)
            .ToList();

        Assert.Contains(results, d => d.Date == monday.Date);
    }

    [Fact]
    public void GetOccurrences_FirstDateIsNextOccurrence_WhenFromDoesNotMatchDayOfWeek()
    {
        // Use a Tuesday; the next Monday is 6 days ahead.
        var tuesday = GetNextOrCurrentDayOfWeek(DateTime.UtcNow.Date, DayOfWeek.Tuesday);
        var expectedFirstMonday = tuesday.AddDays(6);
        var windowEnd = tuesday.AddDays(30);

        var results = RachaSchedulerService
            .GetOccurrences(tuesday, DayOfWeek.Monday, new TimeOnly(20, 0), windowEnd)
            .ToList();

        Assert.NotEmpty(results);
        Assert.Equal(expectedFirstMonday.Date, results[0].Date);
    }

    [Fact]
    public void GetOccurrences_AllReturnedDatesHaveCorrectDayOfWeek()
    {
        var from = DateTime.UtcNow.Date;
        var windowEnd = from.AddDays(56);

        var results = RachaSchedulerService
            .GetOccurrences(from, DayOfWeek.Wednesday, new TimeOnly(19, 30), windowEnd)
            .ToList();

        Assert.NotEmpty(results);
        Assert.All(results, d => Assert.Equal(DayOfWeek.Wednesday, d.DayOfWeek));
    }

    [Fact]
    public void GetOccurrences_AllReturnedDatesHaveCorrectTime()
    {
        var from = DateTime.UtcNow.Date;
        var windowEnd = from.AddDays(56);
        var expectedTime = new TimeOnly(19, 30);

        var results = RachaSchedulerService
            .GetOccurrences(from, DayOfWeek.Wednesday, expectedTime, windowEnd)
            .ToList();

        Assert.All(results, d =>
        {
            Assert.Equal(expectedTime.Hour, d.Hour);
            Assert.Equal(expectedTime.Minute, d.Minute);
        });
    }

    [Fact]
    public void GetOccurrences_ReturnsEmpty_WhenWindowEndIsBeforeNextOccurrence()
    {
        // from = Wednesday; target = Tuesday; windowEnd = Thursday (next Tuesday is 6 days away)
        var wednesday = GetNextOrCurrentDayOfWeek(DateTime.UtcNow.Date, DayOfWeek.Wednesday);
        var windowEnd = wednesday.AddDays(1); // Thursday — next Tuesday is wednesday+6

        var results = RachaSchedulerService
            .GetOccurrences(wednesday, DayOfWeek.Tuesday, new TimeOnly(20, 0), windowEnd)
            .ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void GetOccurrences_DatesAreSpacedExactlySevenDaysApart()
    {
        var from = DateTime.UtcNow.Date;
        var windowEnd = from.AddDays(56);

        var results = RachaSchedulerService
            .GetOccurrences(from, DayOfWeek.Friday, new TimeOnly(21, 0), windowEnd)
            .ToList();

        for (int i = 1; i < results.Count; i++)
        {
            var diff = (results[i] - results[i - 1]).TotalDays;
            Assert.Equal(7.0, diff);
        }
    }

    [Fact]
    public void GetOccurrences_ReturnsAtLeastEightDates_ForEightWeekWindow_StartingOnTargetDay()
    {
        var saturday = GetNextOrCurrentDayOfWeek(DateTime.UtcNow.Date, DayOfWeek.Saturday);
        var windowEnd = saturday.AddDays(8 * 7);

        var results = RachaSchedulerService
            .GetOccurrences(saturday, DayOfWeek.Saturday, new TimeOnly(8, 0), windowEnd)
            .ToList();

        Assert.True(results.Count >= 8, $"Expected at least 8 dates, got {results.Count}");
    }

    [Fact]
    public void GetOccurrences_ReturnsUtcKindDates()
    {
        var from = DateTime.UtcNow.Date;
        var windowEnd = from.AddDays(14);

        var results = RachaSchedulerService
            .GetOccurrences(from, DayOfWeek.Monday, new TimeOnly(20, 0), windowEnd)
            .ToList();

        Assert.All(results, d => Assert.Equal(DateTimeKind.Utc, d.Kind));
    }

    // ──────────────────────────────────────────────
    // GenerateEventsAsync – integration-style unit tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GenerateEventsAsync_CreatesEvents_ForActiveSchedule()
    {
        var (svc, db) = Build();
        SeedSchedule(db, DayOfWeek.Monday, isActive: true);

        await svc.GenerateEventsAsync(CancellationToken.None);

        var events = await db.Events.ToListAsync();
        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task GenerateEventsAsync_DoesNotCreateEvents_ForInactiveSchedule()
    {
        var (svc, db) = Build();
        SeedSchedule(db, DayOfWeek.Monday, isActive: false);

        await svc.GenerateEventsAsync(CancellationToken.None);

        Assert.Empty(await db.Events.ToListAsync());
    }

    [Fact]
    public async Task GenerateEventsAsync_IsIdempotent_CallingTwiceDoesNotDuplicate()
    {
        var (svc, db) = Build();
        SeedSchedule(db, DayOfWeek.Monday, isActive: true);

        await svc.GenerateEventsAsync(CancellationToken.None);
        var countAfterFirst = await db.Events.CountAsync();

        await svc.GenerateEventsAsync(CancellationToken.None);
        var countAfterSecond = await db.Events.CountAsync();

        Assert.Equal(countAfterFirst, countAfterSecond);
        Assert.True(countAfterFirst > 0);
    }

    [Fact]
    public async Task GenerateEventsAsync_SetsCorrectProperties_OnGeneratedEvent()
    {
        var (svc, db) = Build();
        var schedule = SeedSchedule(db, DayOfWeek.Monday, isActive: true, maxPlayers: 14, price: 30m);

        await svc.GenerateEventsAsync(CancellationToken.None);

        var events = await db.Events.ToListAsync();
        Assert.NotEmpty(events);

        var first = events.First();
        Assert.Equal(schedule.GroupId, first.GroupId);
        Assert.Equal(schedule.VenueId, first.VenueId);
        Assert.Equal(Sport.Futsal, first.Sport);
        Assert.Equal(30m, first.Price);
        Assert.Equal(14, first.MaxPlayers);
        Assert.Equal(2, first.MaxGoalkeepers);  // MaxGoalkeepers propagated from schedule
        Assert.Equal(schedule.Id, first.RachaScheduleId);
        Assert.True(first.IsActive);
        Assert.Equal(DayOfWeek.Monday, first.StartsAt.DayOfWeek);
        Assert.Equal("seeder", first.CreatedByUserId);
    }

    [Fact]
    public async Task GenerateEventsAsync_DoesNothing_WhenNoSchedulesExist()
    {
        var (svc, db) = Build();

        await svc.GenerateEventsAsync(CancellationToken.None);

        Assert.Empty(await db.Events.ToListAsync());
    }

    [Fact]
    public async Task GenerateEventsAsync_CreatesEventsForAllActiveSchedules_WhenMultipleExist()
    {
        var (svc, db) = Build();
        SeedSchedule(db, DayOfWeek.Monday, isActive: true);
        SeedSchedule(db, DayOfWeek.Wednesday, isActive: true);

        await svc.GenerateEventsAsync(CancellationToken.None);

        var events = await db.Events.ToListAsync();
        var mondays = events.Count(e => e.StartsAt.DayOfWeek == DayOfWeek.Monday);
        var wednesdays = events.Count(e => e.StartsAt.DayOfWeek == DayOfWeek.Wednesday);
        Assert.True(mondays > 0);
        Assert.True(wednesdays > 0);
    }

    [Fact]
    public async Task GenerateEventsAsync_SkipsCollision_WhenGroupAlreadyHasEventAtThatTime()
    {
        var (svc, db) = Build();
        var schedule = SeedSchedule(db, DayOfWeek.Monday, isActive: true);

        var nextOccurrence = RachaSchedulerService
            .GetOccurrences(DateTime.UtcNow.Date, DayOfWeek.Monday, schedule.TimeOfDay, DateTime.UtcNow.Date.AddDays(14))
            .First();

        db.Events.Add(new Event
        {
            GroupId         = schedule.GroupId,
            Sport           = Sport.Futsal,
            Location        = "Arena Teste",
            StartsAt        = nextOccurrence,
            MaxPlayers      = 10,
            CreatedByUserId = "seeder",
            IsActive        = true,
        });
        db.SaveChanges();

        await svc.GenerateEventsAsync(CancellationToken.None);

        var sameSlotEvents = await db.Events
            .Where(e => e.GroupId == schedule.GroupId && e.StartsAt == nextOccurrence)
            .ToListAsync();

        Assert.Single(sameSlotEvents);
    }
}
