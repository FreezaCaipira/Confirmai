using Confirmai.Services;

namespace Confirmai.Tests;

public class UiTextServiceTests
{
    [Fact]
    public void Get_ReturnsPortugueseText_ByDefault()
    {
        var language = new LanguagePreferenceService();
        var sut = new UiTextService(language);

        var value = sut["App.NotAuthorized"];

        Assert.Equal("Voce nao tem permissao para acessar esta pagina.", value);
    }

    [Fact]
    public void Get_ReturnsEnglishText_WhenLanguageIsEnglish()
    {
        var language = new LanguagePreferenceService();
        language.SetLanguage("en-US");
        var sut = new UiTextService(language);

        var value = sut["App.NotAuthorized"];

        Assert.Equal("You are not authorized to access this page.", value);
    }

    [Fact]
    public void Get_ReturnsSpanishText_WhenLanguageIsSpanish()
    {
        var language = new LanguagePreferenceService();
        language.SetLanguage("es-ES");
        var sut = new UiTextService(language);

        var value = sut["App.NotAuthorized"];

        Assert.Equal("No tienes permiso para acceder a esta pagina.", value);
    }

    [Fact]
    public void Get_ReturnsKey_WhenTranslationIsMissing()
    {
        var language = new LanguagePreferenceService();
        language.SetLanguage("en-US");
        var sut = new UiTextService(language);

        var value = sut["Unknown.Key.For.Tests"];

        Assert.Equal("Unknown.Key.For.Tests", value);
    }
}


