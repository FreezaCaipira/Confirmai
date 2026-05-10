using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class AdminOrderReleaseServiceIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminOrderReleaseServiceIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReleaseFundsAsync_WhenOrderIsAwaitingReview_FinalizesOrderAndWritesDetailedAuditLog()
    {
        var marker = Guid.NewGuid().ToString("N");
        var orderId = await SeedOrderAsync(
            buyerId: $"buyer-release-{marker}",
            sellerId: $"seller-release-{marker}",
            productName: $"Produto release {marker}",
            amount: 0.01000000m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        using (var setScope = _factory.Services.CreateScope())
        {
            var settings = setScope.ServiceProvider.GetRequiredService<AdminSettingsService>();
            var saved = await settings.SetOperationFeePercentAsync(2.5m);
            Assert.True(saved);
        }

        using (var releaseScope = _factory.Services.CreateScope())
        {
            var releaseService = releaseScope.ServiceProvider.GetRequiredService<AdminOrderReleaseService>();
            var result = await releaseService.ReleaseFundsAsync(orderId, adminUserId: $"admin-{marker}");
            Assert.True(result.Succeeded);
            Assert.Equal("Released", result.Reason);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedOrder = db.Orders.First(o => o.Id == orderId);
        Assert.Equal(PaymentStatus.Finalizado, updatedOrder.Status);
        Assert.True(updatedOrder.FundsReleased);

        var log = db.Logs
            .Where(l => l.Source == AdminAuditSources.OrdersReview && l.Message.Contains($"OrderId: {orderId}"))
            .OrderByDescending(l => l.Id)
            .FirstOrDefault();

        Assert.NotNull(log);
        Assert.Equal(AdminAuditLevels.Success, log!.Level);
        Assert.Equal($"admin-{marker}", log.UserId);
        Assert.Contains("StatusAnterior: AguardandoRevisaoAdm", log.Message);
        Assert.Contains("StatusNovo: Finalizado", log.Message);
        Assert.Contains("GrossBTC: 0.01", log.Message);
        Assert.Contains("FeePercent: 2.5", log.Message);
        Assert.Contains("FeeBTC: 0.00025", log.Message);
        Assert.Contains("NetBTC: 0.00975", log.Message);
    }

    [Fact]
    public async Task ReleaseFundsAsync_WhenStatusIsInvalid_WritesWarningAndDoesNotMutateOrder()
    {
        var marker = Guid.NewGuid().ToString("N");
        var orderId = await SeedOrderAsync(
            buyerId: $"buyer-invalid-{marker}",
            sellerId: $"seller-invalid-{marker}",
            productName: $"Produto invalid {marker}",
            amount: 0.005m,
            status: PaymentStatus.Finalizado);

        using (var releaseScope = _factory.Services.CreateScope())
        {
            var releaseService = releaseScope.ServiceProvider.GetRequiredService<AdminOrderReleaseService>();
            var result = await releaseService.ReleaseFundsAsync(orderId, adminUserId: $"admin-{marker}");
            Assert.False(result.Succeeded);
            Assert.Equal("InvalidStatus", result.Reason);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = db.Orders.First(o => o.Id == orderId);
        Assert.Equal(PaymentStatus.Finalizado, order.Status);

        var log = db.Logs
            .Where(l => l.Source == AdminAuditSources.OrdersReview && l.Message.Contains($"OrderId: {orderId}"))
            .OrderByDescending(l => l.Id)
            .FirstOrDefault();

        Assert.NotNull(log);
        Assert.Equal(AdminAuditLevels.Refused, log!.Level);
        Assert.Contains("status invalido", log.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseFundsAsync_WhenOrderDoesNotExist_WritesWarningAndReturnsOrderNotFound()
    {
        var marker = Guid.NewGuid().ToString("N");
        const int missingOrderId = 999999;

        using (var releaseScope = _factory.Services.CreateScope())
        {
            var releaseService = releaseScope.ServiceProvider.GetRequiredService<AdminOrderReleaseService>();
            var result = await releaseService.ReleaseFundsAsync(missingOrderId, adminUserId: $"admin-{marker}");
            Assert.False(result.Succeeded);
            Assert.Equal("OrderNotFound", result.Reason);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var log = db.Logs
            .Where(l => l.Source == AdminAuditSources.OrdersReview && l.Message.Contains($"OrderId: {missingOrderId}"))
            .OrderByDescending(l => l.Id)
            .FirstOrDefault();

        Assert.NotNull(log);
        Assert.Equal(AdminAuditLevels.Refused, log!.Level);
        Assert.Equal($"admin-{marker}", log.UserId);
        Assert.Contains("pedido inexistente", log.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseFundsAsync_WhenParticipantIsDeleted_ReturnsParticipantDeletedAndWritesLog()
    {
        var marker = Guid.NewGuid().ToString("N");
        var orderId = await SeedOrderAsync(
            buyerId: null,
            sellerId: $"seller-participant-deleted-{marker}",
            productName: $"Produto participante deletado {marker}",
            amount: 0.005m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        using (var releaseScope = _factory.Services.CreateScope())
        {
            var releaseService = releaseScope.ServiceProvider.GetRequiredService<AdminOrderReleaseService>();
            var result = await releaseService.ReleaseFundsAsync(orderId, adminUserId: $"admin-{marker}");

            Assert.False(result.Succeeded);
            Assert.Equal("ParticipantDeleted", result.Reason);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = db.Orders.First(o => o.Id == orderId);
        Assert.Equal(PaymentStatus.AguardandoRevisaoAdm, order.Status);
        Assert.False(order.FundsReleased);

        var log = db.Logs
            .Where(l => l.Source == AdminAuditSources.OrdersReview && l.Message.Contains($"OrderId: {orderId}"))
            .OrderByDescending(l => l.Id)
            .FirstOrDefault();

        Assert.NotNull(log);
        Assert.Equal(AdminAuditLevels.Refused, log!.Level);
        Assert.Contains("participante removido", log.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int> SeedOrderAsync(string? buyerId, string? sellerId, string productName, decimal amount, PaymentStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!string.IsNullOrEmpty(buyerId) && !db.Users.Any(u => u.Id == buyerId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = buyerId,
                UserName = buyerId,
                Email = $"{buyerId}@test.local"
            });
        }

        if (!string.IsNullOrEmpty(sellerId) && !db.Users.Any(u => u.Id == sellerId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = sellerId,
                UserName = sellerId,
                Email = $"{sellerId}@test.local"
            });
        }

        await db.SaveChangesAsync();

        var product = new Product
        {
            Name = productName,
            Description = "Descricao release",
            ShortDescription = "Resumo release",
            Price = amount,
            UserId = sellerId ?? string.Empty
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var order = new OrderModel
        {
            BuyerId = buyerId,
            SellerId = sellerId,
            ProductId = product.Id,
            Amount = amount,
            IsPaid = true,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order.Id;
    }
}


