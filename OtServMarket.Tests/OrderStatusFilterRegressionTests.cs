using Microsoft.EntityFrameworkCore;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Tests;

/// <summary>
/// Regression tests for the order status filter that was previously using the
/// IsPaid boolean proxy.  Now the filter uses PaymentStatus enum directly, so
/// AguardandoEntregaInGame / AguardandoRevisaoAdm orders are visible in the UI.
/// </summary>
public class OrderStatusFilterRegressionTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static AppDbContext CreateAndSeedDb()
    {
        var db = TestDataFactory.CreateDbContext();

        db.Users.AddRange(
            new ApplicationUser { Id = "buyer-filter", UserName = "BuyerFilter" },
            new ApplicationUser { Id = "seller-filter", UserName = "SellerFilter" }
        );

        db.Products.AddRange(
            new Product { Id = 10001, Name = "P1", Description = "d", Price = 0.01m, UserId = "seller-filter" },
            new Product { Id = 10002, Name = "P2", Description = "d", Price = 0.02m, UserId = "seller-filter" },
            new Product { Id = 10003, Name = "P3", Description = "d", Price = 0.03m, UserId = "seller-filter" },
            new Product { Id = 10004, Name = "P4", Description = "d", Price = 0.04m, UserId = "seller-filter" },
            new Product { Id = 10005, Name = "P5", Description = "d", Price = 0.05m, UserId = "seller-filter" },
            new Product { Id = 10006, Name = "P6", Description = "d", Price = 0.06m, UserId = "seller-filter" }
        );

        db.Orders.AddRange(
            new OrderModel { Id = 20001, BuyerId = "buyer-filter", SellerId = "seller-filter", ProductId = 10001, Amount = 0.01m, IsPaid = false, Status = PaymentStatus.Pendente },
            new OrderModel { Id = 20002, BuyerId = "buyer-filter", SellerId = "seller-filter", ProductId = 10002, Amount = 0.02m, IsPaid = true,  Status = PaymentStatus.Pago },
            new OrderModel { Id = 20003, BuyerId = "buyer-filter", SellerId = "seller-filter", ProductId = 10003, Amount = 0.03m, IsPaid = true,  Status = PaymentStatus.AguardandoEntregaInGame },
            new OrderModel { Id = 20004, BuyerId = "buyer-filter", SellerId = "seller-filter", ProductId = 10004, Amount = 0.04m, IsPaid = true,  Status = PaymentStatus.AguardandoRevisaoAdm },
            new OrderModel { Id = 20005, BuyerId = "buyer-filter", SellerId = "seller-filter", ProductId = 10005, Amount = 0.05m, IsPaid = true,  Status = PaymentStatus.Finalizado, FundsReleased = true },
            new OrderModel { Id = 20006, BuyerId = "buyer-filter", SellerId = "seller-filter", ProductId = 10006, Amount = 0.06m, IsPaid = false, Status = PaymentStatus.Cancelado }
        );

        db.SaveChanges();
        return db;
    }

    /// <summary>Simulates the new enum-based filter query used in AdminOrders.razor and MyOrders.razor.</summary>
    private static async Task<List<OrderModel>> ApplyStatusFilter(AppDbContext db, string filterStatus)
    {
        IQueryable<OrderModel> query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filterStatus) && Enum.TryParse<PaymentStatus>(filterStatus, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        return await query.OrderBy(o => o.Id).ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // No-filter: returns all orders
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_EmptyStatus_ReturnsAllOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "");
        Assert.Equal(6, result.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Key statuses that were invisible in the old boolean-proxy filter
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_AguardandoEntregaInGame_ReturnsPreciselyThoseOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "AguardandoEntregaInGame");

        Assert.Single(result);
        Assert.Equal(20003, result[0].Id);
        Assert.Equal(PaymentStatus.AguardandoEntregaInGame, result[0].Status);
    }

    [Fact]
    public async Task Filter_AguardandoRevisaoAdm_ReturnsPreciselyThoseOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "AguardandoRevisaoAdm");

        Assert.Single(result);
        Assert.Equal(20004, result[0].Id);
        Assert.Equal(PaymentStatus.AguardandoRevisaoAdm, result[0].Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Old boolean filter would have collapsed these — now each is independent
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_Pago_ReturnsOnlyPagoOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "Pago");

        Assert.Single(result);
        Assert.Equal(20002, result[0].Id);
        // Before the fix, filter=="paid" would have returned 20002, 20003, 20004, 20005
    }

    [Fact]
    public async Task Filter_Pendente_ReturnsOnlyPendenteOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "Pendente");

        Assert.Single(result);
        Assert.Equal(20001, result[0].Id);
        // Before the fix, filter=="pending" would have returned 20001 and 20006
    }

    [Fact]
    public async Task Filter_Finalizado_ReturnsOnlyFinalizadoOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "Finalizado");

        Assert.Single(result);
        Assert.Equal(20005, result[0].Id);
    }

    [Fact]
    public async Task Filter_Cancelado_ReturnsOnlyCanceladoOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "Cancelado");

        Assert.Single(result);
        Assert.Equal(20006, result[0].Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Guard: unknown/invalid status strings are silently ignored (not a crash)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_UnknownStatusString_ReturnsAllOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "paid"); // old string — no longer a valid enum name

        Assert.Equal(6, result.Count);
    }

    [Fact]
    public async Task Filter_PendingString_ReturnsAllOrders()
    {
        using var db = CreateAndSeedDb();
        var result = await ApplyStatusFilter(db, "pending"); // old string — no longer valid

        Assert.Equal(6, result.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Regression: the two critical statuses were previously lumped into "paid"
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Regression_OldPaidFilterWouldHaveReturnedFourOrders_NowEachHasItsOwnStatus()
    {
        using var db = CreateAndSeedDb();

        // Old behavior: "paid" filter → IsPaid == true → 4 orders (Pago, AguardandoEntregaInGame, AguardandoRevisaoAdm, Finalizado)
        var oldPaidOrders = await db.Orders.AsNoTracking()
            .Where(o => o.IsPaid)
            .OrderBy(o => o.Id)
            .ToListAsync();

        Assert.Equal(4, oldPaidOrders.Count);

        // New behavior: each sub-status is independently accessible
        var ingame = await ApplyStatusFilter(db, "AguardandoEntregaInGame");
        var review = await ApplyStatusFilter(db, "AguardandoRevisaoAdm");
        var finalizado = await ApplyStatusFilter(db, "Finalizado");
        var pago = await ApplyStatusFilter(db, "Pago");

        Assert.Single(ingame);
        Assert.Single(review);
        Assert.Single(finalizado);
        Assert.Single(pago);

        // Combined they equal the old "paid" count
        Assert.Equal(4, ingame.Count + review.Count + finalizado.Count + pago.Count);
    }
}
