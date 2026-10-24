namespace Confirmai.Configuration;

/// <summary>
/// C43-A C2 — card de divulgacao da coluna lateral. Sem Title o card
/// nao renderiza. Env: Promo__Title / Promo__Text / Promo__Url / Promo__ImageUrl.
/// </summary>
public sealed class PromoOptions
{
    public const string Section = "Promo";

    public string? Title { get; set; }
    public string? Text { get; set; }
    public string? Url { get; set; }
    /// <summary>Caminho local (ex.: /images/promo/x.webp): o CSP img-src so aceita 'self'.</summary>
    public string? ImageUrl { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Title);
}
