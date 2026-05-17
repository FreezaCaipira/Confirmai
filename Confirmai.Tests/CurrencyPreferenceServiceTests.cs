using Confirmai.Services;

namespace Confirmai.Tests;

public class CurrencyPreferenceServiceTests
{
    [Fact]
    public void DefaultsToUsd()
    {
        var service = new CurrencyPreferenceService();

        Assert.Equal("USD", service.SelectedFiatCurrency);
    }

    [Fact]
    public void SetCurrency_AllowsBrl_AndFallsBackToUsd()
    {
        var service = new CurrencyPreferenceService();

        service.SetCurrency("BRL");
        Assert.Equal("BRL", service.SelectedFiatCurrency);

        service.SetCurrency("EUR");
        Assert.Equal("USD", service.SelectedFiatCurrency);
    }
}

