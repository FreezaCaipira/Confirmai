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
using Microsoft.Extensions.Caching.Memory;

namespace Confirmai.Tests;

public class EfiBankEventPaymentGatewayTests
{
    [Fact]
    public void Name_ReturnsEfiBank()
    {
        var efiBank = CreateEfiBankService();
        var gateway = new EfiBankEventPaymentGateway(efiBank);

        Assert.Equal("EfiBank", gateway.Name);
    }

    [Fact]
    public void DisplayName_ReturnsPixEfiBank()
    {
        var efiBank = CreateEfiBankService();
        var gateway = new EfiBankEventPaymentGateway(efiBank);

        Assert.Equal("Pix · EfiBank", gateway.DisplayName);
    }

    [Fact]
    public void IsAvailable_ReturnsEfiBankIsEnabled()
    {
        var efiBank = CreateEfiBankService();
        var gateway = new EfiBankEventPaymentGateway(efiBank);

        // Since options are configured, IsEnabled should be true
        Assert.True(gateway.IsAvailable);
    }

    private static EfiBankPixService CreateEfiBankService()
    {
        var httpFactory = new StubHttpClientFactory(_ => throw new InvalidOperationException("Should not call HTTP"));
        var options = Options.Create(new EfiBankOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            CertificatePath = "test-cert.p12",
            PixKey = "test-pix-key"
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EfiBankPixService>.Instance;

        return new EfiBankPixService(httpFactory, options, cache, logger);
    }
}
