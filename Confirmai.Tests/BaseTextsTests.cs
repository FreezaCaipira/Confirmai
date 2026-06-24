using Confirmai.Services.Core.UiText;
using Confirmai.Services.User;

namespace Confirmai.Tests;

public class BaseTextsTests
{
    [Fact]
    public void GetText_ReturnsEmpty_WhenKeyIsWhitespace()
    {
        var language = new LanguagePreferenceService();

        var method = typeof(BaseTexts).GetMethod("GetText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { language, "   ", Array.Empty<object>() });

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetText_ReturnsKey_WhenTextNotFound()
    {
        var language = new LanguagePreferenceService();

        var method = typeof(BaseTexts).GetMethod("GetText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { language, "nonexistent.key", Array.Empty<object>() });

        Assert.Equal("nonexistent.key", result);
    }

    [Fact]
    public void GetText_ReturnsFormattedValue_WhenArgsProvided()
    {
        var language = new LanguagePreferenceService();

        var texts = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["pt-BR"] = new Dictionary<string, string>
            {
                ["test.key"] = "Hello {0}"
            }
        };

        var initMethod = typeof(BaseTexts).GetMethod("InitializeTexts",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        initMethod!.Invoke(null, new object[] { texts });

        var method = typeof(BaseTexts).GetMethod("GetText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var result = method!.Invoke(null, new object[] { language, "test.key", new object[] { "World" } });

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void GetText_FallsBackToPtBR_WhenLanguageNotFound()
    {
        var language = new LanguagePreferenceService();
        language.SetLanguage("en-US");

        var texts = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["pt-BR"] = new Dictionary<string, string>
            {
                ["test.key"] = "Olá"
            }
        };

        var initMethod = typeof(BaseTexts).GetMethod("InitializeTexts",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        initMethod!.Invoke(null, new object[] { texts });

        var method = typeof(BaseTexts).GetMethod("GetText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var result = method!.Invoke(null, new object[] { language, "test.key", Array.Empty<object>() });

        Assert.Equal("Olá", result);
    }

    [Fact]
    public void InitializeTexts_SetsTextByLanguage()
    {
        var texts = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["pt-BR"] = new Dictionary<string, string>
            {
                ["test.key"] = "Valor"
            }
        };

        var method = typeof(BaseTexts).GetMethod("InitializeTexts",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        method!.Invoke(null, new object[] { texts });

        // Verify the texts were set by trying to get a value
        var language = new LanguagePreferenceService();

        var getMethod = typeof(BaseTexts).GetMethod("GetText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var result = getMethod!.Invoke(null, new object[] { language, "test.key", Array.Empty<object>() });

        Assert.Equal("Valor", result);
    }

    [Fact]
    public void InitializeTexts_DoesNotReplace_WhenNullProvided()
    {
        var texts = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["pt-BR"] = new Dictionary<string, string>
            {
                ["test.key"] = "Valor"
            }
        };

        var initMethod = typeof(BaseTexts).GetMethod("InitializeTexts",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        initMethod!.Invoke(null, new object[] { texts });

        // Try to set null
        initMethod!.Invoke(null, new object[] { (IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>)null! });

        // Verify the original texts are still there
        var language = new LanguagePreferenceService();

        var getMethod = typeof(BaseTexts).GetMethod("GetText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var result = getMethod!.Invoke(null, new object[] { language, "test.key", Array.Empty<object>() });

        Assert.Equal("Valor", result);
    }
}
