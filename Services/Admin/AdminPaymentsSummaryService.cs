using System.Text.Json;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

public sealed record GatewayTelemetryItem(
    string Gateway,
    int Pending,
    int StalePending,
    int PaidTotal,
    string PendingShareLabel,
    string SeverityLabel,
    string SeverityClass);

public sealed record SweepHistoryItem(
    string TimestampLabel,
    int Considered,
    int Updated,
    int StillPending,
    int NotFound);

public sealed record AdminPaymentsSummaryResult
{
    public int PendingWithChargeId { get; init; }
    public int PendingWithoutChargeId { get; init; }
    public int StalePending { get; init; }
    public int PaidToday { get; init; }
    public string PendingByGatewayLabel { get; init; } = string.Empty;
    public string PaidByGatewayLabel { get; init; } = string.Empty;
    public IReadOnlyList<GatewayTelemetryItem> GatewayTelemetry { get; init; } = Array.Empty<GatewayTelemetryItem>();
    public string LastAutomaticSweepLabel { get; init; } = string.Empty;
    public string LastAutomaticSweepDetails { get; init; } = string.Empty;
    public string LastManualSweepLabel { get; init; } = string.Empty;
    public string LastManualSweepDetails { get; init; } = string.Empty;
    public string PendingTrend24hLabel { get; init; } = string.Empty;
    public bool IsPendingTrendWarning { get; init; }
    public int PendingTrendDelta24h { get; init; }
    public IReadOnlyList<SweepHistoryItem> AutomaticSweepHistory { get; init; } = Array.Empty<SweepHistoryItem>();
    public int WarningThreshold { get; init; }
    public int CriticalThreshold { get; init; }
}

