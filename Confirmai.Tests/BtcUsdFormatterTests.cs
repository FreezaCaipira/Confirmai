using Confirmai.Shared.Helpers;

namespace Confirmai.Tests;

public class BtcUsdFormatterTests
{
    [Fact]
    public void Format_ShowsUnavailableIndicator_WhenUsdRateIsMissing()
    {
        var formatted = BtcUsdFormatter.Format(0.001m, null);

        Assert.Contains("0.001 BTC", formatted);
        Assert.Contains("USD indisponível no momento", formatted);
    }

    [Fact]
    public void Format_ShowsUsdEstimate_WhenUsdRateExists()
    {
        var formatted = BtcUsdFormatter.Format(0.001m, 100000m);

        Assert.Contains("0.001 BTC", formatted);
        Assert.Contains("~$100.00", formatted);
    }

    [Fact]
    public void FormatMarkup_ShowsUnavailableBadge_WhenUsdRateIsMissing()
    {
        var markup = BtcUsdFormatter.FormatMarkup(0.001m, null).Value;

        Assert.Contains("0.001 BTC", markup);
        Assert.Contains("usd-unavailable-badge", markup);
        Assert.Contains("⚠ USD indisponível no momento", markup);
        Assert.Contains("title=\"Não foi possível carregar a cotação USD agora. Tente novamente em instantes.\"", markup);
    }

    [Fact]
    public void FormatMarkup_ShowsEstimateClass_WhenUsdRateExists()
    {
        var markup = BtcUsdFormatter.FormatMarkup(0.001m, 100000m).Value;

        Assert.Contains("0.001 BTC", markup);
        Assert.Contains("usd-estimate", markup);
        Assert.Contains("~$100.00", markup);
    }

    [Fact]
    public void Format_SupportsBrl_WhenSelected()
    {
        var formatted = BtcUsdFormatter.Format(0.001m, btcUsdRate: 100000m, btcBrlRate: 500000m, fiatCurrency: "BRL");

        Assert.Contains("0.001 BTC", formatted);
        Assert.Contains("R$", formatted);
        Assert.Contains("500,00", formatted);
    }
}
