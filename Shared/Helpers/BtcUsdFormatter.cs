using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Shared.Helpers;

public static class BtcUsdFormatter
{
    public static string Format(decimal btcAmount, decimal? btcUsdRate)
    {
        return Format(btcAmount, btcUsdRate, null, "USD");
    }

    public static string Format(decimal btcAmount, decimal? btcUsdRate, decimal? btcBrlRate, string fiatCurrency)
    {
        var btcText = $"{btcAmount:0.########} BTC";

        var fiat = string.Equals(fiatCurrency, "BRL", StringComparison.OrdinalIgnoreCase) ? "BRL" : "USD";
        var selectedRate = fiat == "BRL" ? btcBrlRate : btcUsdRate;

        if (!selectedRate.HasValue || selectedRate.Value <= 0)
            return $"{btcText} ({fiat} indisponível no momento)";

        var fiatValue = btcAmount * selectedRate.Value;
        var culture = fiat == "BRL" ? CultureInfo.GetCultureInfo("pt-BR") : CultureInfo.GetCultureInfo("en-US");
        return $"{btcText} (~{fiatValue.ToString("C", culture)})";
    }

    public static MarkupString FormatMarkup(decimal btcAmount, decimal? btcUsdRate)
    {
        return FormatMarkup(btcAmount, btcUsdRate, null, "USD");
    }

    public static MarkupString FormatMarkup(decimal btcAmount, decimal? btcUsdRate, decimal? btcBrlRate, string fiatCurrency)
    {
        var btcText = $"{btcAmount:0.########} BTC";

        var fiat = string.Equals(fiatCurrency, "BRL", StringComparison.OrdinalIgnoreCase) ? "BRL" : "USD";
        var selectedRate = fiat == "BRL" ? btcBrlRate : btcUsdRate;

        if (!selectedRate.HasValue || selectedRate.Value <= 0)
            return new MarkupString($"{btcText} <span class=\"usd-unavailable-badge\" title=\"Não foi possível carregar a cotação {fiat} agora. Tente novamente em instantes.\">⚠️ {fiat} indisponível no momento</span>");

        var fiatValue = btcAmount * selectedRate.Value;
        var culture = fiat == "BRL" ? CultureInfo.GetCultureInfo("pt-BR") : CultureInfo.GetCultureInfo("en-US");
        var fiatText = fiatValue.ToString("C", culture);
        return new MarkupString($"{btcText} <span class=\"usd-estimate\">(~{fiatText})</span>");
    }
}

