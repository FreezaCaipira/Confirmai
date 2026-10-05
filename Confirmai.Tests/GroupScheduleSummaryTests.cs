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
}
