using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminRevenueReportServiceTests
{
    private static PaymentRecord CreatePaymentRecord(
        decimal baseAmount,
        decimal feeAmount,
        PayoutStatus payoutStatus,
        DateTime createdAt,
        Product? product = null)
    {
        return new PaymentRecord
        {
            PaymentId = Guid.NewGuid().ToString(),
            Amount = baseAmount + feeAmount,
            IsPaid = true,
            PaidAt = createdAt,
            CreatedAt = createdAt,
            BaseAmount = baseAmount,
            FeeAmount = feeAmount,
            PayoutStatus = payoutStatus,
            PayoutPixKey = "organizador@email.com",
            Product = product,
            ProductId = product?.Id ?? 0
        };
    }

    [Fact]
    public async Task GetRevenueSummaryAsync_WithPayments_CalculatesCorrectly()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var product = new Product { Name = "Group A", Description = "Test", Price = 50m, UserId = "seller-1" };
        db.Products.Add(product);
        db.SaveChanges();

        db.Payments.Add(CreatePaymentRecord(50m, 1m, PayoutStatus.Sent, DateTime.UtcNow.AddDays(-5), product));
        db.Payments.Add(CreatePaymentRecord(30m, 0.5m, PayoutStatus.Confirmed, DateTime.UtcNow.AddDays(-3), product));
        db.Payments.Add(CreatePaymentRecord(20m, 0.5m, PayoutStatus.Failed, DateTime.UtcNow.AddDays(-1), product));
        await db.SaveChangesAsync();

        var service = new AdminRevenueReportService(dbFactory);
        var summary = await service.GetRevenueSummaryAsync(null, null);

        Assert.Equal(3, summary.TotalTransactions);
        Assert.Equal(2m, summary.TotalFeesCollected);
        Assert.Equal(100m, summary.TotalBaseAmount);
        Assert.Equal(1, summary.TotalPayoutsSent);
        Assert.Equal(1, summary.TotalPayoutsConfirmed);
        Assert.Equal(1, summary.TotalPayoutsFailed);
        Assert.Equal(0, summary.TotalPayoutsPending);
    }

    [Fact]
    public async Task GetRevenueSummaryAsync_NoPayments_ReturnsZeros()
    {
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var service = new AdminRevenueReportService(dbFactory);
        var summary = await service.GetRevenueSummaryAsync(null, null);

        Assert.Equal(0, summary.TotalTransactions);
        Assert.Equal(0m, summary.TotalFeesCollected);
        Assert.Equal(0m, summary.TotalBaseAmount);
        Assert.Equal(0, summary.TotalPayoutsSent);
        Assert.Equal(0, summary.TotalPayoutsConfirmed);
        Assert.Equal(0, summary.TotalPayoutsFailed);
        Assert.Equal(0, summary.TotalPayoutsPending);
    }

    [Fact]
    public async Task GetRevenueSummaryAsync_FiltersByDateRange()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var product = new Product { Name = "Group A", Description = "Test", Price = 50m, UserId = "seller-1" };
        db.Products.Add(product);
        db.SaveChanges();

        db.Payments.Add(CreatePaymentRecord(50m, 1m, PayoutStatus.Sent, DateTime.UtcNow.AddDays(-10), product));
        db.Payments.Add(CreatePaymentRecord(30m, 0.5m, PayoutStatus.Confirmed, DateTime.UtcNow.AddDays(-2), product));
        await db.SaveChangesAsync();

        var service = new AdminRevenueReportService(dbFactory);
        var summary = await service.GetRevenueSummaryAsync(
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow);

        Assert.Equal(1, summary.TotalTransactions);
        Assert.Equal(0.5m, summary.TotalFeesCollected);
    }

    [Fact]
    public async Task GetRevenueSummaryAsync_ExcludesZeroFeePayments()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var product = new Product { Name = "Group A", Description = "Test", Price = 50m, UserId = "seller-1" };
        db.Products.Add(product);
        db.SaveChanges();

        db.Payments.Add(CreatePaymentRecord(50m, 1m, PayoutStatus.Sent, DateTime.UtcNow, product));
        db.Payments.Add(CreatePaymentRecord(30m, 0m, PayoutStatus.Sent, DateTime.UtcNow, product));
        await db.SaveChangesAsync();

        var service = new AdminRevenueReportService(dbFactory);
        var summary = await service.GetRevenueSummaryAsync(null, null);

        Assert.Equal(1, summary.TotalTransactions);
        Assert.Equal(1m, summary.TotalFeesCollected);
    }

    [Fact]
    public async Task GetRevenueByGroupAsync_WithMultipleGroups_ReturnsBreakdown()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var productA = new Product { Name = "Group A", Description = "Test", Price = 50m, UserId = "seller-1" };
        var productB = new Product { Name = "Group B", Description = "Test", Price = 30m, UserId = "seller-2" };
        db.Products.AddRange(productA, productB);
        db.SaveChanges();

        db.Payments.Add(CreatePaymentRecord(50m, 1m, PayoutStatus.Sent, DateTime.UtcNow, productA));
        db.Payments.Add(CreatePaymentRecord(50m, 1m, PayoutStatus.Confirmed, DateTime.UtcNow, productA));
        db.Payments.Add(CreatePaymentRecord(30m, 0.5m, PayoutStatus.Failed, DateTime.UtcNow, productB));
        await db.SaveChangesAsync();

        var service = new AdminRevenueReportService(dbFactory);
        var breakdown = await service.GetRevenueByGroupAsync(null, null);

        Assert.Equal(2, breakdown.Count);
        Assert.Equal("Group A", breakdown[0].GroupName);
        Assert.Equal(2, breakdown[0].TotalTransactions);
        Assert.Equal(2m, breakdown[0].TotalFeesCollected);
        Assert.Equal("Group B", breakdown[1].GroupName);
        Assert.Equal(1, breakdown[1].TotalTransactions);
        Assert.Equal(0.5m, breakdown[1].TotalFeesCollected);
    }

    [Fact]
    public async Task GetRevenueByGroupAsync_NoPayments_ReturnsEmptyList()
    {
        var (_, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var service = new AdminRevenueReportService(dbFactory);
        var breakdown = await service.GetRevenueByGroupAsync(null, null);

        Assert.Empty(breakdown);
    }

    [Fact]
    public async Task GetRevenueByGroupAsync_OrdersByFeesDescending()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var productLow = new Product { Name = "Low Fee Group", Description = "Test", Price = 10m, UserId = "s1" };
        var productHigh = new Product { Name = "High Fee Group", Description = "Test", Price = 100m, UserId = "s2" };
        db.Products.AddRange(productLow, productHigh);
        db.SaveChanges();

        db.Payments.Add(CreatePaymentRecord(10m, 0.1m, PayoutStatus.Sent, DateTime.UtcNow, productLow));
        db.Payments.Add(CreatePaymentRecord(100m, 5m, PayoutStatus.Sent, DateTime.UtcNow, productHigh));
        await db.SaveChangesAsync();

        var service = new AdminRevenueReportService(dbFactory);
        var breakdown = await service.GetRevenueByGroupAsync(null, null);

        Assert.Equal("High Fee Group", breakdown[0].GroupName);
        Assert.Equal("Low Fee Group", breakdown[1].GroupName);
    }
}
