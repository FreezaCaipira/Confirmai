using Confirmai.Services.Admin;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Confirmai.Tests;

public class AdminLogsQueryServiceIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminLogsQueryServiceIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPageDataAsync_ReturnsCountsAndSortedPage()
    {
        var marker = Guid.NewGuid().ToString("N");
        _ = await SeedLogAsync("Webhook", $"msg {marker}", level: AdminAuditLevels.Success, userId: $"user-b-{marker}", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync("Webhook", $"msg {marker}", level: AdminAuditLevels.Refused, userId: $"user-c-{marker}", timestampUtc: new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync(AdminAuditSources.SecurityPolicy, $"msg {marker}", level: AdminAuditLevels.Success, userId: $"user-d-{marker}", timestampUtc: new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync("Webhook", $"msg {marker}", level: "Info", userId: $"user-a-{marker}", timestampUtc: new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminLogsQueryService>();

        var data = await service.GetPageDataAsync(
            primaryCriteria: new AdminLogFilterCriteria { GlobalTerm = marker },
            auditCountsCriteria: new AdminLogFilterCriteria { GlobalTerm = marker },
            sortColumn: AdminLogSortColumn.User,
            sortAscending: true,
            requestedPage: 1,
            pageSize: 2);

        Assert.Equal(4, data.TotalLogs);
        Assert.Equal(1, data.EffectivePage);
        Assert.Equal(2, data.Logs.Count);
        Assert.Equal(4, data.AuditCounts.All);
        Assert.Equal(1, data.AuditCounts.SecurityPolicy);
        Assert.Equal(0, data.AuditCounts.PaymentPanelStale);

        Assert.Contains($"user-a-{marker}", data.Logs[0].UserId);
        Assert.Contains($"user-b-{marker}", data.Logs[1].UserId);
    }

    [Fact]
    public async Task GetPageDataAsync_WhenRequestedPageIsTooHigh_ClampsToLastPage()
    {
        var marker = Guid.NewGuid().ToString("N");
        _ = await SeedLogAsync("Webhook", $"msg {marker}", userId: $"user-1-{marker}", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync("Webhook", $"msg {marker}", userId: $"user-2-{marker}", timestampUtc: new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync("Webhook", $"msg {marker}", userId: $"user-3-{marker}", timestampUtc: new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminLogsQueryService>();

        var data = await service.GetPageDataAsync(
            primaryCriteria: new AdminLogFilterCriteria { GlobalTerm = marker },
            auditCountsCriteria: new AdminLogFilterCriteria { GlobalTerm = marker },
            sortColumn: AdminLogSortColumn.Timestamp,
            sortAscending: false,
            requestedPage: 99,
            pageSize: 2);

        Assert.Equal(3, data.TotalLogs);
        Assert.Equal(2, data.EffectivePage);
        Assert.Single(data.Logs);
    }

    [Fact]
    public async Task GetPageDataAsync_WhenPageOrPageSizeIsInvalid_NormalizesValues()
    {
        var marker = Guid.NewGuid().ToString("N");
        _ = await SeedLogAsync("Webhook", $"msg {marker}", userId: $"user-1-{marker}", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync("Webhook", $"msg {marker}", userId: $"user-2-{marker}", timestampUtc: new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminLogsQueryService>();

        var data = await service.GetPageDataAsync(
            primaryCriteria: new AdminLogFilterCriteria { GlobalTerm = marker },
            auditCountsCriteria: new AdminLogFilterCriteria { GlobalTerm = marker },
            sortColumn: AdminLogSortColumn.Timestamp,
            sortAscending: false,
            requestedPage: 0,
            pageSize: 0);

        Assert.Equal(2, data.TotalLogs);
        Assert.Equal(1, data.EffectivePage);
        Assert.Single(data.Logs);
        Assert.Equal(2, data.AuditCounts.All);
    }

    [Fact]
    public async Task GetPageDataAsync_ReturnsSecurityPolicyAuditLogs_AfterPolicyChange()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var updateScope = _factory.Services.CreateScope())
        {
            var policyService = updateScope.ServiceProvider.GetRequiredService<AdminSecurityPolicyService>();
            var admin = CreateAdminPrincipal($"admin-security-{marker}");

            var saved = await policyService.SetRuntimePolicyForAdminAsync(
                admin,
                new RuntimeSecurityPolicy(
                    RequireConfirmedEmail: false,
                    LockoutMaxFailedAccessAttempts: 4,
                    LockoutMinutes: 22));

            Assert.True(saved);
        }

        using var queryScope = _factory.Services.CreateScope();
        var service = queryScope.ServiceProvider.GetRequiredService<AdminLogsQueryService>();

        var data = await service.GetPageDataAsync(
            primaryCriteria: new AdminLogFilterCriteria
            {
                Source = AdminAuditSources.SecurityPolicy,
                UserId = $"admin-security-{marker}"
            },
            auditCountsCriteria: new AdminLogFilterCriteria
            {
                Source = AdminAuditSources.SecurityPolicy,
                UserId = $"admin-security-{marker}"
            },
            sortColumn: AdminLogSortColumn.Timestamp,
            sortAscending: false,
            requestedPage: 1,
            pageSize: 10);

        Assert.True(data.TotalLogs >= 1);
        Assert.True(data.AuditCounts.SecurityPolicy >= 1);
        Assert.Contains(data.Logs, log =>
            log.Source == AdminAuditSources.SecurityPolicy &&
            log.UserId == $"admin-security-{marker}" &&
            log.Message.Contains("Security policy updated.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetPageDataAsync_ReturnsPaymentPanelStaleAuditCountsAndRows()
    {
        var marker = Guid.NewGuid().ToString("N");

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(new AppLog
            {
                Source = AdminAuditSources.Payments,
                Message = $"stale panel {marker}",
                Level = "Warning",
                EventType = AuditEvents.PaymentReconciliationPanelStale,
                EntityType = AuditEntities.Payment,
                Timestamp = new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync();
        }

        using var queryScope = _factory.Services.CreateScope();
        var service = queryScope.ServiceProvider.GetRequiredService<AdminLogsQueryService>();

        var data = await service.GetPageDataAsync(
            primaryCriteria: new AdminLogFilterCriteria
            {
                GlobalTerm = marker,
                EventType = AuditEvents.PaymentReconciliationPanelStale
            },
            auditCountsCriteria: new AdminLogFilterCriteria
            {
                GlobalTerm = marker
            },
            sortColumn: AdminLogSortColumn.Timestamp,
            sortAscending: false,
            requestedPage: 1,
            pageSize: 10);

        Assert.Equal(1, data.TotalLogs);
        Assert.Equal(1, data.AuditCounts.PaymentPanelStale);
        Assert.Single(data.Logs);
        Assert.Equal(AuditEvents.PaymentReconciliationPanelStale, data.Logs[0].EventType);
    }

    private static ClaimsPrincipal CreateAdminPrincipal(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, "admin")
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test-auth"));
    }

    private async Task<int> SeedLogAsync(
        string source,
        string message,
        string level = "Info",
        string? userId = null,
        DateTime? timestampUtc = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var log = new AppLog
        {
            Source = source,
            Message = message,
            Level = level,
            UserId = userId,
            Timestamp = timestampUtc ?? DateTime.UtcNow
        };

        db.Logs.Add(log);
        await db.SaveChangesAsync();
        return log.Id;
    }
}


