using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

/// <summary>
/// Unit tests for MatchSchedule-related business logic:
/// - MaxGoalkeepers propagation from event edit back to the parent schedule
/// - scheduleNextEvents query used in MyEvents/Index.razor
/// </summary>
public class MatchScheduleTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb() => TestDataFactory.CreateDbContext();

    private static (MatchSchedule schedule, Event ev) SeedScheduleWithEvent(
        AppDbContext db,
        string creatorId = "creator-1",
        int? maxGoalkeepers = 2,
        int confirmations = 0)
    {
        var group = new Group { Name = "Racha", Sport = Sport.Futsal, CreatedByUserId = creatorId };
        db.Groups.Add(group);
        var venue = new Venue
        {
            Name = "Arena", Address = "Rua 1", City = "Pouso Alegre",
            StateCode = "MG", Type = VenueType.Quadra, IsActive = true
        };
        db.Venues.Add(venue);
        db.SaveChanges();

        var schedule = new MatchSchedule
        {
            GroupId         = group.Id,
            VenueId         = venue.Id,
            DayOfWeek       = DayOfWeek.Friday,
            TimeOfDay       = new TimeOnly(20, 0),
            DurationMinutes = 90,
            MaxPlayers      = 10,
            MaxGoalkeepers  = maxGoalkeepers,
            Price           = 25m,
            IsActive        = true,
            CreatedByUserId = creatorId,
        };
        db.RachaSchedules.Add(schedule);
        db.SaveChanges();

        var ev = new Event
        {
            GroupId         = group.Id,
            Sport           = Sport.Futsal,
            Location        = "Arena",
            StartsAt        = DateTime.UtcNow.AddDays(7),
            MaxPlayers      = 10,
            MaxGoalkeepers  = maxGoalkeepers,
            CreatedByUserId = creatorId,
            IsActive        = true,
            RachaScheduleId = schedule.Id,
        };
        db.Events.Add(ev);
        db.SaveChanges();

        for (int i = 0; i < confirmations; i++)
        {
            var uid = $"player-{i}";
            if (!db.Users.Any(u => u.Id == uid))
                db.Users.Add(new ApplicationUser { Id = uid, UserName = uid, Email = $"{uid}@test.com" });
            db.EventConfirmations.Add(new EventConfirmation { EventId = ev.Id, UserId = uid });
        }
        db.SaveChanges();

        return (schedule, ev);
    }

    // ── MaxGoalkeepers propagation ────────────────────────────────────────────

    [Fact]
    public async Task EventEdit_PropagatesMaxGoalkeepers_ToParentSchedule()
    {
        using var db = CreateDb();
        var (schedule, ev) = SeedScheduleWithEvent(db, maxGoalkeepers: 2);

        // Simulate what Edit.razor Save() does
        ev.MaxGoalkeepers = 4;
        if (ev.RachaScheduleId.HasValue)
        {
            var sch = await db.RachaSchedules.FindAsync(ev.RachaScheduleId.Value);
            if (sch is not null)
                sch.MaxGoalkeepers = ev.MaxGoalkeepers;
        }
        await db.SaveChangesAsync();

        var updated = await db.RachaSchedules.FindAsync(schedule.Id);
        Assert.Equal(4, updated!.MaxGoalkeepers);
    }

    [Fact]
    public async Task EventEdit_ClearsMaxGoalkeepers_WhenRotateInGoalEnabled()
    {
        using var db = CreateDb();
        var (schedule, ev) = SeedScheduleWithEvent(db, maxGoalkeepers: 2);

        // RotateInGoal → MaxGoalkeepers = null (stored as 0 → null)
        ev.MaxGoalkeepers = null;
        if (ev.RachaScheduleId.HasValue)
        {
            var sch = await db.RachaSchedules.FindAsync(ev.RachaScheduleId.Value);
            if (sch is not null)
                sch.MaxGoalkeepers = ev.MaxGoalkeepers;
        }
        await db.SaveChangesAsync();

        var updated = await db.RachaSchedules.FindAsync(schedule.Id);
        Assert.Null(updated!.MaxGoalkeepers);
    }

    [Fact]
    public async Task EventEdit_WithNoRachaScheduleId_DoesNotThrow()
    {
        using var db = CreateDb();
        var group = new Group { Name = "Avulso", Sport = Sport.Futsal, CreatedByUserId = "u1" };
        db.Groups.Add(group);
        db.SaveChanges();

        var ev = new Event
        {
            GroupId         = group.Id,
            Sport           = Sport.Futsal,
            Location        = "Quadra",
            StartsAt        = DateTime.UtcNow.AddDays(1),
            MaxPlayers      = 10,
            MaxGoalkeepers  = 2,
            CreatedByUserId = "u1",
            IsActive        = true,
            RachaScheduleId = null,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(async () =>
        {
            ev.MaxGoalkeepers = 4;
            if (ev.RachaScheduleId.HasValue)
            {
                var sch = await db.RachaSchedules.FindAsync(ev.RachaScheduleId.Value);
                if (sch is not null)
                    sch.MaxGoalkeepers = ev.MaxGoalkeepers;
            }
            await db.SaveChangesAsync();
        });

        Assert.Null(exception);
    }

    // ── scheduleNextEvents query ─────────────────────────────────────────────

    [Fact]
    public async Task ScheduleNextEvents_ReturnsNearestUpcomingEvent_PerSchedule()
    {
        using var db = CreateDb();
        var (schedule, _) = SeedScheduleWithEvent(db, confirmations: 3);

        // Add a second event for the same schedule further in the future
        db.Events.Add(new Event
        {
            GroupId         = schedule.GroupId,
            Sport           = Sport.Futsal,
            Location        = "Arena",
            StartsAt        = DateTime.UtcNow.AddDays(14),
            MaxPlayers      = 10,
            CreatedByUserId = "creator-1",
            IsActive        = true,
            RachaScheduleId = schedule.Id,
        });
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var scheduleIds = new List<int> { schedule.Id };

        var nextRows = await db.Events
            .Where(e => e.RachaScheduleId.HasValue
                     && scheduleIds.Contains(e.RachaScheduleId!.Value)
                     && e.StartsAt > now
                     && e.IsActive)
            .Select(e => new
            {
                ScheduleId = e.RachaScheduleId!.Value,
                e.Id,
                e.StartsAt,
                Confirmed  = e.Confirmations.Count,
            })
            .ToListAsync();

        var scheduleNextEvents = nextRows
            .GroupBy(e => e.ScheduleId)
            .ToDictionary(
                g => g.Key,
                g => { var f = g.OrderBy(e => e.StartsAt).First(); return (f.Id, f.StartsAt, f.Confirmed); });

        Assert.True(scheduleNextEvents.ContainsKey(schedule.Id));
        var next = scheduleNextEvents[schedule.Id];
        Assert.Equal(3, next.Confirmed);
        // Should be the nearer event (addDays 7, not 14)
        Assert.True(next.StartsAt < DateTime.UtcNow.AddDays(10));
    }

    [Fact]
    public async Task ScheduleNextEvents_ReturnsEmpty_WhenNoFutureEvents()
    {
        using var db = CreateDb();

        var group = new Group { Name = "G", Sport = Sport.Futsal, CreatedByUserId = "u" };
        db.Groups.Add(group);
        db.SaveChanges();

        var schedule = new MatchSchedule
        {
            GroupId = group.Id, DayOfWeek = DayOfWeek.Monday,
            TimeOfDay = new TimeOnly(20, 0), DurationMinutes = 90,
            MaxPlayers = 10, Price = 0, IsActive = true, CreatedByUserId = "u"
        };
        db.RachaSchedules.Add(schedule);
        // Add a PAST event only
        db.Events.Add(new Event
        {
            GroupId = group.Id, Sport = Sport.Futsal, Location = "X",
            StartsAt = DateTime.UtcNow.AddDays(-1), MaxPlayers = 10,
            CreatedByUserId = "u", IsActive = true, RachaScheduleId = schedule.Id,
        });
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var scheduleIds = new List<int> { schedule.Id };

        var nextRows = await db.Events
            .Where(e => e.RachaScheduleId.HasValue
                     && scheduleIds.Contains(e.RachaScheduleId!.Value)
                     && e.StartsAt > now
                     && e.IsActive)
            .Select(e => new { ScheduleId = e.RachaScheduleId!.Value, e.Id, e.StartsAt, Confirmed = e.Confirmations.Count })
            .ToListAsync();

        Assert.Empty(nextRows);
    }
}
