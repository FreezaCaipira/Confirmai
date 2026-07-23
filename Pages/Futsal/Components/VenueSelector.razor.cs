using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class VenueSelector
{
    [Parameter]
    public List<Venue> Venues { get; set; } = new();

    [Parameter]
    public int SelectedVenueId { get; set; }

    [Parameter]
    public Venue? SelectedVenue { get; set; }

    [Parameter]
    public EventCallback<int> OnVenueChanged { get; set; }

    private async Task HandleVenueChanged(ChangeEventArgs e)
    {
        var venueId = int.Parse(e.Value?.ToString() ?? "0");
        await OnVenueChanged.InvokeAsync(venueId);
    }

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
