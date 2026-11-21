using Confirmai.Configuration;

namespace Confirmai.Tests;

/// <summary>
/// C46 (item 5): PromoOptions ganhou rotacao — Items + Title legado como
/// item 0 — e ShowDemo (default true) que liga os 3 demos sem config.
/// </summary>
public class PromoOptionsTests
{
    [Fact]
    public void IsConfigured_NoTitleNoItems_ReturnsFalse()
    {
        var options = new PromoOptions();

        Assert.False(options.IsConfigured);
        Assert.Empty(options.ConfiguredItems());
    }

    [Fact]
    public void IsConfigured_WhitespaceTitle_ReturnsFalse()
    {
        var options = new PromoOptions { Title = "   " };

        Assert.False(options.IsConfigured);
        Assert.Empty(options.ConfiguredItems());
    }

    [Fact]
    public void ConfiguredItems_LegacyTitle_BecomesItemZero()
    {
        var options = new PromoOptions
        {
            Title = "Legacy",
            Text = "texto",
            Url = "https://x",
            ImageUrl = "/images/promo/x.webp",
        };

        var items = options.ConfiguredItems().ToList();

        Assert.True(options.IsConfigured);
        Assert.Single(items);
        Assert.Equal("Legacy", items[0].Title);
        Assert.Equal("texto", items[0].Text);
        Assert.Equal("https://x", items[0].Url);
        Assert.Equal("/images/promo/x.webp", items[0].ImageUrl);
    }

    [Fact]
    public void ConfiguredItems_LegacyPlusItems_KeepsLegacyFirst()
    {
        var options = new PromoOptions
        {
            Title = "Legacy",
            Items = { new PromoItem { Title = "Novo" } },
        };

        var items = options.ConfiguredItems().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("Legacy", items[0].Title);
        Assert.Equal("Novo", items[1].Title);
    }

    [Fact]
    public void ConfiguredItems_SkipsItemsWithoutTitle()
    {
        var options = new PromoOptions
        {
            Items =
            {
                new PromoItem { Title = "  " },
                new PromoItem { Title = "Valido" },
            },
        };

        var items = options.ConfiguredItems().ToList();

        Assert.Single(items);
        Assert.Equal("Valido", items[0].Title);
    }

    [Fact]
    public void ShowDemo_DefaultsToTrue()
    {
        var options = new PromoOptions();

        Assert.True(options.ShowDemo);
    }
}
