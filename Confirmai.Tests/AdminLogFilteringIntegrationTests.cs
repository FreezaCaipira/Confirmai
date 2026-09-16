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

namespace Confirmai.Tests;

public class AdminLogFilteringIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminLogFilteringIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Apply_WithDateRange_IncludesBoundariesOnly()
    {
        var marker = Guid.NewGuid().ToString("N");
        var insideStart = await SeedLogAsync($"source-date-{marker}", $"msg-date-{marker}", timestampUtc: new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc));
        var insideEnd = await SeedLogAsync($"source-date-{marker}", $"msg-date-{marker}", timestampUtc: new DateTime(2026, 3, 12, 23, 59, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync($"source-date-{marker}", $"msg-date-{marker}", timestampUtc: new DateTime(2026, 3, 9, 23, 59, 0, DateTimeKind.Utc));
        _ = await SeedLogAsync($"source-date-{marker}", $"msg-date-{marker}", timestampUtc: new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var filtered = AdminLogFiltering.Apply(db.Logs, new AdminLogFilterCriteria
        {
            Source = $"source-date-{marker}",
            StartDate = new DateTime(2026, 3, 10),
            EndDate = new DateTime(2026, 3, 12)
        })
        .Select(l => l.Id)
        .ToList();

        Assert.Equal(new[] { insideStart, insideEnd }, filtered.OrderBy(id => id));
    }

    [Fact]
    public async Task Apply_WithLevelSourceAndMessage_ReturnsOnlyMatchingRows()
    {
        var marker = Guid.NewGuid().ToString("N");
        var expected = await SeedLogAsync(
            source: $"AdminOrdersReview-{marker}",
            message: $"Repasse confirmado {marker}",
            level: "Info",
            userId: $"admin-{marker}");

        _ = await SeedLogAsync(
            source: $"AdminOrdersReview-{marker}",
            message: $"Tentativa recusada {marker}",
            level: "Warning",
            userId: $"admin-{marker}");

        _ = await SeedLogAsync(
            source: $"Webhook-{marker}",
            message: $"Repasse confirmado {marker}",
            level: "Info",
            userId: $"admin-{marker}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var filtered = AdminLogFiltering.Apply(db.Logs, new AdminLogFilterCriteria
        {
            Source = $"AdminOrdersReview-{marker}",
            Message = $"confirmado {marker}",
            Level = "Info"
        })
        .Select(l => l.Id)
        .ToList();

        Assert.Single(filtered);
        Assert.Equal(expected, filtered[0]);
    }

    [Fact]
    public async Task Apply_WithUserIdSubstring_ReturnsOnlyUserRelatedRows()
    {
        var marker = Guid.NewGuid().ToString("N");
        var expectedOne = await SeedLogAsync($"source-user-{marker}", $"msg-user-{marker}", userId: $"adm-{marker}-01");
        var expectedTwo = await SeedLogAsync($"source-user-{marker}", $"msg-user-{marker}", userId: $"adm-{marker}-02");
        _ = await SeedLogAsync($"source-user-{marker}", $"msg-user-{marker}", userId: $"other-{marker}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var filtered = AdminLogFiltering.Apply(db.Logs, new AdminLogFilterCriteria
        {
            Source = $"source-user-{marker}",
            UserId = $"adm-{marker}"
        })
        .Select(l => l.Id)
        .OrderBy(id => id)
        .ToList();

        Assert.Equal(new[] { expectedOne, expectedTwo }, filtered);
    }

    [Fact]
    public async Task Apply_WithGlobalTerm_MatchesUserSourceOrMessage()
    {
        var marker = Guid.NewGuid().ToString("N");
        var expectedFromUser = await SeedLogAsync(
            source: $"source-global-{marker}",
            message: "evento comum",
            userId: $"operator-{marker}-01");

        var expectedFromSource = await SeedLogAsync(
            source: $"gateway-{marker}",
            message: "evento comum",
            userId: "operator-other");

        var expectedFromMessage = await SeedLogAsync(
            source: "source-other",
            message: $"mensagem com token {marker}",
            userId: "operator-other");

        _ = await SeedLogAsync(
            source: "source-other",
            message: "sem match",
            userId: "operator-other");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var filtered = AdminLogFiltering.Apply(db.Logs, new AdminLogFilterCriteria
        {
            GlobalTerm = marker
        })
        .Select(l => l.Id)
        .OrderBy(id => id)
        .ToList();

        Assert.Equal(
            new[] { expectedFromUser, expectedFromSource, expectedFromMessage }.OrderBy(id => id),
            filtered);
    }

    [Fact]
    public async Task Apply_WithGlobalTermAndLevelAndDateRange_ReturnsOnlyIntersection()
    {
        var marker = Guid.NewGuid().ToString("N");
        var expected = await SeedLogAsync(
            source: AdminAuditSources.SecurityPolicy,
            message: $"repasse confirmado token {marker}",
            level: AdminAuditLevels.Success,
            timestampUtc: new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc));

        _ = await SeedLogAsync(
            source: AdminAuditSources.SecurityPolicy,
            message: $"repasse confirmado token {marker}",
            level: AdminAuditLevels.Refused,
            timestampUtc: new DateTime(2026, 3, 14, 13, 0, 0, DateTimeKind.Utc));

        _ = await SeedLogAsync(
            source: AdminAuditSources.SecurityPolicy,
            message: $"repasse confirmado token {marker}",
            level: AdminAuditLevels.Success,
            timestampUtc: new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var filtered = AdminLogFiltering.Apply(db.Logs, new AdminLogFilterCriteria
        {
            GlobalTerm = marker,
            Level = AdminAuditLevels.Success,
            StartDate = new DateTime(2026, 3, 10),
            EndDate = new DateTime(2026, 3, 20)
        })
        .Select(l => l.Id)
        .ToList();

        Assert.Single(filtered);
        Assert.Equal(expected, filtered[0]);
    }

    private async Task<int> SeedLogAsync(
        string source,
        string message,
        string level = "Info",
        string? userId = null,
        DateTime? timestampUtc = null)
    {
        if (userId is not null)
        {
            await _factory.EnsureUserAsync(userId);
        }

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


