using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class EventDateTextTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 14, 30, 0, DateTimeKind.Local);

    [Fact]
    public void Relative_Today_ReturnsToday()
    {
        var rel = EventDateText.Relative(Now.AddHours(4), Now);
        Assert.Equal(EventRelativeKind.Today, rel.Kind);
    }

    [Fact]
    public void Relative_Tomorrow_ReturnsTomorrow()
    {
        var rel = EventDateText.Relative(Now.AddDays(1), Now);
        Assert.Equal(EventRelativeKind.Tomorrow, rel.Kind);
        Assert.Equal(1, rel.Days);
    }

    [Fact]
    public void Relative_InDays_ReturnsDayCount()
    {
        var rel = EventDateText.Relative(Now.AddDays(3), Now);
        Assert.Equal(EventRelativeKind.InDays, rel.Kind);
        Assert.Equal(3, rel.Days);
    }

    [Fact]
    public void Relative_DaysAgo_ReturnsDayCount()
    {
        var rel = EventDateText.Relative(Now.AddDays(-2), Now);
        Assert.Equal(EventRelativeKind.DaysAgo, rel.Kind);
        Assert.Equal(2, rel.Days);
    }

    [Fact]
    public void Relative_ComparesCalendarDays_NotElapsedHours()
    {
        // 00:01 amanha vs 23:59 hoje = ~2 minutos, mas e "amanha" no calendario
        var lateNow = new DateTime(2026, 6, 22, 23, 59, 0, DateTimeKind.Local);
        var rel = EventDateText.Relative(new DateTime(2026, 6, 23, 0, 1, 0, DateTimeKind.Local), lateNow);
        Assert.Equal(EventRelativeKind.Tomorrow, rel.Kind);
    }
}
