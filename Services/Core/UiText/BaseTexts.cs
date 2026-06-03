using System.Globalization;
using Confirmai.Services.User;

namespace Confirmai.Services.Core.UiText;

/// <summary>
/// Base class for UI text providers. Implements shared cache and retrieval logic.
/// All text providers inherit from this to access the text dictionary.
/// </summary>
internal abstract class BaseTexts
{
    /// <summary>
    /// Global text dictionary: language code → key → value
    /// Populated by subclasses and accessed by all providers
    /// </summary>
    protected static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> TextByLanguage { get; set; }
        = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get a text value by key and language, with optional formatting.
    /// Falls back to key name if not found.
    /// </summary>
    protected static string GetText(
        LanguagePreferenceService language,
        string key,
        params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var lang = language.SelectedLanguage;
        
        if (TextByLanguage.TryGetValue(lang, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }
        }

        // Fallback to pt-BR if current language not found
        if (TextByLanguage.TryGetValue("pt-BR", out var ptDict))
        {
            if (ptDict.TryGetValue(key, out var value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }
        }

        // Ultimate fallback: return key name
        return key;
    }

    /// <summary>
    /// Register the global text dictionary for all providers to use
    /// </summary>
    internal static void InitializeTexts(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> texts)
    {
        TextByLanguage = texts ?? TextByLanguage;
    }
}
