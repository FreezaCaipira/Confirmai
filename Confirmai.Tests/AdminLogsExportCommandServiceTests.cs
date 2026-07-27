using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Tests;

public class AdminLogsExportCommandServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, AdminLogsExportCommandService svc, TrackingJSRuntime js) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"logsexport-{Guid.NewGuid()}");
        var queryService = new AdminLogsQueryService(factory);
        var exportService = new AdminLogsExportService();
        var js = new TrackingJSRuntime();
        var svc = new AdminLogsExportCommandService(queryService, exportService, js);
        return (factory, svc, js);
    }

    private static async Task SeedLogsAsync(IDbContextFactory<AppDbContext> factory, int count)
    {
        await using var db = factory.CreateDbContext();
        for (int i = 0; i < count; i++)
        {
            db.Logs.Add(new AppLog
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Level = "Info",
                Source = "Payment",
                Message = $"Test log message {i}",
                UserId = "user-1",
                EventType = "payment.created",
                EntityType = "Payment",
                EntityId = i.ToString()
            });
        }
        await db.SaveChangesAsync();
    }

    private static AdminLogFilterCriteria CreateCriteria() => new();

    // ── ExportCsvAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ExportCsvAsync_ReturnsNull_WhenNoRows()
    {
        var (_, svc, _) = Setup();
        var result = await svc.ExportCsvAsync(CreateCriteria(), 1000, "", "", null, null, "Truncated: {0}");
        Assert.Null(result);
    }

    [Fact]
    public async Task ExportCsvAsync_ReturnsNull_WhenRowsExistAndNotTruncated()
    {
        var (factory, svc, js) = Setup();
        await SeedLogsAsync(factory, 5);

        var result = await svc.ExportCsvAsync(CreateCriteria(), 1000, "", "", null, null, "Truncated: {0}");

        Assert.Null(result);
        Assert.Equal(1, js.CallCount);
    }

    [Fact]
    public async Task ExportCsvAsync_ReturnsTruncatedMessage_WhenTruncated()
    {
        var (factory, svc, _) = Setup();
        await SeedLogsAsync(factory, 10);

        var result = await svc.ExportCsvAsync(CreateCriteria(), 5, "", "", null, null, "Max {0} rows");

        Assert.NotNull(result);
        Assert.Contains("5", result);
    }

    [Fact]
    public async Task ExportCsvAsync_DownloadsFile_WithCsvExtension()
    {
        var (factory, svc, js) = Setup();
        await SeedLogsAsync(factory, 3);

        await svc.ExportCsvAsync(CreateCriteria(), 1000, "", "", null, null, "Truncated: {0}");

        Assert.Equal(1, js.CallCount);
        Assert.EndsWith(".csv", js.LastFileName);
    }

    // ── ExportJsonAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ExportJsonAsync_ReturnsNull_WhenNoRows()
    {
        var (_, svc, _) = Setup();
        var result = await svc.ExportJsonAsync(CreateCriteria(), 1000, "", "", null, null, "Truncated: {0}");
        Assert.Null(result);
    }

    [Fact]
    public async Task ExportJsonAsync_ReturnsNull_WhenRowsExistAndNotTruncated()
    {
        var (factory, svc, js) = Setup();
        await SeedLogsAsync(factory, 5);

        var result = await svc.ExportJsonAsync(CreateCriteria(), 1000, "", "", null, null, "Truncated: {0}");

        Assert.Null(result);
        Assert.Equal(1, js.CallCount);
    }

    [Fact]
    public async Task ExportJsonAsync_ReturnsTruncatedMessage_WhenTruncated()
    {
        var (factory, svc, _) = Setup();
        await SeedLogsAsync(factory, 10);

        var result = await svc.ExportJsonAsync(CreateCriteria(), 5, "", "", null, null, "Max {0} rows");

        Assert.NotNull(result);
        Assert.Contains("5", result);
    }

    [Fact]
    public async Task ExportJsonAsync_DownloadsFile_WithJsonExtension()
    {
        var (factory, svc, js) = Setup();
        await SeedLogsAsync(factory, 3);

        await svc.ExportJsonAsync(CreateCriteria(), 1000, "", "", null, null, "Truncated: {0}");

        Assert.Equal(1, js.CallCount);
        Assert.EndsWith(".json", js.LastFileName);
    }

    // ── Combined ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExportCsvAsync_ReturnsNull_WhenFilteredToEmpty()
    {
        var (factory, svc, _) = Setup();
        await SeedLogsAsync(factory, 5);
        var criteria = new AdminLogFilterCriteria { Level = "Error" };

        var result = await svc.ExportCsvAsync(criteria, 1000, "Error", "", null, null, "Truncated: {0}");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExportJsonAsync_ReturnsNull_WhenFilteredToEmpty()
    {
        var (factory, svc, _) = Setup();
        await SeedLogsAsync(factory, 5);
        var criteria = new AdminLogFilterCriteria { Level = "Error" };

        var result = await svc.ExportJsonAsync(criteria, 1000, "Error", "", null, null, "Truncated: {0}");

        Assert.Null(result);
    }
}

internal sealed class TrackingJSRuntime : IJSRuntime
{
    public int CallCount { get; private set; }
    public string LastFileName { get; private set; } = "";

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        CallCount++;
        if (args is { Length: > 0 } && args[0] is string fn)
            LastFileName = fn;
        return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        CallCount++;
        if (args is { Length: > 0 } && args[0] is string fn)
            LastFileName = fn;
        return ValueTask.FromResult<TValue>(default!);
    }
}