public sealed class AdminPaymentsSummaryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AdminSettingsService _settingsService;

    public AdminPaymentsSummaryService(
        IDbContextFactory<AppDbContext> dbFactory,
        AdminSettingsService settingsService)
    {
        _dbFactory = dbFactory;
        _settingsService = settingsService;
    }

    public async Task<AdminPaymentsSummaryResult> LoadSummaryAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var thresholds = await _settingsService.GetReconciliationSeverityThresholdsAsync();
        var staleCutoff = DateTime.UtcNow.AddMinutes(-30);
        var todayUtc = DateTime.UtcNow.Date;

        var pendingWithChargeId = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null);
        var pendingWithoutChargeId = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId == null);
        var stalePending = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.ConfirmedAt <= staleCutoff);
        var paidToday = await db.Logs.CountAsync(l => l.EventType == AuditEvents.PaymentConfirmed && l.Timestamp >= todayUtc);

        var pendingByGateway = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.PaymentGatewayName != null)
            .GroupBy(c => c.PaymentGatewayName!)
            .Select(g => new { Gateway = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Gateway)
            .ToListAsync();

        var pendingByGatewayLabel = pendingByGateway.Count == 0
            ? "Sem pendências com gateway identificado."
            : string.Join(" • ", pendingByGateway.Select(x => $"{x.Gateway}: {x.Count}"));

        var paidByGateway = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Paid && c.PaymentGatewayName != null)
            .GroupBy(c => c.PaymentGatewayName!)
            .Select(g => new { Gateway = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Gateway)
            .ToListAsync();

        var paidByGatewayLabel = paidByGateway.Count == 0
            ? "Sem confirmações pagas com gateway identificado."
            : string.Join(" • ", paidByGateway.Select(x => $"{x.Gateway}: {x.Count}"));

        var gatewayTelemetryRaw = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PaymentGatewayName != null)
            .GroupBy(c => c.PaymentGatewayName!)
            .Select(g => new
            {
                Gateway = g.Key,
                Pending = g.Count(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending),
                StalePending = g.Count(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.ConfirmedAt <= staleCutoff),
                PaidTotal = g.Count(c => c.PaymentStatus == EventConfirmationPaymentStatus.Paid)
            })
            .OrderByDescending(x => x.Pending)
            .ThenBy(x => x.Gateway)
            .ToListAsync();

        var gatewayTelemetry = new List<GatewayTelemetryItem>();
        var totalPendingAcrossGateways = gatewayTelemetryRaw.Sum(x => x.Pending);

        foreach (var item in gatewayTelemetryRaw)
        {
            var share = totalPendingAcrossGateways == 0
                ? 0m
                : Math.Round((decimal)item.Pending * 100m / totalPendingAcrossGateways, 1, MidpointRounding.AwayFromZero);

            var (severityLabel, severityClass) = GetGatewaySeverity(item.Pending, item.StalePending);

            gatewayTelemetry.Add(new GatewayTelemetryItem(
                item.Gateway,
                item.Pending,
                item.StalePending,
                item.PaidTotal,
                $"{share:N1}%",
                severityLabel,
                severityClass));
        }

        var latestAutomaticSweepCandidates = await db.Logs
            .AsNoTracking()
            .Where(l =>
                l.EventType == AuditEvents.PaymentReconciliationSweep &&
                l.MetadataJson != null)
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToListAsync();

        var latestAutomaticSweep = latestAutomaticSweepCandidates
            .FirstOrDefault(l => HasSweepOrigin(l.MetadataJson, "worker.reconciliation"));

        string lastAutoSweepLabel;
        string lastAutoSweepDetails;
        if (latestAutomaticSweep is null)
        {
            lastAutoSweepLabel = "Sem varredura automática registrada.";
            lastAutoSweepDetails = string.Empty;
        }
        else
        {
            lastAutoSweepLabel = latestAutomaticSweep.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            lastAutoSweepDetails = BuildSweepDetails(latestAutomaticSweep.MetadataJson);
        }

        var latestManualSweep = latestAutomaticSweepCandidates
            .FirstOrDefault(l => HasSweepOrigin(l.MetadataJson, "admin.sweep"));

        string lastManualSweepLabel;
        string lastManualSweepDetails;
        if (latestManualSweep is null)
        {
            lastManualSweepLabel = "Sem varredura manual registrada.";
            lastManualSweepDetails = string.Empty;
        }
        else
        {
            lastManualSweepLabel = latestManualSweep.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            lastManualSweepDetails = BuildSweepDetails(latestManualSweep.MetadataJson);
        }

        string pendingTrend24hLabel;
        var isPendingTrendWarning = false;
        var pendingTrendDelta24h = 0;

        if (latestAutomaticSweep is null)
        {
            pendingTrend24hLabel = "Sem dados suficientes para tendência.";
        }
        else
        {
            var cutoff24h = DateTime.UtcNow.AddHours(-24);
            var baselineWorkerSweep = latestAutomaticSweepCandidates
                .Where(l => l.Timestamp >= cutoff24h && HasSweepOrigin(l.MetadataJson, "worker.reconciliation"))
                .OrderBy(l => l.Timestamp)
                .FirstOrDefault();

            var latestPending = GetSweepMetric(latestAutomaticSweep.MetadataJson, "stillPending");
            var baselinePending = baselineWorkerSweep is null
                ? (int?)null
                : GetSweepMetric(baselineWorkerSweep.MetadataJson, "stillPending");

            pendingTrend24hLabel = BuildPendingTrendLabel(latestPending, baselinePending, out var pendingDelta);
            pendingTrendDelta24h = pendingDelta;
            isPendingTrendWarning = pendingDelta >= thresholds.warningThreshold;
        }

        var latestAutomaticSweeps = latestAutomaticSweepCandidates
            .Where(l => HasSweepOrigin(l.MetadataJson, "worker.reconciliation"))
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToList();

        var sweepHistory = latestAutomaticSweeps
            .Select(log => new SweepHistoryItem(
                log.Timestamp.ToLocalTime().ToString("dd/MM HH:mm"),
                GetSweepMetric(log.MetadataJson, "considered") ?? 0,
                GetSweepMetric(log.MetadataJson, "updated") ?? 0,
                GetSweepMetric(log.MetadataJson, "stillPending") ?? 0,
                GetSweepMetric(log.MetadataJson, "notFound") ?? 0))
            .ToList();

        return new AdminPaymentsSummaryResult
        {
            PendingWithChargeId = pendingWithChargeId,
            PendingWithoutChargeId = pendingWithoutChargeId,
            StalePending = stalePending,
            PaidToday = paidToday,
            PendingByGatewayLabel = pendingByGatewayLabel,
            PaidByGatewayLabel = paidByGatewayLabel,
            GatewayTelemetry = gatewayTelemetry,
            LastAutomaticSweepLabel = lastAutoSweepLabel,
            LastAutomaticSweepDetails = lastAutoSweepDetails,
            LastManualSweepLabel = lastManualSweepLabel,
            LastManualSweepDetails = lastManualSweepDetails,
            PendingTrend24hLabel = pendingTrend24hLabel,
            IsPendingTrendWarning = isPendingTrendWarning,
            PendingTrendDelta24h = pendingTrendDelta24h,
            AutomaticSweepHistory = sweepHistory,
            WarningThreshold = thresholds.warningThreshold,
            CriticalThreshold = thresholds.criticalThreshold
        };
    }

    public static (string Label, string CssClass) GetGatewaySeverity(int pending, int stalePending)
    {
        if (stalePending >= 3 || pending >= 10)
            return ("Crítico", "admin-payments-gateway-severity--critical");

        if (stalePending >= 1 || pending >= 5)
            return ("Atenção", "admin-payments-gateway-severity--warning");

        return ("OK", "admin-payments-gateway-severity--ok");
    }

    public static string BuildPendingTrendLabel(int? latestPending, int? baselinePending, out int pendingDelta)
    {
        pendingDelta = 0;

        if (!latestPending.HasValue)
            return "Sem dados suficientes para tendência.";

        if (!baselinePending.HasValue)
            return $"Atual: {latestPending.Value} pendente(s); sem baseline de 24h.";

        var delta = latestPending.Value - baselinePending.Value;
        pendingDelta = delta;

        if (delta > 0)
            return $"Subindo (+{delta}) nas últimas 24h.";

        if (delta < 0)
            return $"Caindo ({delta}) nas últimas 24h.";

        return "Estável (sem variação nas últimas 24h).";
    }

    public static int? GetSweepMetric(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return null;

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (!document.RootElement.TryGetProperty(propertyName, out var property))
                return null;

            return property.ValueKind == JsonValueKind.Number
                ? property.GetInt32()
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static bool HasSweepOrigin(string? metadataJson, string expectedOrigin)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return false;

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (!document.RootElement.TryGetProperty("origin", out var originProperty))
                return false;

            var origin = originProperty.GetString();
            return string.Equals(origin, expectedOrigin, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string BuildSweepDetails(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var root = document.RootElement;

            var considered = root.TryGetProperty("considered", out var consideredProp)
                ? consideredProp.GetInt32()
                : 0;
            var updated = root.TryGetProperty("updated", out var updatedProp)
                ? updatedProp.GetInt32()
                : 0;
            var stillPending = root.TryGetProperty("stillPending", out var pendingProp)
                ? pendingProp.GetInt32()
                : 0;
            var notFound = root.TryGetProperty("notFound", out var notFoundProp)
                ? notFoundProp.GetInt32()
                : 0;

            return $"Considerados={considered}, Atualizados={updated}, Pendentes={stillPending}, Não encontrados={notFound}";
        }
        catch
        {
            return string.Empty;
        }
    }

    public static (string Label, string CssClass) GetReconciliationSeverity(
        int stalePending,
        int pendingTrendDelta24h,
        int pendingWithChargeId,
        int warningThreshold,
        int criticalThreshold)
    {
        var isCritical = stalePending >= 10 || pendingTrendDelta24h >= 10 || pendingWithChargeId >= 25;
        var isWarning = stalePending >= 3 || pendingTrendDelta24h >= warningThreshold || pendingWithChargeId >= 10;

        if (pendingTrendDelta24h >= criticalThreshold)
            isCritical = true;

        if (isCritical)
            return ("Crítico", "admin-payments-severity-pill--critical");

        if (isWarning)
            return ("Atenção", "admin-payments-severity-pill--warning");

        return ("OK", "admin-payments-severity-pill--ok");
    }
}
