using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class AdminOrdersReviewIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminOrdersReviewIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OrdersReview_DefaultSortByAmountDesc_LoadedFromDb_ReturnsExpectedSequence()
    {
        _ = await SeedOrderAsync(
            buyerId: "buyer-review-1",
            sellerId: "seller-review-1",
            productName: "Produto review alto",
            amount: 0.003m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        _ = await SeedOrderAsync(
            buyerId: "buyer-review-2",
            sellerId: "seller-review-2",
            productName: "Produto review medio",
            amount: 0.002m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        _ = await SeedOrderAsync(
            buyerId: "buyer-review-3",
            sellerId: "seller-review-3",
            productName: "Produto review baixo",
            amount: 0.001m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        _ = await SeedOrderAsync(
            buyerId: "buyer-review-hidden",
            sellerId: "seller-review-hidden",
            productName: "Produto review oculto",
            amount: 0.050m,
            status: PaymentStatus.Finalizado);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var allOrdersCount = await db.Orders.CountAsync();
        Assert.True(allOrdersCount >= 4, $"Expected at least 4 seeded orders, got {allOrdersCount}.");

        var reviewOrders = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Product)
            .Where(o => o.Status == PaymentStatus.AguardandoRevisaoAdm)
            .Where(o => o.SellerId != null && o.SellerId.StartsWith("seller-review-"))
            .ToListAsync();

        Assert.Equal(3, reviewOrders.Count);

        var sorted = OrderReviewSorting
            .Apply(reviewOrders, OrderReviewSortColumn.Amount, sortAscending: false)
            .Select(o => o.Product!.Name)
            .ToArray();

        Assert.Equal(new[]
        {
            "Produto review alto",
            "Produto review medio",
            "Produto review baixo"
        }, sorted);

        Assert.DoesNotContain("Produto review oculto", sorted);
    }

    [Fact]
    public async Task OrdersReview_DefaultSortByAmountDesc_WhenAmountsTie_UsesBuyerProductAndIdTieBreakers()
    {
        var orderZ = await SeedOrderAsync(
            buyerId: "ana-review-1",
            sellerId: "seller-tie-1",
            productName: "Produto Z",
            amount: 0.005m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderAFirst = await SeedOrderAsync(
            buyerId: "ana-review-1",
            sellerId: "seller-tie-2",
            productName: "Produto A",
            amount: 0.005m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderBruno = await SeedOrderAsync(
            buyerId: "bruno-review-1",
            sellerId: "seller-tie-3",
            productName: "Produto B",
            amount: 0.005m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderASecond = await SeedOrderAsync(
            buyerId: "ana-review-1",
            sellerId: "seller-tie-4",
            productName: "Produto A",
            amount: 0.005m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reviewOrders = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Product)
            .Where(o => o.Status == PaymentStatus.AguardandoRevisaoAdm)
            .Where(o => o.Amount == 0.005m)
            .Where(o => o.SellerId != null && o.SellerId.StartsWith("seller-tie-"))
            .ToListAsync();

        Assert.Equal(4, reviewOrders.Count);

        var sortedIds = OrderReviewSorting
            .Apply(reviewOrders, OrderReviewSortColumn.Amount, sortAscending: false)
            .Select(o => o.Id)
            .ToArray();

        Assert.Equal(new[] { orderAFirst, orderASecond, orderZ, orderBruno }, sortedIds);
    }

    [Fact]
    public async Task OrdersReview_SortByBuyerAsc_WhenBuyerTies_UsesAmountDescAndIdTieBreakers()
    {
        var orderAnaLow = await SeedOrderAsync(
            buyerId: "buyer-sort-ana",
            sellerId: "seller-buyer-tie-1",
            productName: "Produto buyer tie 1",
            amount: 0.0071m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderAnaHighFirst = await SeedOrderAsync(
            buyerId: "buyer-sort-ana",
            sellerId: "seller-buyer-tie-2",
            productName: "Produto buyer tie 2",
            amount: 0.0079m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderBruno = await SeedOrderAsync(
            buyerId: "buyer-sort-bruno",
            sellerId: "seller-buyer-tie-3",
            productName: "Produto buyer tie 3",
            amount: 0.0075m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderAnaHighSecond = await SeedOrderAsync(
            buyerId: "buyer-sort-ana",
            sellerId: "seller-buyer-tie-4",
            productName: "Produto buyer tie 4",
            amount: 0.0079m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reviewOrders = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Product)
            .Where(o => o.Status == PaymentStatus.AguardandoRevisaoAdm)
            .Where(o => o.Amount >= 0.007m && o.Amount < 0.008m)
            .Where(o => o.SellerId != null && o.SellerId.StartsWith("seller-buyer-tie-"))
            .ToListAsync();

        Assert.Equal(4, reviewOrders.Count);

        var sortedIds = OrderReviewSorting
            .Apply(reviewOrders, OrderReviewSortColumn.Buyer, sortAscending: true)
            .Select(o => o.Id)
            .ToArray();

        Assert.Equal(new[] { orderAnaHighFirst, orderAnaHighSecond, orderAnaLow, orderBruno }, sortedIds);
    }

    [Fact]
    public async Task OrdersReview_SortByProductDesc_WhenProductTies_UsesBuyerAmountAndIdTieBreakers()
    {
        var orderZenAnaLow = await SeedOrderAsync(
            buyerId: "buyer-product-ana",
            sellerId: "seller-product-tie-1",
            productName: "Produto Zen",
            amount: 0.0091m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderZenAnaHighFirst = await SeedOrderAsync(
            buyerId: "buyer-product-ana",
            sellerId: "seller-product-tie-2",
            productName: "Produto Zen",
            amount: 0.0099m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderZenBruno = await SeedOrderAsync(
            buyerId: "buyer-product-bruno",
            sellerId: "seller-product-tie-3",
            productName: "Produto Zen",
            amount: 0.0095m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderZenAnaHighSecond = await SeedOrderAsync(
            buyerId: "buyer-product-ana",
            sellerId: "seller-product-tie-4",
            productName: "Produto Zen",
            amount: 0.0099m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        var orderA = await SeedOrderAsync(
            buyerId: "buyer-product-zz",
            sellerId: "seller-product-tie-5",
            productName: "Produto A",
            amount: 0.0098m,
            status: PaymentStatus.AguardandoRevisaoAdm);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reviewOrders = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Product)
            .Where(o => o.Status == PaymentStatus.AguardandoRevisaoAdm)
            .Where(o => o.Amount >= 0.009m && o.Amount < 0.010m)
            .Where(o => o.SellerId != null && o.SellerId.StartsWith("seller-product-tie-"))
            .ToListAsync();

        Assert.Equal(5, reviewOrders.Count);

        var sortedIds = OrderReviewSorting
            .Apply(reviewOrders, OrderReviewSortColumn.Product, sortAscending: false)
            .Select(o => o.Id)
            .ToArray();

        Assert.Equal(new[] { orderZenAnaHighFirst, orderZenAnaHighSecond, orderZenAnaLow, orderZenBruno, orderA }, sortedIds);
    }

    private async Task<int> SeedOrderAsync(string buyerId, string sellerId, string productName, decimal amount, PaymentStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Users.AnyAsync(u => u.Id == buyerId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = buyerId,
                UserName = buyerId,
                Email = $"{buyerId}@test.local"
            });
        }

        if (!await db.Users.AnyAsync(u => u.Id == sellerId))
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
            Description = "Descricao pedido review",
            ShortDescription = "Resumo review",
            Price = amount,
            UserId = sellerId
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

    // ── Regression: novos status de pedido nao aparecem na fila de revisao ──

    [Fact]
    public async Task OrdersReview_ExcludesAguardandoEntregaInGame_Orders()
    {
        // Arrange: seed one order in each status, only AguardandoRevisaoAdm should appear
        var reviewId = await SeedOrderAsync("buyer-rgr-1", "seller-rgr-1", "Item Em Revisao", 0.01m, PaymentStatus.AguardandoRevisaoAdm);
        _ = await SeedOrderAsync("buyer-rgr-2", "seller-rgr-2", "Item Aguardando InGame", 0.01m, PaymentStatus.AguardandoEntregaInGame);
        _ = await SeedOrderAsync("buyer-rgr-3", "seller-rgr-3", "Item Em Disputa", 0.01m, PaymentStatus.Disputa);
        _ = await SeedOrderAsync("buyer-rgr-4", "seller-rgr-4", "Item Finalizado", 0.01m, PaymentStatus.Finalizado);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // This is the exact query the AdminOrdersReview page uses
        var reviewOrders = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Product)
            .Include(o => o.Payment)
            .Where(o => o.Status == PaymentStatus.AguardandoRevisaoAdm)
            .ToListAsync();

        Assert.Contains(reviewOrders, o => o.Id == reviewId);
        Assert.DoesNotContain(reviewOrders, o => o.SellerId == "seller-rgr-2");
        Assert.DoesNotContain(reviewOrders, o => o.SellerId == "seller-rgr-3");
        Assert.DoesNotContain(reviewOrders, o => o.SellerId == "seller-rgr-4");
    }

    [Fact]
    public async Task OrdersReview_ExcludesDisputa_Orders()
    {
        _ = await SeedOrderAsync("buyer-dis-1", "seller-dis-1", "Disputa Item A", 0.02m, PaymentStatus.Disputa);
        _ = await SeedOrderAsync("buyer-dis-2", "seller-dis-2", "Disputa Item B", 0.02m, PaymentStatus.Disputa);
        var reviewId = await SeedOrderAsync("buyer-dis-3", "seller-dis-3", "Review Item", 0.02m, PaymentStatus.AguardandoRevisaoAdm);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reviewOrders = await db.Orders
            .Where(o => o.Status == PaymentStatus.AguardandoRevisaoAdm)
            .ToListAsync();

        Assert.Contains(reviewOrders, o => o.Id == reviewId);
        Assert.DoesNotContain(reviewOrders, o => o.SellerId == "seller-dis-1");
        Assert.DoesNotContain(reviewOrders, o => o.SellerId == "seller-dis-2");
    }

}


