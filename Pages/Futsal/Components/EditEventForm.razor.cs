using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Events;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class EditEventForm
{
    [Parameter]
    public EditEventFormData Form { get; set; } = null!;

    [Parameter]
    public List<Venue> Venues { get; set; } = new();

    [Parameter]
    public Venue? SelectedVenue { get; set; }

    [Parameter]
    public bool IsSaving { get; set; }

    [Parameter]
    public bool ShowCancelButton { get; set; }

    [Parameter]
    public bool ShowCancelConfirm { get; set; }

    [Parameter]
    public string SaveError { get; set; } = string.Empty;

    [Parameter]
    public string? CollisionHref { get; set; }

    [Parameter]
    public string CancelError { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnFormSubmit { get; set; }

    [Parameter]
    public EventCallback OnVenueSelected { get; set; }

    [Parameter]
    public EventCallback OnRotateToggled { get; set; }

    [Parameter]
    public EventCallback<ChangeEventArgs> OnTimeChanged { get; set; }

    [Parameter]
    public EventCallback<bool> OnCancelConfirmShowChanged { get; set; }

    [Parameter]
    public EventCallback OnCancelConfirmedClicked { get; set; }

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

    private async Task OnValidSubmit()
        => await OnFormSubmit.InvokeAsync();

    private async Task OnVenueChanged()
        => await OnVenueSelected.InvokeAsync();

    private async Task OnRotateInGoalChanged()
        => await OnRotateToggled.InvokeAsync();

    private async Task OnTimeChange(ChangeEventArgs e)
        => await OnTimeChanged.InvokeAsync(e);

    private async Task OnShowCancelConfirm(bool show)
        => await OnCancelConfirmShowChanged.InvokeAsync(show);

    private async Task OnCancelConfirmed()
        => await OnCancelConfirmedClicked.InvokeAsync();

    private string MapsSearchUrl
    {
        get => SelectedVenue is not null
            ? $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString($"{SelectedVenue.Name}, {SelectedVenue.City}, {SelectedVenue.StateCode}, Brasil")}"
            : "#";
    }

    private static string VenueTypeLabel(VenueType t) => t switch
    {
        VenueType.Quadra  => "Quadra",
        VenueType.Society => "Society",
        VenueType.Campo   => "Campo",
        _                 => t.ToString()
    };
}
