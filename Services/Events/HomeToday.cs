using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services.Events;

public enum HomeGreeting { Morning, Afternoon, Evening }

public sealed record HomeTodayMatch(int EventId, string GroupName, DateTime StartsLocal, Sport Sport, bool Organizing);

public sealed record HomeTodaySummary(
    HomeGreeting Greeting,
    IReadOnlyList<HomeTodayMatch> Today,
    HomeTodayMatch? Next,
    int PendingPayments,
    int UnreadMessages);

/// <summary>
/// "Hoje" panel of the home: what the signed-in user has today across all
/// groups. Pure calculation over already-loaded data, in local time.
/// </summary>
public static class HomeToday
{
    public const int TipCount = 5;

    public static HomeGreeting GreetingFor(DateTime localNow) => localNow.Hour switch
    {
        < 5  => HomeGreeting.Evening,
        < 12 => HomeGreeting.Morning,
        < 18 => HomeGreeting.Afternoon,
        _    => HomeGreeting.Evening,
    };

    public static int TipOfTheDay(DateTime localNow) => localNow.DayOfYear % TipCount;

    public static HomeTodaySummary Build(
        IEnumerable<EventConfirmation> playing,
        IEnumerable<Event> organizing,
        int pendingPayments,
        int unreadMessages,
        DateTime localNow)
    {
        var matches = playing.Select(c => (Event: c.Event, Organizing: false))
            .Concat(organizing.Select(e => (Event: e, Organizing: true)))
            .Where(x => x.Event.IsActive && x.Event.StartsAt.ToLocalTime() >= localNow)
            .GroupBy(x => x.Event.Id)
            .Select(g => new HomeTodayMatch(
                g.Key,
                g.First().Event.Group?.Name ?? string.Empty,
                g.First().Event.StartsAt.ToLocalTime(),
                g.First().Event.Sport,
                g.Any(x => x.Organizing)))
            .OrderBy(m => m.StartsLocal)
            .ToList();

        var today = matches.Where(m => m.StartsLocal.Date == localNow.Date).ToList();

        return new HomeTodaySummary(
            GreetingFor(localNow),
            today,
            matches.FirstOrDefault(),
            pendingPayments,
            unreadMessages);
    }
}
