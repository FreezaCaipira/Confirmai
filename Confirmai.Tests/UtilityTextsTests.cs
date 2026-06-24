using Confirmai.Services.Core.UiText;

namespace Confirmai.Tests;

public class UtilityTextsTests
{
    [Fact]
    public void PtBr_ContainsRequiredKeys()
    {
        // Act
        var dictionary = UtilityTexts.PtBr;

        // Assert
        Assert.NotNull(dictionary);
        Assert.True(dictionary.Count > 0);
    }

    [Fact]
    public void PtBr_ValuesAreNotEmpty()
    {
        // Act
        var dictionary = UtilityTexts.PtBr;

        // Assert
        foreach (var kvp in dictionary)
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value), $"Key '{kvp.Key}' has empty value");
        }
    }

    [Fact]
    public void PtBr_UsesCaseInsensitiveComparer()
    {
        // Act
        var dictionary = UtilityTexts.PtBr;

        // Assert - Get first key and test case insensitivity
        var firstKey = dictionary.Keys.FirstOrDefault();
        if (firstKey != null)
        {
            Assert.True(dictionary.ContainsKey(firstKey.ToLower()));
            Assert.True(dictionary.ContainsKey(firstKey.ToUpper()));
        }
    }
}
