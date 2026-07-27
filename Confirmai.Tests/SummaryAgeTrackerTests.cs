using Confirmai.Data;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

public class SummaryAgeTrackerTests
{
    private static SummaryAgeTracker CreateTracker(out Mock<AuthenticationStateProvider> authMock)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"age-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        authMock = new Mock<AuthenticationStateProvider>();
        authMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        return new SummaryAgeTracker(logService, authMock.Object);
    }

    // ── Initial state ─────────────────────────────────────────────────

    [Fact]
    public void AgeLabel_DefaultsToNotAvailable()
    {
        var tracker = CreateTracker(out _);
        Assert.Equal("n/d", tracker.AgeLabel);
    }

    [Fact]
    public void AgeClass_DefaultsToEmpty()
    {
        var tracker = CreateTracker(out _);
        Assert.Equal(string.Empty, tracker.AgeClass);
    }

    [Fact]
    public void LastRefreshLabel_DefaultsToNotUpdated()
    {
        var tracker = CreateTracker(out _);
        Assert.Contains("atualizado", tracker.LastRefreshLabel.ToLowerInvariant());
    }

    // ── MarkRefreshed ─────────────────────────────────────────────────

    [Fact]
    public void MarkRefreshed_SetsLastRefreshLabel()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        Assert.NotEqual("Ainda nao atualizado.", tracker.LastRefreshLabel);
    }

    [Fact]
    public void MarkRefreshed_UpdatesAgeLabel()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        Assert.NotEqual("n/d", tracker.AgeLabel);
    }

    [Fact]
    public void MarkRefreshed_SetsAgeClassToNormal()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        Assert.Equal("admin-payments-summary-age", tracker.AgeClass);
    }

    // ── UpdateAgeLabel ────────────────────────────────────────────────

    [Fact]
    public void UpdateAgeLabel_ReturnsNotAvailable_WhenNeverRefreshed()
    {
        var tracker = CreateTracker(out _);
        tracker.UpdateAgeLabel();
        Assert.Equal("n/d", tracker.AgeLabel);
        Assert.Equal(string.Empty, tracker.AgeClass);
    }

    [Fact]
    public void UpdateAgeLabel_ShowsSeconds_WhenLessThan1Minute()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        tracker.UpdateAgeLabel();
        Assert.Matches(@"^\d+s$", tracker.AgeLabel);
    }

    [Fact]
    public void UpdateAgeLabel_ShowsMinutesAndSeconds_WhenBetween1MinAnd1Hour()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        // Simulate elapsed time by using reflection to set _lastRefreshAt
        var field = typeof(SummaryAgeTracker).GetField("_lastRefreshAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(tracker, DateTime.UtcNow.AddMinutes(-5));
        tracker.UpdateAgeLabel();
        Assert.Matches(@"^\d+m \d{2}s$", tracker.AgeLabel);
    }

    [Fact]
    public void UpdateAgeLabel_ShowsHoursMinutesSeconds_WhenOver1Hour()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        var field = typeof(SummaryAgeTracker).GetField("_lastRefreshAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(tracker, DateTime.UtcNow.AddHours(-2).AddMinutes(-30));
        tracker.UpdateAgeLabel();
        Assert.Matches(@"^\d+h \d{2}m \d{2}s$", tracker.AgeLabel);
    }

    [Fact]
    public void UpdateAgeLabel_SetsWarningClass_WhenOverWarningThreshold()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        var field = typeof(SummaryAgeTracker).GetField("_lastRefreshAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(tracker, DateTime.UtcNow.AddSeconds(-SummaryAgeTracker.WarningThresholdSeconds - 5));
        tracker.UpdateAgeLabel();
        Assert.Contains("warning", tracker.AgeClass);
    }

    [Fact]
    public void UpdateAgeLabel_SetsNormalClass_WhenUnderWarningThreshold()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        tracker.UpdateAgeLabel();
        Assert.DoesNotContain("warning", tracker.AgeClass);
    }

    // ── Constants ─────────────────────────────────────────────────────

    [Fact]
    public void RefreshIntervalSeconds_Is30()
    {
        Assert.Equal(30, SummaryAgeTracker.RefreshIntervalSeconds);
    }

    [Fact]
    public void WarningThresholdSeconds_IsDoubleRefreshInterval()
    {
        Assert.Equal(SummaryAgeTracker.RefreshIntervalSeconds * 2, SummaryAgeTracker.WarningThresholdSeconds);
    }

    [Fact]
    public void AuditThresholdSeconds_Is5Minutes()
    {
        Assert.Equal(300, SummaryAgeTracker.AuditThresholdSeconds);
    }

    // ── TryWriteStalenessAuditAsync ───────────────────────────────────

    [Fact]
    public async Task TryWriteStalenessAuditAsync_DoesNotThrow_WhenNeverRefreshed()
    {
        var tracker = CreateTracker(out _);
        await tracker.TryWriteStalenessAuditAsync(true, false);
    }

    [Fact]
    public async Task TryWriteStalenessAuditAsync_DoesNotThrow_WhenRecentlyRefreshed()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        await tracker.TryWriteStalenessAuditAsync(true, false);
    }

    [Fact]
    public async Task TryWriteStalenessAuditAsync_DoesNotThrow_WhenStaleButUnderAuditThreshold()
    {
        var tracker = CreateTracker(out _);
        tracker.MarkRefreshed();
        var field = typeof(SummaryAgeTracker).GetField("_lastRefreshAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(tracker, DateTime.UtcNow.AddSeconds(-SummaryAgeTracker.WarningThresholdSeconds - 5));
        tracker.UpdateAgeLabel();
        await tracker.TryWriteStalenessAuditAsync(true, false);
    }
}
