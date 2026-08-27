using Confirmai.Services.Core;
namespace Confirmai.Services.User;

public sealed class LanguagePreferenceService
{
    public const string DefaultLanguage = "pt-BR";

    // Maps any casing to the canonical code. Callers pass values that come from
    // the `uiLang` query string, a cookie and localStorage, so "en-us" is a real
    // input; consumers that switch on the exact code (UiTextService.FormatDateTime)
    // would otherwise silently fall back to pt-BR.
    private static readonly Dictionary<string, string> CanonicalLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt-BR"] = "pt-BR",
        ["en-US"] = "en-US",
        ["es-ES"] = "es-ES"
    };

    public event Action? Changed;

    public string SelectedLanguage { get; private set; } = DefaultLanguage;

    public IReadOnlyList<(string Code, string Label)> SupportedLanguages { get; } =
    [
        ("pt-BR", "Portugues (Brasil)"),
        ("en-US", "English (US)"),
        ("es-ES", "Espanol")
    ];

    public void SetLanguage(string? language)
    {
        var normalized = string.IsNullOrWhiteSpace(language)
            ? DefaultLanguage
            : language.Trim();

        if (!CanonicalLanguages.TryGetValue(normalized, out var canonical))
        {
            canonical = DefaultLanguage;
        }

        normalized = canonical;

        if (string.Equals(SelectedLanguage, normalized, StringComparison.Ordinal))
        {
            return;
        }

        SelectedLanguage = normalized;
        Changed?.Invoke();
    }
}
