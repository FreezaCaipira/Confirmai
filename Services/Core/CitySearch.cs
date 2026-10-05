namespace Confirmai.Services.Core;

/// <summary>
/// C38 F3 — busca de cidade sem acento/caixa para o CityAutocomplete.
/// Aceita so grafia oficial do cities.json (TryGetCanonical) no submit.
/// </summary>
public static class CitySearch
{
    public const int MinQueryLength = 2;
    public const int MaxSuggestions = 8;

    /// <summary>Prefixo primeiro, depois ocorrencias parciais — ate <paramref name="maxResults"/>.</summary>
    public static List<string> Search(IEnumerable<string> cities, string? query, int maxResults = MaxSuggestions)
    {
        var q = CityNormalizer.Normalize(query);
        if (q.Length < MinQueryLength) return new();

        return cities
            .Select(c => (city: c, norm: CityNormalizer.Normalize(c)))
            .Where(x => x.norm.Contains(q))
            .OrderByDescending(x => x.norm.StartsWith(q))
            .ThenBy(x => x.norm)
            .Select(x => x.city)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>Resolve a digitacao do usuario para a grafia oficial da lista; false se nao pertence ao UF.</summary>
    public static bool TryGetCanonical(IEnumerable<string> cities, string? input, out string official)
    {
        var q = CityNormalizer.Normalize(input);
        official = q.Length == 0
            ? string.Empty
            : cities.FirstOrDefault(c => CityNormalizer.Normalize(c) == q) ?? string.Empty;
        return official.Length > 0;
    }
}
