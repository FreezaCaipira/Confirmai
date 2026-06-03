using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

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

