using Confirmai.Services.Core.UiText;

namespace Confirmai.Tests;

public class AuthTextsTests
{
    [Fact]
    public void PtBr_ContainsRequiredKeys()
    {
        // Act
        var dictionary = AuthTexts.PtBr;

        // Assert
        Assert.NotNull(dictionary);
        Assert.True(dictionary.Count > 0);
    }

    [Fact]
    public void PtBr_ValuesAreNotEmpty()
    {
        // Act
        var dictionary = AuthTexts.PtBr;

        // Assert
        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value), $"Key '{kvp.Key}' has empty value");
        }
    }

    [Fact]
    public void PtBr_ContainsAuthKeys()
    {
        // Act
        var dictionary = AuthTexts.PtBr;

        // Assert
        Assert.True(dictionary.ContainsKey("Identity.Login.Title"));
    }
}
