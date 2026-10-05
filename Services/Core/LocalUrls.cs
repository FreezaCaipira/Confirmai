namespace Confirmai.Services.Core;

/// <summary>
/// Equivalente Blazor do <c>Url.IsLocalUrl</c> das Razor Pages: so aceita
/// caminho relativo a raiz ("/x" ou "~/x"), nunca "//host" nem "/\host" —
/// evita open redirect quando o valor vem da query string.
/// </summary>
public static class LocalUrls
{
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return url.StartsWith('/')
            ? !url.StartsWith("//") && !url.StartsWith("/\\")
            : url.StartsWith("~/");
    }
}
