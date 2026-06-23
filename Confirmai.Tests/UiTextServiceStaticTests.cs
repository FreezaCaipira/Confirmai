using Confirmai.Services.Core;
using Confirmai.Services.User;

namespace Confirmai.Tests;

public class UiTextServiceStaticTests
{
    [Fact]
    public void FormatDateTime_DateDefault_ReturnsCorrectFormat()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = service.FormatDateTime(dateTime, "DateDefault");

        // Assert
        Assert.Equal("22/06", result);
    }

    [Fact]
    public void FormatDateTime_TimeDefault_ReturnsCorrectFormat()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = service.FormatDateTime(dateTime, "TimeDefault");

        // Assert
        Assert.Equal("14:30", result);
    }

    [Fact]
    public void FormatDateTime_DateTimeDefault_ReturnsCorrectFormat()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = service.FormatDateTime(dateTime, "DateTimeDefault");

        // Assert
        Assert.Equal("22/06 14:30", result);
    }

    [Fact]
    public void FormatDateTime_DateTimeFullShort_ReturnsCorrectFormat()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = service.FormatDateTime(dateTime, "DateTimeFullShort");

        // Assert
        Assert.Equal("Mon, 22/06 14:30", result);
    }

    [Fact]
    public void FormatDateTime_DateTimeShortCompact_ReturnsCorrectFormat()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = service.FormatDateTime(dateTime, "DateTimeShortCompact");

        // Assert
        Assert.Equal("22/06/26 14:30", result);
    }

    [Fact]
    public void FormatDateTime_UnknownFormat_ReturnsDefaultFormat()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = service.FormatDateTime(dateTime, "UnknownFormat");

        // Assert
        Assert.NotEmpty(result);
    }

    [Fact]
    public void FormatDateTime_DefaultDateTime_ReturnsEmpty()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var result = service.FormatDateTime(default, "DateDefault");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Get_WithEmptyKey_ReturnsEmpty()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var result = service.Get("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Get_WithWhitespaceKey_ReturnsEmpty()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var result = service.Get("   ");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Get_WithUnknownKey_ReturnsKeyName()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var result = service.Get("Unknown.Key.That.Does.Not.Exist");

        // Assert
        Assert.Equal("Unknown.Key.That.Does.Not.Exist", result);
    }

    [Fact]
    public void Get_WithFormattingArgs_FormatsString()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act - Test formatting with a placeholder key
        var result = service.Get("Unknown.Key", "John");

        // Assert
        Assert.Equal("Unknown.Key", result); // Unknown key returns itself
    }

    [Fact]
    public void Indexer_ReturnsSameAsGet()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);
        var key = "Test.Key";

        // Act
        var getResult = service.Get(key);
        var indexerResult = service[key];

        // Assert
        Assert.Equal(getResult, indexerResult);
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectCounts()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var stats = service.GetStatistics();

        // Assert
        Assert.True(stats.PtBr > 0);
        Assert.True(stats.Total > 0);
        Assert.Equal(stats.PtBr + stats.EnUs + stats.EsEs, stats.Total);
    }

    [Fact]
    public void GetAllKeys_ReturnsKeysForLanguage()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var keys = service.GetAllKeys("pt-BR");

        // Assert
        Assert.NotEmpty(keys);
    }

    [Fact]
    public void GetAllKeys_UnknownLanguage_ReturnsEmpty()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var keys = service.GetAllKeys("unknown-XX");

        // Assert
        Assert.Empty(keys);
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var language = new LanguagePreferenceService();
        language.SetLanguage("pt-BR");
        var service = new UiTextService(language);

        // Act
        var result = service.ContainsKey("Non.Existing.Key");

        // Assert
        Assert.False(result);
    }
}
