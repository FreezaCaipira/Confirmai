namespace Confirmai.Configuration;

/// <summary>
/// C40 F1 — configuracao geral do app (secao "App"). Hoje so carrega a URL
/// publica usada para montar links absolutos em emails/WhatsApp.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// URL publica do app (env <c>App__PublicBaseUrl</c>, ex.:
    /// https://confirmai.com). Vazio = emails/WhatsApp saem sem link —
    /// nunca se monta link relativo para fora do app.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
