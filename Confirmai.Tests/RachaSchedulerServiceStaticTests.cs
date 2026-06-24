using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class RachaSchedulerServiceStaticTests
{
    [Fact]
    public void GetOccurrences_GeneratesWeeklyOccurrences()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc); // Monday
        var dayOfWeek = DayOfWeek.Monday;
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(21); // 3 weeks

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        Assert.Equal(4, occurrences.Count()); // Today + 3 more Mondays
    }

    [Fact]
    public void GetOccurrences_WhenDayOfWeekIsToday_IncludesToday()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc); // Monday
        var dayOfWeek = DayOfWeek.Monday;
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(7);

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        var first = occurrences.First();
        Assert.Equal(from.Date.Add(timeOfDay.ToTimeSpan()), first);
    }

    [Fact]
    public void GetOccurrences_WhenDayOfWeekIsFuture_StartsFromNextOccurrence()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc); // Monday
        var dayOfWeek = DayOfWeek.Wednesday;
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(14);

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        var first = occurrences.First();
        Assert.Equal(DayOfWeek.Wednesday, first.DayOfWeek);
        Assert.Equal(2, occurrences.Count()); // 2 Wednesdays in 2 weeks
    }

    [Fact]
    public void GetOccurrences_WhenDayOfWeekIsPast_StartsFromNextWeek()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc); // Monday
        var dayOfWeek = DayOfWeek.Sunday;
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(14);

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        var first = occurrences.First();
        Assert.Equal(DayOfWeek.Sunday, first.DayOfWeek);
        Assert.Equal(2, occurrences.Count()); // 2 Sundays in 2 weeks
    }

    [Fact]
    public void GetOccurrences_RespectsTimeOfDay()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);
        var dayOfWeek = DayOfWeek.Monday;
        var timeOfDay = new TimeOnly(20, 30);
        var windowEnd = from.AddDays(7);

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        var first = occurrences.First();
        Assert.Equal(20, first.Hour);
        Assert.Equal(30, first.Minute);
    }

    [Fact]
    public void GetOccurrences_GeneratesCorrectUtcTimes()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);
        var dayOfWeek = DayOfWeek.Monday;
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(7);

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        Assert.All(occurrences, occ => Assert.Equal(DateTimeKind.Utc, occ.Kind));
    }

    [Fact]
    public void GetOccurrences_WhenWindowEndIsBeforeFirstOccurrence_ReturnsEmpty()
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc); // Monday
        var dayOfWeek = DayOfWeek.Wednesday;
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(1); // Window ends before Wednesday

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        Assert.Empty(occurrences);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void GetOccurrences_WorksForAllDaysOfWeek(DayOfWeek dayOfWeek)
    {
        // Arrange
        var from = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc); // Monday
        var timeOfDay = new TimeOnly(19, 0);
        var windowEnd = from.AddDays(14);

        // Act
        var occurrences = RachaSchedulerService.GetOccurrences(from, dayOfWeek, timeOfDay, windowEnd);

        // Assert
        Assert.NotEmpty(occurrences);
        Assert.All(occurrences, occ => Assert.Equal(dayOfWeek, occ.DayOfWeek));
    }
}
