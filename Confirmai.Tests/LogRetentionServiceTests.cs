using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Confirmai.Tests;

/// <summary>
/// Integration tests for <see cref="LogRetentionService"/> verifying the
/// daily retention policy:
///   - IpAddress anonymized for logs older than 30 days.
///   - Non-financial operational logs purged after 90 days.
///   - Financial logs (payment.* / order.*) never purged.
/// </summary>
public class LogRetentionServiceTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public LogRetentionServiceTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // IP anonymization
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_AnonymizesIpAddress_ForLogsOlderThan30Days()
    {
        var marker = Guid.NewGuid().ToString("N");

        // Seed a log older than 30 days with an IP address
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-31),
                Message = $"old-log-{marker}",
                Source = "Test",
                Level = "Info",
                IpAddress = "192.168.1.100",
                EventType = "user.login"
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = verifyDb.Logs.Single(l => l.Message == $"old-log-{marker}");
        Assert.Null(log.IpAddress);
    }

    [Fact]
    public async Task RunAsync_DoesNotAnonymizeIpAddress_ForLogsNewerThan30Days()
    {
        var marker = Guid.NewGuid().ToString("N");
        const string ip = "10.0.0.42";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-15),
                Message = $"recent-log-{marker}",
                Source = "Test",
                Level = "Info",
                IpAddress = ip,
                EventType = "user.login"
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = verifyDb.Logs.Single(l => l.Message == $"recent-log-{marker}");
        Assert.Equal(ip, log.IpAddress);
    }

    [Fact]
    public async Task RunAsync_AnonymizesIpAddress_ButPreservesOtherFields()
    {
        var marker = Guid.NewGuid().ToString("N");

        // AppLog.UserId is a real FK under Postgres.
        await _factory.EnsureUserAsync("user-abc");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-35),
                Message = $"fields-log-{marker}",
                Source = "Auth",
                Level = "Warning",
                IpAddress = "172.16.0.1",
                EventType = "user.login",
                UserId = "user-abc"
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = verifyDb.Logs.Single(l => l.Message == $"fields-log-{marker}");

        Assert.Null(log.IpAddress);                          // anonymized
        Assert.Equal("Auth", log.Source);                    // unchanged
        Assert.Equal("Warning", log.Level);                  // unchanged
        Assert.Equal("user.login", log.EventType);           // unchanged
        Assert.Equal("user-abc", log.UserId);                // unchanged
    }

    [Fact]
    public async Task RunAsync_DoesNotAnonymize_LogsThatAlreadyHaveNullIpAddress()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-45),
                Message = $"null-ip-log-{marker}",
                Source = "Test",
                Level = "Info",
                IpAddress = null,
                EventType = "user.login"
            });
            await db.SaveChangesAsync();
        }

        // Should not throw � filtering on IpAddress != null skips this entry
        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = verifyDb.Logs.Single(l => l.Message == $"null-ip-log-{marker}");
        Assert.Null(log.IpAddress); // was already null
    }

    // -------------------------------------------------------------------------
    // Purge of operational logs (> 90 days, non-financial)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_PurgesNonFinancialOperationalLog_OlderThan90Days()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-91),
                Message = $"old-operational-{marker}",
                Source = "Test",
                Level = "Info",
                EventType = "user.login"   // not financial
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = verifyDb.Logs.Any(l => l.Message == $"old-operational-{marker}");
        Assert.False(exists);
    }

    [Fact]
    public async Task RunAsync_PurgesLogWithNullEventType_OlderThan90Days()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-100),
                Message = $"null-event-type-{marker}",
                Source = "Test",
                Level = "Info",
                EventType = null
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = verifyDb.Logs.Any(l => l.Message == $"null-event-type-{marker}");
        Assert.False(exists);
    }

    [Fact]
    public async Task RunAsync_DoesNotPurge_NonFinancialLogNewerThan90Days()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-45),
                Message = $"recent-operational-{marker}",
                Source = "Test",
                Level = "Info",
                EventType = "user.login"
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = verifyDb.Logs.Any(l => l.Message == $"recent-operational-{marker}");
        Assert.True(exists);
    }

    // -------------------------------------------------------------------------
    // Financial log protection
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("payment.confirmed")]
    [InlineData("payment.refunded")]
    [InlineData("payment.created")]
    [InlineData("order.released")]
    [InlineData("order.disputed")]
    [InlineData("order.created")]
    public async Task RunAsync_NeverPurges_FinancialLog_OlderThan90Days(string eventType)
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-365),
                Message = $"financial-{eventType}-{marker}",
                Source = "Payment",
                Level = "Info",
                EventType = eventType
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = verifyDb.Logs.Any(l => l.Message == $"financial-{eventType}-{marker}");
        Assert.True(exists, $"Financial log with EventType '{eventType}' must never be purged.");
    }

    [Fact]
    public async Task RunAsync_NeverPurges_FinancialLog_EvenAfterManyYears()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddYears(-3),
                Message = $"very-old-financial-{marker}",
                Source = "Payment",
                Level = "Info",
                EventType = "payment.confirmed"
            });
            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = verifyDb.Logs.Any(l => l.Message == $"very-old-financial-{marker}");
        Assert.True(exists);
    }

    // -------------------------------------------------------------------------
    // Edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_AnonymizesIpAndPurgesOldOperational_InSameCycle()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Log that needs IP anonymization (> 30 days, < 90 days)
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-60),
                Message = $"anon-ip-{marker}",
                Source = "Test",
                Level = "Info",
                IpAddress = "1.2.3.4",
                EventType = "user.login"
            });

            // Old operational log to be purged (> 90 days)
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-95),
                Message = $"to-purge-{marker}",
                Source = "Test",
                Level = "Info",
                EventType = "user.logout"
            });

            // Financial log to be preserved (> 90 days)
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddDays(-200),
                Message = $"preserve-financial-{marker}",
                Source = "Payment",
                Level = "Info",
                EventType = "payment.confirmed"
            });

            await db.SaveChangesAsync();
        }

        await RunRetentionAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var anonLog = verifyDb.Logs.Single(l => l.Message == $"anon-ip-{marker}");
        Assert.Null(anonLog.IpAddress);

        var purged = verifyDb.Logs.Any(l => l.Message == $"to-purge-{marker}");
        Assert.False(purged);

        var financial = verifyDb.Logs.Any(l => l.Message == $"preserve-financial-{marker}");
        Assert.True(financial);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Invokes the internal <c>RunAsync</c> logic of <see cref="LogRetentionService"/>
    /// through the real <see cref="IServiceScopeFactory"/> provided by the test factory.
    /// </summary>
    private async Task RunRetentionAsync()
    {
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<LogRetentionService>.Instance;
        var service = new LogRetentionService(scopeFactory, logger);

        // Use reflection to invoke the private RunAsync method.
        var runMethod = typeof(LogRetentionService)
            .GetMethod("RunAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException("RunAsync method not found on LogRetentionService.");

        var task = (Task)runMethod.Invoke(service, [CancellationToken.None])!;
        await task;
    }
}
