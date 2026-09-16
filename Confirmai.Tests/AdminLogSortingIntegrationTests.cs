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

public class AdminLogSortingIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminLogSortingIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Apply_WhenSortingByTimestampDescending_ReturnsNewestFirst()
    {
        var marker = Guid.NewGuid().ToString("N");
        var oldest = await SeedLogAsync($"source-time-{marker}", $"msg-time-{marker}", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        var newest = await SeedLogAsync($"source-time-{marker}", $"msg-time-{marker}", timestampUtc: new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc));
        var middle = await SeedLogAsync($"source-time-{marker}", $"msg-time-{marker}", timestampUtc: new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sorted = AdminLogSorting.Apply(
                db.Logs.Where(l => l.Source == $"source-time-{marker}"),
                AdminLogSortColumn.Timestamp,
                sortAscending: false)
            .Select(l => l.Id)
            .ToList();

        Assert.Equal(new[] { newest, middle, oldest }, sorted);
    }

    [Fact]
    public async Task Apply_WhenSortingByLevelAscending_UsesTimestampAsTieBreaker()
    {
        var marker = Guid.NewGuid().ToString("N");
        var infoOlder = await SeedLogAsync($"source-level-{marker}", "info older", level: "Info", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        var errorNewest = await SeedLogAsync($"source-level-{marker}", "error newest", level: "Error", timestampUtc: new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc));
        var infoNewest = await SeedLogAsync($"source-level-{marker}", "info newest", level: "Info", timestampUtc: new DateTime(2026, 3, 13, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sorted = AdminLogSorting.Apply(
                db.Logs.Where(l => l.Source == $"source-level-{marker}"),
                AdminLogSortColumn.Level,
                sortAscending: true)
            .Select(l => l.Id)
            .ToList();

        Assert.Equal(new[] { errorNewest, infoNewest, infoOlder }, sorted);
    }

    [Fact]
    public async Task Apply_WhenSortingBySourceDescending_ReturnsLexicographicOrder()
    {
        var marker = Guid.NewGuid().ToString("N");
        var sourceA = await SeedLogAsync($"A-{marker}", "msg", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        var sourceC = await SeedLogAsync($"C-{marker}", "msg", timestampUtc: new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc));
        var sourceB = await SeedLogAsync($"B-{marker}", "msg", timestampUtc: new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sorted = AdminLogSorting.Apply(
                db.Logs.Where(l => l.Source.EndsWith(marker)),
                AdminLogSortColumn.Source,
                sortAscending: false)
            .Select(l => l.Id)
            .ToList();

        Assert.Equal(new[] { sourceC, sourceB, sourceA }, sorted);
    }

    [Fact]
    public async Task Apply_WhenSortingByUserAscending_PlacesNullOrEmptyFirstThenLexicographic()
    {
        var marker = Guid.NewGuid().ToString("N");
        var userZulu = await SeedLogAsync($"source-user-sort-{marker}", "msg", userId: $"zulu-{marker}", timestampUtc: new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc));
        var userNull = await SeedLogAsync($"source-user-sort-{marker}", "msg", userId: null, timestampUtc: new DateTime(2026, 3, 12, 8, 0, 0, DateTimeKind.Utc));
        var userAlpha = await SeedLogAsync($"source-user-sort-{marker}", "msg", userId: $"alpha-{marker}", timestampUtc: new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sorted = AdminLogSorting.Apply(
                db.Logs.Where(l => l.Source == $"source-user-sort-{marker}"),
                AdminLogSortColumn.User,
                sortAscending: true)
            .Select(l => l.Id)
            .ToList();

        Assert.Equal(new[] { userNull, userAlpha, userZulu }, sorted);
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


