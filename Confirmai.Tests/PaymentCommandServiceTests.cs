using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class PaymentCommandServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, PaymentCommandService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"pcmd-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var adminSettingsService = new AdminSettingsService(factory);
        var paymentFactory = new BitcoinPaymentFactory(
            new List<Confirmai.Services.Interfaces.IBitcoinPaymentService>());
        var confirmationService = new PaymentConfirmationService(
            factory, paymentFactory, logService,
            Mock.Of<Microsoft.AspNetCore.SignalR.IHubContext<Hubs.PaymentHub>>(),
            new PaymentEventBus());
        var svc = new PaymentCommandService(factory, paymentFactory, confirmationService, logService, adminSettingsService);
        return (factory, svc);
    }

    private static async Task<int> SeedProductAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var db = factory.CreateDbContext();
        var product = new Product { Name = "Test", Description = "Test Description", Price = 10m, UserId = "seller-1" };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    [Fact]
    public async Task SavePaymentRecordAsync_PersistsRecord()
    {
        var (factory, svc) = Setup();
        var productId = await SeedProductAsync(factory);

        var record = new PaymentRecord
        {
            ProductId = productId,
            Address = "tb1qtest",
            PaymentId = "pay-1",
            Amount = 10m,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow,
            PaymentMethod = "Testnet",
            UserId = "buyer-1"
        };

        await svc.SavePaymentRecordAsync(record);

        await using var db2 = factory.CreateDbContext();
        var saved = await db2.Payments.FirstOrDefaultAsync(p => p.PaymentId == "pay-1");
        Assert.NotNull(saved);
        Assert.Equal("tb1qtest", saved!.Address);
    }

    [Fact]
    public async Task FindPaymentByAddressAsync_ReturnsPayment_WhenExists()
    {
        var (factory, svc) = Setup();
        var productId = await SeedProductAsync(factory);
        await using var db = factory.CreateDbContext();
        db.Payments.Add(new PaymentRecord
        {
            ProductId = productId,
            Address = "tb1qfound",
            PaymentId = "pay-2",
            Amount = 10m,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow,
            PaymentMethod = "Testnet"
        });
        await db.SaveChangesAsync();

        var result = await svc.FindPaymentByAddressAsync("tb1qfound");

        Assert.NotNull(result);
        Assert.Equal("pay-2", result!.PaymentId);
    }

    [Fact]
    public async Task FindPaymentByAddressAsync_ReturnsNull_WhenNotFound()
    {
        var (factory, svc) = Setup();

        var result = await svc.FindPaymentByAddressAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindPaymentByAddressAsync_TrimsAddress()
    {
        var (factory, svc) = Setup();
        var productId = await SeedProductAsync(factory);
        await using var db = factory.CreateDbContext();
        db.Payments.Add(new PaymentRecord
        {
            ProductId = productId,
            Address = "tb1qtrim",
            PaymentId = "pay-3",
            Amount = 10m,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow,
            PaymentMethod = "Testnet"
        });
        await db.SaveChangesAsync();

        var result = await svc.FindPaymentByAddressAsync("  tb1qtrim  ");

        Assert.NotNull(result);
        Assert.Equal("pay-3", result!.PaymentId);
    }
}
