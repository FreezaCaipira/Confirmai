using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

/// <summary>
/// Covers the 60s in-memory cache added in the home-page performance pass
/// for <see cref="TibiaServerService.GetAllAsync"/> and
/// <see cref="TibiaServerService.GetAllServerCardStatsAsync"/>.
/// </summary>
public class TibiaServerServiceCacheTests
{
    [Fact]
    public async Task GetAllAsync_CachesFirstResult_AndReturnsSameInstanceOnSecondCall()
    {
        using var db = TestDataFactory.CreateDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TibiaServerService(db, CreateEnvironment(), cache);

        db.Servers.Add(new TibiaServer { Name = "Cached", IsActive = true });
        await db.SaveChangesAsync();

        var first = await service.GetAllAsync();

        // Mutate DB directly behind the service's back: cache should
        // hide this change for the duration of the TTL.
        db.Servers.Add(new TibiaServer { Name = "Hidden", IsActive = true });
        await db.SaveChangesAsync();

        var second = await service.GetAllAsync();

        Assert.Same(first, second);
        Assert.Single(second);
        Assert.Equal("Cached", second[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_AlwaysHitsDb_WhenCacheIsNotConfigured()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new TibiaServerService(db, CreateEnvironment(), cache: null);

        db.Servers.Add(new TibiaServer { Name = "First", IsActive = true });
        await db.SaveChangesAsync();

        var first = await service.GetAllAsync();
        Assert.Single(first);

        db.Servers.Add(new TibiaServer { Name = "Second", IsActive = true });
        await db.SaveChangesAsync();

        var second = await service.GetAllAsync();
        Assert.Equal(2, second.Count);
    }

    [Fact]
    public async Task GetAllServerCardStatsAsync_CachesFirstResult()
    {
        using var db = TestDataFactory.CreateDbContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TibiaServerService(db, CreateEnvironment(), cache);

        db.Servers.Add(new TibiaServer { Id = 1, Name = "Alpha", IsActive = true });
        db.Orders.Add(new OrderModel
        {
            ServerId = 1,
            ProductId = 1,
            BuyerId = "b",
            SellerId = "s",
            Amount = 0.01m,
            IsPaid = true
        });
        await db.SaveChangesAsync();

        var first = await service.GetAllServerCardStatsAsync();

        // Add another order; cached result should NOT include it.
        db.Orders.Add(new OrderModel
        {
            ServerId = 1,
            ProductId = 2,
            BuyerId = "b2",
            SellerId = "s",
            Amount = 0.02m,
            IsPaid = true
        });
        await db.SaveChangesAsync();

        var second = await service.GetAllServerCardStatsAsync();

        Assert.Same(first, second);
        Assert.Equal(1, second[1].CompletedSales);
    }

    private static IWebHostEnvironment CreateEnvironment()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        return env.Object;
    }
}
