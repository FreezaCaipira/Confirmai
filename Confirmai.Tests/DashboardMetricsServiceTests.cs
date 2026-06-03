using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

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

        db.Logs.AddRange(
            new AppLog { Source = "Quote", Message = "quote call" },
            new AppLog { Source = "CryptoQuote", Message = "quote call" },
            new AppLog { Source = "Payment", Message = "other" });

        await db.SaveChangesAsync();

        var service = new DashboardMetricsService(db);
        var snapshot = await service.GetSnapshotAsync();

        Assert.IsType<DashboardMetricsSnapshot>(snapshot);
        Assert.Equal(2, snapshot.UsersCount);
        Assert.Equal(2, snapshot.QuoteQueriesCount);
    }
}

