using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminPaymentsQueryServiceTests
{
    private static (AppDbContext db, AdminPaymentsQueryService service) CreateService()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var exportService = new AdminLogsExportService();
        var service = new AdminPaymentsQueryService(dbFactory, exportService);

        return (db, service);
    }

    private static async Task SeedPaymentsAsync(AppDbContext db)
    {
        var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
        var user2 = new ApplicationUser { Id = "user2", UserName = "bob" };

        db.Users.Add(user1);
        db.Users.Add(user2);

        for (int i = 1; i <= 25; i++)
        {
            db.Payments.Add(new PaymentRecord
            {
                Id = i,
                PaymentId = $"pay-{i}",
                UserId = i % 2 == 0 ? "user2" : "user1",
                User = i % 2 == 0 ? user2 : user1,
                Amount = 10m * i,
                IsPaid = i % 3 == 0,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        await db.SaveChangesAsync();
    }

    // ── GetPaymentsPageAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPaymentsPageAsync_ReturnsAllPayments_WhenNoFilters()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "", null, 1, 10);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Payments.Count);
    }

    [Fact]
    public async Task GetPaymentsPageAsync_ReturnsCorrectPage()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "", null, 2, 10);

        Assert.Equal(10, result.Payments.Count);
    }

    [Fact]
    public async Task GetPaymentsPageAsync_ReturnsLastPage_WithRemainingItems()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "", null, 3, 10);

        Assert.Equal(5, result.Payments.Count);
    }

    [Fact]
    public async Task GetPaymentsPageAsync_FiltersByUserId()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("alice", null, null, "", null, 1, 20);

        Assert.True(result.TotalCount <= 13);
        Assert.All(result.Payments, p => Assert.Equal("user1", p.UserId));
    }

    [Fact]
    public async Task GetPaymentsPageAsync_FiltersByMinAmount()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", 100m, null, "", null, 1, 50);

        Assert.All(result.Payments, p => Assert.True(p.Amount >= 100m));
    }

    [Fact]
    public async Task GetPaymentsPageAsync_FiltersByMaxAmount()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, 50m, "", null, 1, 50);

        Assert.All(result.Payments, p => Assert.True(p.Amount <= 50m));
    }

    [Fact]
    public async Task GetPaymentsPageAsync_FiltersByPaidStatus()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "paid", null, 1, 50);

        Assert.All(result.Payments, p => Assert.True(p.IsPaid));
    }

    [Fact]
    public async Task GetPaymentsPageAsync_FiltersByUnpaidStatus()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "unpaid", null, 1, 50);

        Assert.All(result.Payments, p => Assert.False(p.IsPaid));
    }

    [Fact]
    public async Task GetPaymentsPageAsync_ReturnsEmpty_WhenNoMatches()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("nonexistent", null, null, "", null, 1, 10);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Payments);
    }

    [Fact]
    public async Task GetPaymentsPageAsync_ReturnsOnePage_WhenUnderPageSize()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "", null, 1, 50);

        Assert.Equal(1, result.TotalPages);
        Assert.Equal(25, result.Payments.Count);
    }

    [Fact]
    public async Task GetPaymentsPageAsync_OrdersByCreatedAtDescending()
    {
        var (db, service) = CreateService();
        await SeedPaymentsAsync(db);

        var result = await service.GetPaymentsPageAsync("", null, null, "", null, 1, 50);

        for (int i = 1; i < result.Payments.Count; i++)
        {
            Assert.True(result.Payments[i - 1].CreatedAt >= result.Payments[i].CreatedAt);
        }
    }

    // ── BuildReconciliationExportAsync ───────────────────────────────────

    [Fact]
    public async Task BuildReconciliationExportAsync_ReturnsCsv_WhenNoData()
    {
        var (_, service) = CreateService();

        var (csv, fileName) = await service.BuildReconciliationExportAsync();

        Assert.False(string.IsNullOrEmpty(csv));
        Assert.False(string.IsNullOrEmpty(fileName));
        Assert.Contains(".csv", fileName);
    }
}
