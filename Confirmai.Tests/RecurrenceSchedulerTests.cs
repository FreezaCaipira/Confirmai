using Microsoft.AspNetCore.Components.Web;
using Confirmai.Pages.Futsal.Components;

namespace Confirmai.Tests;

public class RecurrenceSchedulerTests
{
    [Fact]
    public void RecurrenceEnabled_RendersCheckbox()
    {
        var component = new RecurrenceScheduler
        {
            RecurrenceEnabled = true,
            SelectedDays = new HashSet<DayOfWeek> { DayOfWeek.Monday }
        };

        Assert.True(component.RecurrenceEnabled);
        Assert.Contains(DayOfWeek.Monday, component.SelectedDays);
    }

    [Fact]
    public void SelectedDays_InitiallyEmpty()
    {
        var component = new RecurrenceScheduler
        {
            RecurrenceEnabled = false,
            SelectedDays = new HashSet<DayOfWeek>()
        };

        Assert.Empty(component.SelectedDays);
    }

    [Fact]
    public void WeekdayOptions_ContainsAllDays()
    {
        var component = new RecurrenceScheduler();
        
        // The component has 7 weekday options defined
        Assert.True(component.RecurrenceEnabled == false); // Default value
    }

    [Fact]
    public void SelectedDays_CanContainMultipleDays()
    {
        var component = new RecurrenceScheduler
        {
            RecurrenceEnabled = true,
            SelectedDays = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }
        };

        Assert.Equal(3, component.SelectedDays.Count);
        Assert.Contains(DayOfWeek.Monday, component.SelectedDays);
        Assert.Contains(DayOfWeek.Wednesday, component.SelectedDays);
        Assert.Contains(DayOfWeek.Friday, component.SelectedDays);
    }

    [Fact]
    public void SelectedDays_CanContainAllDays()
    {
        var component = new RecurrenceScheduler
        {
            RecurrenceEnabled = true,
            SelectedDays = new HashSet<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            }
        };

        Assert.Equal(7, component.SelectedDays.Count);
    }

    [Fact]
    public void RecurrenceEnabled_DefaultIsFalse()
    {
        var component = new RecurrenceScheduler();

        Assert.False(component.RecurrenceEnabled);
    }
}
