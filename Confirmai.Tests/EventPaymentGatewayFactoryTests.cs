using Confirmai.Models;
using Confirmai.Services.Factories;
using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;

namespace Confirmai.Tests;

public class EventPaymentGatewayFactoryTests
{
    private sealed class FakeGateway : IEventPaymentGateway
    {
        public string Name { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public bool IsAvailable { get; init; }

        public Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId)
            => Task.FromResult(new EventPaymentChargeResult($"charge-{Name}", "brcode"));

        public Task<bool> IsChargePaidAsync(string chargeId) => Task.FromResult(false);
    }

    private static (EventPaymentGatewayFactory factory, GatewayService gatewayService)
        CreateSut(params FakeGateway[] gateways)
    {
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var gatewayService = new GatewayService(dbFactory);
        var factory = new EventPaymentGatewayFactory(gateways, gatewayService);
        return (factory, gatewayService);
    }

    private static async Task SeedGateway(GatewayService gatewayService, string name, bool enabled)
    {
        // GetAllAsync auto-seeds defaults; then we toggle the specific one
        await gatewayService.GetAllAsync();
        await gatewayService.SetStatusAsync(name, enabled);
    }

    // ── GetAvailableAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableAsync_ReturnsEmpty_WhenNoGateways()
    {
        var (factory, gatewayService) = CreateSut();
        await gatewayService.GetAllAsync(); // seed defaults

        var result = await factory.GetAvailableAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableAsync_ReturnsOnlyEnabledAndAvailable()
    {
        var efi = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank Pix", IsAvailable = true };
        var btc = new FakeGateway { Name = "BTCPayServer", DisplayName = "BTC Pay", IsAvailable = true };
        var disabled = new FakeGateway { Name = "Appmax", DisplayName = "Appmax", IsAvailable = true };
        var unavailable = new FakeGateway { Name = "Pix", DisplayName = "Pix Manual", IsAvailable = false };

        var (factory, gatewayService) = CreateSut(efi, btc, disabled, unavailable);
        await gatewayService.GetAllAsync();
        // EfiBank defaults to enabled, BTCPayServer defaults to disabled, Appmax defaults to disabled
        await gatewayService.SetStatusAsync("EfiBank", true);
        await gatewayService.SetStatusAsync("BTCPayServer", false);
        await gatewayService.SetStatusAsync("Appmax", false);

        var result = await factory.GetAvailableAsync();

        Assert.Single(result);
        Assert.Equal("EfiBank", result[0].Name);
    }

    [Fact]
    public async Task GetAvailableAsync_ReturnsSortedByName()
    {
        var z = new FakeGateway { Name = "ZGateway", DisplayName = "Z", IsAvailable = true };
        var a = new FakeGateway { Name = "AGateway", DisplayName = "A", IsAvailable = true };

        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        using (db)
        {
            db.Gateways.Add(new GatewayInfo { Name = "ZGateway", Enabled = true });
            db.Gateways.Add(new GatewayInfo { Name = "AGateway", Enabled = true });
            await db.SaveChangesAsync();
        }

        var gatewayService = new GatewayService(dbFactory);
        var factory = new EventPaymentGatewayFactory(new[] { z, a }, gatewayService);

        var result = await factory.GetAvailableAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("AGateway", result[0].Name);
        Assert.Equal("ZGateway", result[1].Name);
    }

    // ── GetGatewayAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetGatewayAsync_ReturnsGateway_WhenEnabledAndAvailable()
    {
        var efi = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank", IsAvailable = true };
        var (factory, gatewayService) = CreateSut(efi);
        await SeedGateway(gatewayService, "EfiBank", true);

        var result = await factory.GetGatewayAsync("EfiBank");

        Assert.NotNull(result);
        Assert.Equal("EfiBank", result.Name);
    }

    [Fact]
    public async Task GetGatewayAsync_IsCaseInsensitive()
    {
        var efi = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank", IsAvailable = true };
        var (factory, gatewayService) = CreateSut(efi);
        await SeedGateway(gatewayService, "EfiBank", true);

        var result = await factory.GetGatewayAsync("efibank");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetGatewayAsync_ReturnsNull_WhenNotEnabled()
    {
        var btc = new FakeGateway { Name = "BTCPayServer", DisplayName = "BTC", IsAvailable = true };
        var (factory, gatewayService) = CreateSut(btc);
        await SeedGateway(gatewayService, "BTCPayServer", false);

        var result = await factory.GetGatewayAsync("BTCPayServer");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGatewayAsync_ReturnsNull_WhenNotAvailable()
    {
        var gw = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank", IsAvailable = false };
        var (factory, gatewayService) = CreateSut(gw);
        await SeedGateway(gatewayService, "EfiBank", true);

        var result = await factory.GetGatewayAsync("EfiBank");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGatewayAsync_ReturnsNull_WhenNameNotFound()
    {
        var (factory, gatewayService) = CreateSut();
        await gatewayService.GetAllAsync();

        var result = await factory.GetGatewayAsync("NonExistent");

        Assert.Null(result);
    }

    // ── GetDefaultAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDefaultAsync_ReturnsNull_WhenNoAvailableGateways()
    {
        var (factory, gatewayService) = CreateSut();
        await gatewayService.GetAllAsync();

        var result = await factory.GetDefaultAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsFirstAvailable()
    {
        var efi = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank", IsAvailable = true };
        var (factory, gatewayService) = CreateSut(efi);
        await SeedGateway(gatewayService, "EfiBank", true);

        var result = await factory.GetDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("EfiBank", result.Name);
    }

    // ── GetByNameIgnoringToggle ──────────────────────────────────────────

    [Fact]
    public void GetByNameIgnoringToggle_ReturnsAvailableGateway_EvenIfToggleDisabled()
    {
        var gw = new FakeGateway { Name = "Appmax", DisplayName = "Appmax", IsAvailable = true };
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var gatewayService = new GatewayService(dbFactory);
        var factory = new EventPaymentGatewayFactory(new[] { gw }, gatewayService);

        var result = factory.GetByNameIgnoringToggle("Appmax");

        Assert.NotNull(result);
        Assert.Equal("Appmax", result.Name);
    }

    [Fact]
    public void GetByNameIgnoringToggle_IsCaseInsensitive()
    {
        var gw = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank", IsAvailable = true };
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var factory = new EventPaymentGatewayFactory(new[] { gw }, new GatewayService(dbFactory));

        var result = factory.GetByNameIgnoringToggle("efibank");

        Assert.NotNull(result);
    }

    [Fact]
    public void GetByNameIgnoringToggle_ReturnsNull_WhenNotAvailable()
    {
        var gw = new FakeGateway { Name = "EfiBank", DisplayName = "EfiBank", IsAvailable = false };
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var factory = new EventPaymentGatewayFactory(new[] { gw }, new GatewayService(dbFactory));

        var result = factory.GetByNameIgnoringToggle("EfiBank");

        Assert.Null(result);
    }

    // ── GetAllAvailableIgnoringToggle ────────────────────────────────────

    [Fact]
    public void GetAllAvailableIgnoringToggle_ReturnsOnlyAvailableOnes()
    {
        var available = new FakeGateway { Name = "A", DisplayName = "A", IsAvailable = true };
        var unavailable = new FakeGateway { Name = "B", DisplayName = "B", IsAvailable = false };
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var factory = new EventPaymentGatewayFactory(
            new IEventPaymentGateway[] { available, unavailable },
            new GatewayService(dbFactory));

        var result = factory.GetAllAvailableIgnoringToggle();

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    // ── GetConfiguredNames ───────────────────────────────────────────────

    [Fact]
    public void GetConfiguredNames_ReturnsCaseInsensitiveSet()
    {
        var gw = new FakeGateway { Name = "EfiBank", DisplayName = "E", IsAvailable = true };
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var factory = new EventPaymentGatewayFactory(new[] { gw }, new GatewayService(dbFactory));

        var result = factory.GetConfiguredNames();

        Assert.Contains("EfiBank", result);
        Assert.Contains("efibank", result);
        Assert.Contains("EFIBANK", result);
    }

    [Fact]
    public void GetConfiguredNames_ExcludesUnavailable()
    {
        var gw = new FakeGateway { Name = "Offline", DisplayName = "Off", IsAvailable = false };
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        var factory = new EventPaymentGatewayFactory(new[] { gw }, new GatewayService(dbFactory));

        var result = factory.GetConfiguredNames();

        Assert.DoesNotContain("Offline", result);
    }
}
