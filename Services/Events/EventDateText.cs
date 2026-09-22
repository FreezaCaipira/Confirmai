namespace Confirmai.Services.Events;

public enum EventRelativeKind { Today, Tomorrow, InDays, DaysAgo }

public readonly record struct EventRelativeDate(EventRelativeKind Kind, int Days);

/// <summary>
/// Calendar-day bucketing for an event start, relative to "now".
/// Pure calculation — the caller maps <see cref="EventRelativeKind"/>
/// to localized text, so this stays culture-free and unit-testable.
/// </summary>
public static class EventDateText
{
    public static EventRelativeDate Relative(DateTime startsAt, DateTime now)
    {
        var days = (startsAt.Date - now.Date).Days;
        return days switch
        {
            0 => new EventRelativeDate(EventRelativeKind.Today, 0),
            1 => new EventRelativeDate(EventRelativeKind.Tomorrow, 1),
            > 1 => new EventRelativeDate(EventRelativeKind.InDays, days),
            _ => new EventRelativeDate(EventRelativeKind.DaysAgo, -days),
        };
    }
}
