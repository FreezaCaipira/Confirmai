using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class GatewayServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsGateways()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        using (db)
        {
            db.Gateways.AddRange(
                new GatewayInfo { Name = "BTCPayServer", Enabled = true },
                new GatewayInfo { Name = "Testnet", Enabled = false });
            await db.SaveChangesAsync();

            var service = new GatewayService(factory);
            var gateways = await service.GetAllAsync();

            // GetAllAsync auto-seeds defaults (BTCPayServer, Testnet, Pix, EfiBank) then merges
            Assert.Equal(4, gateways.Count);
        }
    }

    [Fact]
    public async Task SetStatusAsync_Updates_WhenGatewayExists_AndReturnsFalseOtherwise()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        using (db)
        {
            db.Gateways.Add(new GatewayInfo { Name = "Testnet", Enabled = false });
            await db.SaveChangesAsync();

            var service = new GatewayService(factory);

            var updated = await service.SetStatusAsync("Testnet", enabled: true);
            var missing = await service.SetStatusAsync("Unknown", enabled: true);

            Assert.True(updated);
            Assert.False(missing);

            await using var verifyDb = factory.CreateDbContext();
            Assert.True((await verifyDb.Gateways.FindAsync("Testnet"))!.Enabled);
        }
    }
}

