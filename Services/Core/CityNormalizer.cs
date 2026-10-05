using System.Globalization;
using System.Text;
using Confirmai.Models;

namespace Confirmai.Services.Core;

/// <summary>
/// C38 F4/F3 — comparacao de cidade/UF sem acento, caixa ou espacos
/// ("São Paulo" == "sao paulo" == "SÃO PAULO"). Os dados existentes estao
/// inconsistentes; nunca comparar com igualdade direta.
/// </summary>
public static class CityNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(ch));
        }
        return string.Join(' ', sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool SameCity(string? a, string? b) => Normalize(a) == Normalize(b);
    public static bool SameState(string? a, string? b)
        => string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);

    /// <summary>Grupo "Exterior" (EX) compara so a cidade; UF brasileira exige os dois.</summary>
    public static bool VenueMatchesGroup(Venue venue, string? groupCity, string? groupState)
    {
        if (groupState == "EX") return SameCity(venue.City, groupCity);
        return SameCity(venue.City, groupCity) && SameState(venue.StateCode, groupState);
    }

    public static bool VenueMatchesGroup(Venue venue, Group group)
        => VenueMatchesGroup(venue, group.City, group.StateCode);

    /// <summary>
    /// Lista de quadras da cidade do grupo. <paramref name="alwaysInclude"/>
    /// mantem a quadra atual de uma partida legada editavel mesmo fora da
    /// cidade do grupo (C38 F4 — tela de edicao).
    /// </summary>
    public static List<Venue> VenuesForGroup(IEnumerable<Venue> venues, Group group, Venue? alwaysInclude = null)
    {
        var list = venues.Where(v => VenueMatchesGroup(v, group)).ToList();
        if (alwaysInclude is not null && list.All(v => v.Id != alwaysInclude.Id))
            list.Add(alwaysInclude);
        return list;
    }
}
