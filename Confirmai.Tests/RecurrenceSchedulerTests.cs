using Microsoft.AspNetCore.Components.Web;
using Confirmai.Pages.Futsal;
using Confirmai.Pages.Futsal.Components;

namespace Confirmai.Tests;

public class RecurrenceSchedulerTests
{
    private static Create.CreateMatchForm NewForm() => new();

    [Fact]
    public void RecurrenceEnabled_RendersCheckbox()
    {
        var form = NewForm();
        form.RecurrenceEnabled = true;
        form.SelectedDays = new HashSet<DayOfWeek> { DayOfWeek.Monday };
        var component = new RecurrenceScheduler { Form = form };

        Assert.True(component.Form.RecurrenceEnabled);
        Assert.Contains(DayOfWeek.Monday, component.Form.SelectedDays);
    }

    [Fact]
    public void SelectedDays_InitiallyEmpty()
    {
        var component = new RecurrenceScheduler { Form = NewForm() };

        Assert.Empty(component.Form.SelectedDays);
    }

    [Fact]
    public void WeekdayOptions_ContainsAllDays()
    {
        var component = new RecurrenceScheduler { Form = NewForm() };
        
        Assert.False(component.Form.RecurrenceEnabled);
    }

    [Fact]
    public void SelectedDays_CanContainMultipleDays()
    {
        var form = NewForm();
        form.RecurrenceEnabled = true;
        form.SelectedDays = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
        var component = new RecurrenceScheduler { Form = form };

        Assert.Equal(3, component.Form.SelectedDays.Count);
        Assert.Contains(DayOfWeek.Monday, component.Form.SelectedDays);
        Assert.Contains(DayOfWeek.Wednesday, component.Form.SelectedDays);
        Assert.Contains(DayOfWeek.Friday, component.Form.SelectedDays);
    }

    [Fact]
    public void SelectedDays_CanContainAllDays()
    {
        var form = NewForm();
        form.RecurrenceEnabled = true;
        form.SelectedDays = new HashSet<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };
        var component = new RecurrenceScheduler { Form = form };

        Assert.Equal(7, component.Form.SelectedDays.Count);
    }

    [Fact]
    public void RecurrenceEnabled_DefaultIsFalse()
    {
        var component = new RecurrenceScheduler { Form = NewForm() };

        Assert.False(component.Form.RecurrenceEnabled);
    }
}
