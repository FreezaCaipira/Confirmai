using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Xunit;

namespace Confirmai.Tests;

/// <summary>C37 F8 — resumo de agenda do grupo para os cards da home.</summary>
public class GroupScheduleSummaryTests
{
    private static MatchSchedule Schedule(
        DayOfWeek dow, int hour = 19, string venue = "Quadra Central", bool active = true)
        => new()
        {
            DayOfWeek = dow,
            TimeOfDay = new TimeOnly(hour, 0),
            IsActive = active,
            Venue = new Venue { Name = venue, Type = VenueType.Society },
        };

    [Fact]
    public void Build_EmptyOrInactive_ReturnsNull()
    {
        Assert.Null(GroupScheduleSummary.Build([]));
        Assert.Null(GroupScheduleSummary.Build(new[] { Schedule(DayOfWeek.Monday, active: false) }));
    }

    [Fact]
    public void Build_SingleDay_ReturnsDayTimeVenue()
    {
        var line = GroupScheduleSummary.Build(new[] { Schedule(DayOfWeek.Wednesday) });

        Assert.NotNull(line);
        Assert.Equal(new[] { DayOfWeek.Wednesday }, line!.Days);
        Assert.Equal(new TimeOnly(19, 0), line.CommonTime);
        Assert.Equal("Quadra Central", line.VenueName);
    }

    [Fact]
    public void Build_MultipleDays_OrderedMondayFirst()
    {
        var line = GroupScheduleSummary.Build(new[]
        {
            Schedule(DayOfWeek.Saturday),
            Schedule(DayOfWeek.Monday),
            Schedule(DayOfWeek.Wednesday),
        });

        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Saturday },
            line!.Days);
    }

    [Fact]
    public void Build_MixedTimes_KeepDaysOnly()
    {
        var line = GroupScheduleSummary.Build(new[]
        {
            Schedule(DayOfWeek.Monday, hour: 19),
            Schedule(DayOfWeek.Friday, hour: 21),
        });

        Assert.NotNull(line);
        Assert.Null(line!.CommonTime);
        Assert.Equal(2, line.Days.Count);
    }

    [Fact]
    public void Build_DistinctVenues_AreJoined()
    {
        var line = GroupScheduleSummary.Build(new[]
        {
            Schedule(DayOfWeek.Monday, venue: "Quadra A"),
            Schedule(DayOfWeek.Friday, venue: "Quadra B"),
        });

        Assert.Equal("Quadra A · Quadra B", line!.VenueName);
    }

    [Fact]
    public void Build_NoVenue_StillSummarizes()
    {
        var line = GroupScheduleSummary.Build(new[]
        {
            new MatchSchedule { DayOfWeek = DayOfWeek.Tuesday, TimeOfDay = new TimeOnly(20, 0), IsActive = true },
        });

        Assert.NotNull(line);
        Assert.Null(line!.VenueName);
        Assert.Equal(new TimeOnly(20, 0), line.CommonTime);
    }

    private static Event Upcoming(DateTime utc, string? venue = null, string location = "Rua X")
        => new()
        {
            StartsAt = utc,
            Location = location,
            IsActive = true,
            Venue = venue is null ? null : new Venue { Name = venue, Type = VenueType.Society },
        };

    [Fact]
    public void BuildFromUpcoming_Empty_ReturnsNull()
    {
        Assert.Null(GroupScheduleSummary.BuildFromUpcoming([]));
    }

    [Fact]
    public void BuildFromUpcoming_UsesLocalDayAndNextEventVenue()
    {
        var first = new DateTime(2030, 1, 7, 22, 0, 0, DateTimeKind.Utc);
        var line = GroupScheduleSummary.BuildFromUpcoming(new[]
        {
            Upcoming(first.AddDays(3), venue: "Quadra B"),
            Upcoming(first, venue: "Quadra A"),
        });

        Assert.NotNull(line);
        Assert.Equal(
            new[] { first.ToLocalTime().DayOfWeek, first.AddDays(3).ToLocalTime().DayOfWeek }
                .OrderBy(d => ((int)d + 6) % 7),
            line!.Days);
        Assert.Equal(TimeOnly.FromDateTime(first.ToLocalTime()), line.CommonTime);
        Assert.Equal("Quadra A", line.VenueName);
    }

    [Fact]
    public void BuildFromUpcoming_WithoutVenue_FallsBackToLocation()
    {
        var line = GroupScheduleSummary.BuildFromUpcoming(new[]
        {
            Upcoming(new DateTime(2030, 1, 7, 22, 0, 0, DateTimeKind.Utc), location: "Ginásio do bairro"),
        });

        Assert.Equal("Ginásio do bairro", line!.VenueName);
    }
}
