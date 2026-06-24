using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Services.EventPayments;
using Confirmai.Configuration;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

public class AbacatePayEventPaymentGatewayTests
{
    [Fact]
    public void Name_ReturnsPix()
    {
        var abacatePay = CreateAbacatePayService();
        var gateway = new AbacatePayEventPaymentGateway(abacatePay);

        Assert.Equal("Pix", gateway.Name);
    }

    [Fact]
    public void DisplayName_ReturnsPixAbacatePay()
    {
        var abacatePay = CreateAbacatePayService();
        var gateway = new AbacatePayEventPaymentGateway(abacatePay);

        Assert.Equal("Pix · AbacatePay", gateway.DisplayName);
    }

    [Fact]
    public void IsAvailable_ReturnsAbacatePayIsEnabled()
    {
        var abacatePay = CreateAbacatePayService();
        var gateway = new AbacatePayEventPaymentGateway(abacatePay);

        // Since options are configured, IsEnabled should be true
        Assert.True(gateway.IsAvailable);
    }

    private static AbacatePayPixService CreateAbacatePayService()
    {
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var options = Options.Create(new AbacatePayOptions
        {
            ApiKey = "test-api-key"
        });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AbacatePayPixService>.Instance;

        return new AbacatePayPixService(httpFactory, options, logger);
    }
}
