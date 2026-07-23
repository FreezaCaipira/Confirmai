using Confirmai.Pages.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class RecurrenceScheduler
{
    [Parameter]
    public Create.CreateMatchForm Form { get; set; } = new();

    [Parameter]
    public EventCallback<(DayOfWeek, bool)> OnToggleDay { get; set; }

    private static readonly (DayOfWeek Dow, string Label)[] WeekdayOptions =
    [
        (DayOfWeek.Monday,    "Seg"),
        (DayOfWeek.Tuesday,   "Ter"),
        (DayOfWeek.Wednesday, "Qua"),
        (DayOfWeek.Thursday,  "Qui"),
        (DayOfWeek.Friday,    "Sex"),
        (DayOfWeek.Saturday,  "Sáb"),
        (DayOfWeek.Sunday,    "Dom"),
    ];

    private async Task HandleToggleDay(DayOfWeek dow, bool on)
        => await OnToggleDay.InvokeAsync((dow, on));
}
