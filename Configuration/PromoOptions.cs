namespace Confirmai.Configuration;

/// <summary>
/// C43-A C2 — card de divulgacao da coluna lateral.
/// C46 (item 5): ganhou rotacao — <see cref="Items"/> lista os anuncios
/// configurados (o par Title/Text/Url/ImageUrl legado vira o item 0) e
/// <see cref="ShowDemo"/> liga os 3 anuncios de demonstracao em codigo
/// quando nada esta configurado.
/// Env: Promo__Title / Promo__Text / Promo__Url / Promo__ImageUrl,
/// Promo__Items__0__Title ..., Promo__ShowDemo=false.
/// </summary>
public sealed class PromoOptions
{
    public const string Section = "Promo";

    /// <summary>Item 0 legado (antes de Items existir).</summary>
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string? Url { get; set; }
    /// <summary>Caminho local (ex.: /images/promo/x.webp): o CSP img-src so aceita 'self'.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Anuncios configurados; nao substitui o item 0 legado — soma.</summary>
    public List<PromoItem> Items { get; set; } = new();

    /// <summary>Sem nenhum item configurado, mostra os demos. Default true.</summary>
    public bool ShowDemo { get; set; } = true;

    public bool IsConfigured => ConfiguredItems().Any();

    /// <summary>Anuncios configurados: item 0 legado + <see cref="Items"/> com Title.</summary>
    public IEnumerable<PromoItem> ConfiguredItems()
    {
        if (!string.IsNullOrWhiteSpace(Title))
        {
            yield return new PromoItem
            {
                Title = Title,
                Text = Text,
                Url = Url,
                ImageUrl = ImageUrl,
            };
        }
        foreach (var item in Items)
        {
            if (!string.IsNullOrWhiteSpace(item.Title))
            {
                yield return item;
            }
        }
    }
}

/// <summary>Um anuncio da rotacao do card lateral (C46 item 5).</summary>
public sealed class PromoItem
{
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string? Url { get; set; }
    /// <summary>Caminho local; o CSP img-src so aceita 'self'.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Rotulo do link. Vazio usa SidebarPromo.Open.</summary>
    public string? Cta { get; set; }
}
