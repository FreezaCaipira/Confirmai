using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class DashboardMetricsServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_ReturnsAggregatedMetrics()
    {
        using var db = TestDataFactory.CreateDbContext();

        db.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "user1", Email = "user1@test.local" },
            new ApplicationUser { Id = "u2", UserName = "user2", Email = "user2@test.local" });

        db.Orders.AddRange(
            new OrderModel { BuyerId = "u1", SellerId = "u2", ProductId = 1, Amount = 0.01m, IsPaid = true, Status = PaymentStatus.Pago },
            new OrderModel { BuyerId = "u1", SellerId = "u2", ProductId = 2, Amount = 0.02m, IsPaid = true, Status = PaymentStatus.Finalizado },
            new OrderModel { BuyerId = "u2", SellerId = "u1", ProductId = 3, Amount = 0.03m, IsPaid = false, Status = PaymentStatus.AguardandoRevisaoAdm, FundsReleased = false },
            new OrderModel { BuyerId = "u2", SellerId = "u1", ProductId = 4, Amount = 0.04m, IsPaid = false, Status = PaymentStatus.Cancelado, FundsReleased = false });

        db.Logs.AddRange(
            new AppLog { Source = "Quote", Message = "quote call" },
            new AppLog { Source = "CryptoQuote", Message = "quote call" },
            new AppLog { Source = "Payment", Message = "other" });

        await db.SaveChangesAsync();

        var service = new DashboardMetricsService(db);
        var snapshot = await service.GetSnapshotAsync();

        Assert.IsType<DashboardMetricsSnapshot>(snapshot);
        Assert.Equal(2, snapshot.UsersCount);
        Assert.Equal(2, snapshot.PaidSalesCount);
        Assert.Equal(0.03m, snapshot.PaidSalesVolumeBtc);
        Assert.Equal(1, snapshot.PendingOrdersCount);
        Assert.Equal(2, snapshot.QuoteQueriesCount);
    }
}

