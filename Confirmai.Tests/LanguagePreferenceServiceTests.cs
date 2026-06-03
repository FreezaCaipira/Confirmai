using Confirmai.Services.Core;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class LanguagePreferenceServiceTests
{
    [Fact]
    public void SetLanguage_UsesDefault_WhenInputIsNullOrWhitespace()
    {
        var service = new LanguagePreferenceService();
        service.SetLanguage("en-US");

        service.SetLanguage("   ");

        Assert.Equal(LanguagePreferenceService.DefaultLanguage, service.SelectedLanguage);
    }

    [Fact]
    public void SetLanguage_UsesDefault_WhenLanguageIsUnsupported()
    {
        var service = new LanguagePreferenceService();

        service.SetLanguage("fr-FR");

        Assert.Equal(LanguagePreferenceService.DefaultLanguage, service.SelectedLanguage);
    }

    [Fact]
    public void SetLanguage_TrimsAndAppliesSupportedLanguage()
    {
        var service = new LanguagePreferenceService();

        service.SetLanguage("  es-ES  ");

        Assert.Equal("es-ES", service.SelectedLanguage);
    }

    [Fact]
    public void SetLanguage_RaisesChangedOnlyWhenLanguageActuallyChanges()
    {
        var service = new LanguagePreferenceService();
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.SetLanguage("en-US");
        service.SetLanguage("EN-us");

        Assert.Equal(1, changedCount);
    }
}


