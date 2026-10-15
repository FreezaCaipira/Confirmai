using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Moq;

namespace Confirmai.Tests;

public class C42BPageViewTrackerTests
{
    private static (PageViewTracker Tracker, IDbContextFactory<AppDbContext> Factory) Create()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"c42b-pv-{Guid.NewGuid()}");
        var tracker = new PageViewTracker(
            TestLogService.Create(factory),
            new Mock<IJSRuntime>().Object,
            new OperationalMetrics(),
            Microsoft.Extensions.Options.Options.Create(new MetricsOptions()));
        return (tracker, factory);
    }

    private static async Task<List<string>> PageViewsAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Logs
            .Where(l => l.EventType == AuditEvents.PageView)
            .OrderBy(l => l.Id)
            .Select(l => l.Message)
            .ToListAsync();
    }

    [Fact]
    public async Task SamePath_RecordedOnce_PerCircuit()
    {
        var (tracker, factory) = Create();

        await tracker.RecordAsync("https://app.test/grupo/1", "u1", 120);
        await tracker.RecordAsync("https://app.test/grupo/1", "u1", 4000);

        Assert.Equal(["/grupo/{id}"], await PageViewsAsync(factory));
    }

    [Fact]
    public async Task DifferentIds_SameTemplate_AreSeparateViews()
    {
        var (tracker, factory) = Create();

        await tracker.RecordAsync("https://app.test/grupo/1", "u1", 120);
        await tracker.RecordAsync("https://app.test/grupo/2", "u1", 90);

        Assert.Equal(["/grupo/{id}", "/grupo/{id}"], await PageViewsAsync(factory));
    }

    [Fact]
    public void MainLayout_RecordsPageView_OnlyAfterNavigation()
    {
        var razor = File.ReadAllText(Path.Combine(RepoRoot(), "Shared", "Components", "MainLayout.razor"));

        Assert.Contains("if (pageViewPending)", razor);
        Assert.Contains("pageViewPending = true;", razor);
    }

    [Fact]
    public void JoinPage_InviteOpened_OnlyInInteractiveCircuit()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", "Groups", "Join.razor.cs"));
        var guard = code.IndexOf("if (RendererInfo.IsInteractive)", StringComparison.Ordinal);
        var audit = code.IndexOf("AuditEvents.GroupInviteOpened", StringComparison.Ordinal);

        Assert.True(guard >= 0 && guard < audit);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Confirmai.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }
}
