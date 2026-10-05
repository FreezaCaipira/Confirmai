using Confirmai.Services.Core;

namespace Confirmai.Tests;

/// <summary>
/// C38 F3 — CityAutocomplete: busca sem acento/caixa a partir de 2
/// caracteres, ate 8 sugestoes, e submit aceita so grafia oficial do UF.
/// </summary>
public class C38CityAutocompleteTests
{
    private static readonly string[] MinasGerais =
    [
        "Belo Horizonte",
        "Guaranésia",
        "Muzambinho",
        "Muzambinho Sul",
        "Poços de Caldas",
        "Pouso Alegre",
        "São Paulo Fictício",
        "Uberlândia",
    ];

    [Fact]
    public void Search_FindsCity_IgnoringAccentAndCase()
    {
        var results = CitySearch.Search(MinasGerais, "muza");

        Assert.Equal("Muzambinho", results[0]);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_RequiresTwoCharacters()
    {
        Assert.Empty(CitySearch.Search(MinasGerais, "m"));
        Assert.Empty(CitySearch.Search(MinasGerais, ""));
        Assert.Empty(CitySearch.Search(MinasGerais, null));
    }

    [Fact]
    public void Search_CapsAtEightResults()
    {
        var cities = Enumerable.Range(0, 20).Select(i => $"Cidade {i:00}");
        Assert.Equal(CitySearch.MaxSuggestions, CitySearch.Search(cities, "cidade").Count);
    }

    [Fact]
    public void Search_PrefixMatches_First()
    {
        var results = CitySearch.Search(MinasGerais, "sao paulo");

        Assert.Equal("São Paulo Fictício", results[0]);
    }

    [Fact]
    public void TryGetCanonical_ResolvesTypedInput_ToOfficialSpelling()
    {
        Assert.True(CitySearch.TryGetCanonical(MinasGerais, "MUZAMBINHO", out var official));
        Assert.Equal("Muzambinho", official);
    }

    [Fact]
    public void TryGetCanonical_RejectsCity_OutsideStateList()
    {
        Assert.False(CitySearch.TryGetCanonical(MinasGerais, "Pouso Fundo", out _));
        Assert.False(CitySearch.TryGetCanonical(MinasGerais, "", out _));
        Assert.False(CitySearch.TryGetCanonical(MinasGerais, null, out _));
    }
}
