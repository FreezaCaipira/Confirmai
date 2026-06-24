using Microsoft.JSInterop;
using System.Text.Json;

namespace Confirmai.Services;

/// <summary>
/// Service para operações de geolocalização.
/// </summary>
public class LocationService
{
    private readonly IJSRuntime _jsRuntime;

    public LocationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Obtém a localização atual do usuário via Geolocation API.
    /// </summary>
    public async Task<(double Latitude, double Longitude)?> GetCurrentPositionAsync()
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<object[]>("navigator.geolocation.getCurrentPosition");
            if (result == null || result.Length == 0)
                return null;

            var position = result[0] as IDictionary<string, object>;
            if (position == null)
                return null;

            var coords = position["coords"] as IDictionary<string, object>;
            if (coords == null)
                return null;

            var lat = coords["latitude"];
            var lon = coords["longitude"];

            if (lat is double latitude && lon is double longitude)
                return (latitude, longitude);

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Faz reverse geocoding usando a API Nominatim do OpenStreetMap.
    /// </summary>
    public async Task<(string City, string StateCode)?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(5);
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&accept-language=pt-BR";
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
            var address = data.GetProperty("address");

            var city = address.TryGetProperty("city", out var cityProp) ? cityProp.GetString()
                     : address.TryGetProperty("town", out var townProp) ? townProp.GetString()
                     : address.TryGetProperty("municipality", out var munProp) ? munProp.GetString()
                     : null;

            var state = address.TryGetProperty("state", out var stateProp) ? stateProp.GetString() : null;

            if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(state))
                return null;

            // Converte nome do estado para código UF
            var stateCode = GetStateCode(state);
            if (string.IsNullOrEmpty(stateCode))
                return null;

            return (city, stateCode);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converte nome do estado brasileiro para código UF.
    /// </summary>
    private static string GetStateCode(string stateName)
    {
        var stateMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Acre"] = "AC", ["Alagoas"] = "AL", ["Amapá"] = "AP", ["Amazonas"] = "AM",
            ["Bahia"] = "BA", ["Ceará"] = "CE", ["Distrito Federal"] = "DF", ["Espírito Santo"] = "ES",
            ["Goiás"] = "GO", ["Maranhão"] = "MA", ["Mato Grosso"] = "MT", ["Mato Grosso do Sul"] = "MS",
            ["Minas Gerais"] = "MG", ["Pará"] = "PA", ["Paraíba"] = "PB", ["Paraná"] = "PR",
            ["Pernambuco"] = "PE", ["Piauí"] = "PI", ["Rio de Janeiro"] = "RJ", ["Rio Grande do Norte"] = "RN",
            ["Rio Grande do Sul"] = "RS", ["Rondônia"] = "RO", ["Roraima"] = "RR", ["Santa Catarina"] = "SC",
            ["São Paulo"] = "SP", ["Sergipe"] = "SE", ["Tocantins"] = "TO"
        };

        return stateMap.TryGetValue(stateName, out var code) ? code : string.Empty;
    }
}
