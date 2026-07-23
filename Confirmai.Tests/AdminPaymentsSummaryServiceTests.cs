using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminPaymentsSummaryServiceTests
{
    private static (AppDbContext db, AdminPaymentsSummaryService service) CreateService()
    {
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();

        var settingsService = new AdminSettingsService(dbFactory);
        var service = new AdminPaymentsSummaryService(dbFactory, settingsService);

        return (db, service);
    }

    // ── LoadSummaryAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task LoadSummaryAsync_ReturnsZeros_WhenNoData()
    {
        var (_, service) = CreateService();

        var result = await service.LoadSummaryAsync();

        Assert.Equal(0, result.PendingWithChargeId);
        Assert.Equal(0, result.PendingWithoutChargeId);
        Assert.Equal(0, result.StalePending);
        Assert.Equal(0, result.PaidToday);
    }

    [Fact]
    public async Task LoadSummaryAsync_CountsPendingWithChargeId()
    {
        var (db, service) = CreateService();

        db.EventConfirmations.Add(new EventConfirmation
        {
            Id = 1,
            EventId = 1,
            UserId = "user1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixTxId = "tx1",
            ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.LoadSummaryAsync();

        Assert.Equal(1, result.PendingWithChargeId);
    }

    [Fact]
    public async Task LoadSummaryAsync_CountsPendingWithoutChargeId()
    {
        var (db, service) = CreateService();

        db.EventConfirmations.Add(new EventConfirmation
        {
            Id = 1,
            EventId = 1,
            UserId = "user1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixTxId = null,
            ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.LoadSummaryAsync();

        Assert.Equal(1, result.PendingWithoutChargeId);
    }

    [Fact]
    public async Task LoadSummaryAsync_CountsStalePending_WhenOlderThan30Min()
    {
        var (db, service) = CreateService();

        db.EventConfirmations.Add(new EventConfirmation
        {
            Id = 1,
            EventId = 1,
            UserId = "user1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixTxId = "tx1",
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-31)
        });
        await db.SaveChangesAsync();

        var result = await service.LoadSummaryAsync();

        Assert.Equal(1, result.StalePending);
    }

    [Fact]
    public async Task LoadSummaryAsync_DoesNotCountStalePending_WhenNewerThan30Min()
    {
        var (db, service) = CreateService();

        db.EventConfirmations.Add(new EventConfirmation
        {
            Id = 1,
            EventId = 1,
            UserId = "user1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixTxId = "tx1",
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        var result = await service.LoadSummaryAsync();

        Assert.Equal(0, result.StalePending);
    }

    [Fact]
    public async Task LoadSummaryAsync_ReturnsEmptyGatewayTelemetry_WhenNoGateways()
    {
        var (_, service) = CreateService();

        var result = await service.LoadSummaryAsync();

        Assert.Empty(result.GatewayTelemetry);
    }

    [Fact]
    public async Task LoadSummaryAsync_ReturnsGatewayTelemetry_WithCorrectData()
    {
        var (db, service) = CreateService();

        db.EventConfirmations.Add(new EventConfirmation
        {
            Id = 1, EventId = 1, UserId = "u1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixTxId = "tx1", PaymentGatewayName = "EfiBank",
            ConfirmedAt = DateTime.UtcNow
        });
        db.EventConfirmations.Add(new EventConfirmation
        {
            Id = 2, EventId = 1, UserId = "u2",
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            PaymentGatewayName = "EfiBank",
            ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.LoadSummaryAsync();

        Assert.Single(result.GatewayTelemetry);
        Assert.Equal("EfiBank", result.GatewayTelemetry[0].Gateway);
        Assert.Equal(1, result.GatewayTelemetry[0].Pending);
        Assert.Equal(1, result.GatewayTelemetry[0].PaidTotal);
    }

    [Fact]
    public async Task LoadSummaryAsync_ReturnsDefaultSweepLabels_WhenNoSweeps()
    {
        var (_, service) = CreateService();

        var result = await service.LoadSummaryAsync();

        Assert.Equal("Sem varredura automática registrada.", result.LastAutomaticSweepLabel);
        Assert.Equal("Sem varredura manual registrada.", result.LastManualSweepLabel);
    }

    [Fact]
    public async Task LoadSummaryAsync_ReturnsNoTrendData_WhenNoSweeps()
    {
        var (_, service) = CreateService();

        var result = await service.LoadSummaryAsync();

        Assert.Equal("Sem dados suficientes para tendência.", result.PendingTrend24hLabel);
        Assert.False(result.IsPendingTrendWarning);
        Assert.Equal(0, result.PendingTrendDelta24h);
    }

    // ── GetGatewaySeverity (static) ──────────────────────────────────────

    [Theory]
    [InlineData(10, 0, "Crítico")]
    [InlineData(0, 3, "Crítico")]
    [InlineData(5, 1, "Atenção")]
    [InlineData(4, 0, "OK")]
    public void GetGatewaySeverity_ReturnsCorrectSeverity(int pending, int stalePending, string expectedLabel)
    {
        var (label, _) = AdminPaymentsSummaryService.GetGatewaySeverity(pending, stalePending);

        Assert.Equal(expectedLabel, label);
    }

    // ── BuildPendingTrendLabel (static) ──────────────────────────────────

    [Fact]
    public void BuildPendingTrendLabel_ReturnsNoData_WhenLatestIsNull()
    {
        var result = AdminPaymentsSummaryService.BuildPendingTrendLabel(null, 5, out var delta);

        Assert.Equal("Sem dados suficientes para tendência.", result);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_ReturnsNoBaseline_WhenBaselineIsNull()
    {
        var result = AdminPaymentsSummaryService.BuildPendingTrendLabel(10, null, out var delta);

        Assert.Contains("sem baseline", result);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_ReturnsUp_WhenDeltaPositive()
    {
        var result = AdminPaymentsSummaryService.BuildPendingTrendLabel(15, 10, out var delta);

        Assert.Contains("Subindo", result);
        Assert.Equal(5, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_ReturnsDown_WhenDeltaNegative()
    {
        var result = AdminPaymentsSummaryService.BuildPendingTrendLabel(5, 10, out var delta);

        Assert.Contains("Caindo", result);
        Assert.Equal(-5, delta);
    }

    [Fact]
    public void BuildPendingTrendLabel_ReturnsStable_WhenDeltaZero()
    {
        var result = AdminPaymentsSummaryService.BuildPendingTrendLabel(10, 10, out var delta);

        Assert.Contains("Estável", result);
        Assert.Equal(0, delta);
    }

    // ── GetSweepMetric (static) ──────────────────────────────────────────

    [Fact]
    public void GetSweepMetric_ReturnsNull_WhenJsonIsNull()
    {
        var result = AdminPaymentsSummaryService.GetSweepMetric(null, "considered");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_ReturnsNull_WhenJsonIsEmpty()
    {
        var result = AdminPaymentsSummaryService.GetSweepMetric("", "considered");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_ReturnsValue_WhenPropertyExists()
    {
        var json = """{"considered": 42, "updated": 10}""";

        var result = AdminPaymentsSummaryService.GetSweepMetric(json, "considered");

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetSweepMetric_ReturnsNull_WhenPropertyDoesNotExist()
    {
        var json = """{"considered": 42}""";

        var result = AdminPaymentsSummaryService.GetSweepMetric(json, "notFound");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_ReturnsNull_WhenPropertyIsNotNumber()
    {
        var json = """{"considered": "text"}""";

        var result = AdminPaymentsSummaryService.GetSweepMetric(json, "considered");

        Assert.Null(result);
    }

    [Fact]
    public void GetSweepMetric_ReturnsNull_WhenJsonIsInvalid()
    {
        var result = AdminPaymentsSummaryService.GetSweepMetric("not json", "considered");

        Assert.Null(result);
    }

    // ── HasSweepOrigin (static) ──────────────────────────────────────────

    [Fact]
    public void HasSweepOrigin_ReturnsFalse_WhenJsonIsNull()
    {
        var result = AdminPaymentsSummaryService.HasSweepOrigin(null, "worker.reconciliation");

        Assert.False(result);
    }

    [Fact]
    public void HasSweepOrigin_ReturnsTrue_WhenOriginMatches()
    {
        var json = """{"origin": "worker.reconciliation"}""";

        var result = AdminPaymentsSummaryService.HasSweepOrigin(json, "worker.reconciliation");

        Assert.True(result);
    }

    [Fact]
    public void HasSweepOrigin_ReturnsFalse_WhenOriginDoesNotMatch()
    {
        var json = """{"origin": "admin.sweep"}""";

        var result = AdminPaymentsSummaryService.HasSweepOrigin(json, "worker.reconciliation");

        Assert.False(result);
    }

    [Fact]
    public void HasSweepOrigin_ReturnsFalse_WhenOriginPropertyMissing()
    {
        var json = """{"other": "value"}""";

        var result = AdminPaymentsSummaryService.HasSweepOrigin(json, "worker.reconciliation");

        Assert.False(result);
    }

    [Fact]
    public void HasSweepOrigin_IsCaseInsensitive()
    {
        var json = """{"origin": "Worker.Reconciliation"}""";

        var result = AdminPaymentsSummaryService.HasSweepOrigin(json, "worker.reconciliation");

        Assert.True(result);
    }

    // ── BuildSweepDetails (static) ───────────────────────────────────────

    [Fact]
    public void BuildSweepDetails_ReturnsEmpty_WhenJsonIsNull()
    {
        var result = AdminPaymentsSummaryService.BuildSweepDetails(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildSweepDetails_ReturnsFormattedString_WhenValidJson()
    {
        var json = """{"considered": 10, "updated": 5, "stillPending": 3, "notFound": 2}""";

        var result = AdminPaymentsSummaryService.BuildSweepDetails(json);

        Assert.Contains("Considerados=10", result);
        Assert.Contains("Atualizados=5", result);
        Assert.Contains("Pendentes=3", result);
        Assert.Contains("Não encontrados=2", result);
    }

    [Fact]
    public void BuildSweepDetails_ReturnsZeros_WhenPropertiesMissing()
    {
        var json = """{"other": "value"}""";

        var result = AdminPaymentsSummaryService.BuildSweepDetails(json);

        Assert.Contains("Considerados=0", result);
        Assert.Contains("Atualizados=0", result);
    }

    // ── GetReconciliationSeverity (static) ───────────────────────────────

    [Theory]
    [InlineData(10, 0, 0, 3, 10, "Crítico")]
    [InlineData(0, 10, 0, 3, 10, "Crítico")]
    [InlineData(0, 0, 25, 3, 10, "Crítico")]
    [InlineData(0, 5, 0, 3, 10, "Atenção")]
    [InlineData(3, 0, 0, 3, 10, "Atenção")]
    [InlineData(0, 3, 0, 3, 10, "Atenção")]
    [InlineData(0, 0, 10, 3, 10, "Atenção")]
    [InlineData(1, 1, 5, 3, 10, "OK")]
    public void GetReconciliationSeverity_ReturnsCorrectSeverity(
        int stalePending, int trendDelta, int pendingWithChargeId,
        int warningThreshold, int criticalThreshold, string expectedLabel)
    {
        var (label, _) = AdminPaymentsSummaryService.GetReconciliationSeverity(
            stalePending, trendDelta, pendingWithChargeId, warningThreshold, criticalThreshold);

        Assert.Equal(expectedLabel, label);
    }

    // ── Helper ───────────────────────────────────────────────────────────

}
