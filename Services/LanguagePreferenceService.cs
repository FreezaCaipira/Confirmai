namespace Confirmai.Services;

public sealed class LanguagePreferenceService
{
    public const string DefaultLanguage = "pt-BR";

    private static readonly HashSet<string> SupportedLanguageSet = new(StringComparer.OrdinalIgnoreCase)
    {
        "pt-BR",
        "en-US",
        "es-ES"
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

        if (!SupportedLanguageSet.Contains(normalized))
        {
            normalized = DefaultLanguage;
        }

        if (string.Equals(SelectedLanguage, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SelectedLanguage = normalized;
        Changed?.Invoke();
    }
}


