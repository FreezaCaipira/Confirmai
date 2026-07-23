using Confirmai.Models;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal.Components;

public partial class DetailLocationSection
{
    [Parameter]
    public Event Event { get; set; } = null!;

    [Parameter]
    public string MapsApiKey { get; set; } = string.Empty;

    private bool HasMapsKey => !string.IsNullOrWhiteSpace(MapsApiKey) && !MapsApiKey.Contains("SET_VIA");

    private string MapAddress
    {
        get => Event.Venue is not null
            ? $"{Event.Venue.Name}, {Event.Venue.City}, {Event.Venue.StateCode}, Brasil"
            : Event.Location;
    }

    private string MapsSearchUrl
    {
        get => $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(MapAddress)}";
    }

    private string EmbedUrl
    {
        get => $"https://www.google.com/maps/embed/v1/place?key={MapsApiKey}&q={Uri.EscapeDataString(MapAddress)}&language=pt-BR";
    }

    private string MapWrapId => $"map-wrap-{Event.Id}";

    private string MapFallbackId => $"map-fallback-{Event.Id}";
}
