using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Services.EventPayments;
using Confirmai.Configuration;
using Confirmai.Data;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;

namespace Confirmai.Tests;

public class AppmaxEventPaymentGatewayTests
{
    [Fact]
    public void Name_ReturnsAppmax()
    {
        var appmax = CreateAppmaxService();
        var gateway = new AppmaxEventPaymentGateway(appmax);

        Assert.Equal("Appmax", gateway.Name);
    }

    [Fact]
    public void DisplayName_ReturnsPixAppmax()
    {
        var appmax = CreateAppmaxService();
        var gateway = new AppmaxEventPaymentGateway(appmax);

        Assert.Equal("Pix · Appmax", gateway.DisplayName);
    }

    [Fact]
    public void IsAvailable_ReturnsAppmaxIsEnabled()
    {
        var appmax = CreateAppmaxService();
        var gateway = new AppmaxEventPaymentGateway(appmax);

        // Since options are configured, IsEnabled should be true
        Assert.True(gateway.IsAvailable);
    }

    private static AppmaxPixService CreateAppmaxService()
    {
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var httpContextAccessor = new HttpContextAccessor();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new AppmaxOptions
        {
            ApiKey = "test-api-key",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            EnableEventCheckout = true
        });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AppmaxPixService>.Instance;

        return new AppmaxPixService(httpFactory, dbFactory, httpContextAccessor, cache, options, logger);
    }
}
