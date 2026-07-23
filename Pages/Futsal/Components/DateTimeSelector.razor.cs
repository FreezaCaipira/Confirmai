using Confirmai.Pages.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class DateTimeSelector
{
    [Parameter]
    public Create.CreateMatchForm Form { get; set; } = new();

    [Parameter]
    public EventCallback<TimeOnly> OnTimeChange { get; set; }

    private static readonly (int Min, string Label)[] DurationOptions =
    [
        (60,  "1h"),
        (75,  "1h15min"),
        (90,  "1h30min"),
        (105, "1h45min"),
        (120, "2h"),
        (150, "2h30min"),
        (180, "3h"),
    ];

    private async Task HandleTimeChange(ChangeEventArgs e)
    {
        var newTime = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : new TimeOnly(19, 0);
        await OnTimeChange.InvokeAsync(newTime);
    }
}
