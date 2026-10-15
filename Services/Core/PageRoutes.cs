using System.Text.RegularExpressions;

namespace Confirmai.Services.Core;

/// <summary>
/// C42-B: normaliza um path para o <b>template de rota</b> usado nos eventos
/// <c>page.view</c> — nunca a URL crua (cardeais como ids, códigos de convite
/// e emails iriam explodir a cardinalidade e vazar PII nos logs).
///
/// Regras por segmento:
///   - só dígitos            → "{id}"       (/grupo/123 → /grupo/{id})
///   - contém '@'            → "{email}"    (nunca persistir email)
///   - Guid                  → "{guid}"
///   - alfanum longo c/ letra+dígito → "{code}"  (código de convite, tokens)
///   - demais                → literal em minúsculas
/// </summary>
public static class PageRoutes
{
    private static readonly Regex Guidish =
        new("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-", RegexOptions.Compiled);

    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";

        var queryIndex = path.IndexOfAny(['?', '#']);
        if (queryIndex >= 0) path = path[..queryIndex];

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return "/";

        return "/" + string.Join('/', segments.Select(NormalizeSegment));
    }

    private static string NormalizeSegment(string segment)
    {
        if (segment.All(char.IsDigit)) return "{id}";
        if (segment.Contains('@')) return "{email}";
        if (Guid.TryParse(segment, out _) || Guidish.IsMatch(segment)) return "{guid}";
        if (segment.Length >= 6
            && segment.All(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            && (segment.Any(char.IsDigit) && segment.Any(char.IsLetter)
                || segment.All(char.IsUpper)))
            return "{code}";
        return segment.ToLowerInvariant();
    }
}
