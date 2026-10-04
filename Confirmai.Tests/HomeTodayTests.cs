using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class HomeTodayTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 14, 30, 0, DateTimeKind.Local);

    private static Event Ev(int id, DateTime startsAt, bool active = true, string group = "Racha") => new()
    {
        Id = id,
        StartsAt = startsAt,
        IsActive = active,
        Sport = Sport.Futsal,
        Group = new Group { Name = group },
    };

    private static EventConfirmation Conf(Event ev) => new() { Event = ev };

    [Theory]
    [InlineData(3, HomeGreeting.Evening)]
    [InlineData(5, HomeGreeting.Morning)]
    [InlineData(11, HomeGreeting.Morning)]
    [InlineData(12, HomeGreeting.Afternoon)]
    [InlineData(17, HomeGreeting.Afternoon)]
    [InlineData(18, HomeGreeting.Evening)]
    public void GreetingFor_UsesLocalHour(int hour, HomeGreeting expected)
    {
        Assert.Equal(expected, HomeToday.GreetingFor(new DateTime(2026, 6, 22, hour, 0, 0, DateTimeKind.Local)));
    }

    [Fact]
    public void Build_TodayOnlyHasFutureActiveMatchesOfToday()
    {
        var playing = new[]
        {
            Conf(Ev(1, Now.AddHours(2))),
            Conf(Ev(2, Now.AddHours(-1))),
            Conf(Ev(3, Now.AddHours(3), active: false)),
            Conf(Ev(4, Now.AddDays(1))),
        };

        var summary = HomeToday.Build(playing, [], 0, 0, Now);

        Assert.Equal([1], summary.Today.Select(m => m.EventId));
        Assert.Equal(1, summary.Next?.EventId);
    }

    [Fact]
    public void Build_SameEventPlayingAndOrganizing_AppearsOnceAsOrganizing()
    {
        var ev = Ev(7, Now.AddHours(1));

        var summary = HomeToday.Build([Conf(ev)], [ev], 0, 0, Now);

        var match = Assert.Single(summary.Today);
        Assert.True(match.Organizing);
    }

    [Fact]
    public void Build_NextIsEarliestUpcomingEvenIfNotToday()
    {
        var summary = HomeToday.Build(
            [Conf(Ev(1, Now.AddDays(3)))],
            [Ev(2, Now.AddDays(2), group: "Organizado")],
            2, 5, Now);

        Assert.Empty(summary.Today);
        Assert.Equal(2, summary.Next?.EventId);
        Assert.Equal("Organizado", summary.Next?.GroupName);
        Assert.Equal(2, summary.PendingPayments);
        Assert.Equal(5, summary.UnreadMessages);
    }

    [Fact]
    public void TipOfTheDay_StaysInRange()
    {
        for (var d = 0; d < 400; d++)
        {
            var tip = HomeToday.TipOfTheDay(Now.AddDays(d));
            Assert.InRange(tip, 0, HomeToday.TipCount - 1);
        }
    }
}
