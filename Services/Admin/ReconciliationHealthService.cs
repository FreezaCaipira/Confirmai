using System.Text.Json;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

public sealed class ReconciliationHealthData
{
    public int PendingWithChargeId { get; set; }
    public int StalePending { get; set; }
    public string LastAutoSweepLabel { get; set; } = "Sem varredura automatica registrada.";
    public string LastHealthRefreshLabel { get; set; } = "Ainda nao atualizado.";
    public string PendingTrendLabel { get; set; } = "Sem dados suficientes para tendencia.";
    public bool PendingTrendWarning { get; set; }
}

public sealed class ReconciliationHealthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReconciliationHealthService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ReconciliationHealthData> LoadAsync(int warningThreshold)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var staleCutoff = DateTime.UtcNow.AddMinutes(-30);
        var data = new ReconciliationHealthData
        {
            PendingWithChargeId = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null),
            StalePending = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.ConfirmedAt <= staleCutoff),
        };

        var sweepCandidates = await db.Logs
            .AsNoTracking()
            .Where(l => l.EventType == AuditEvents.PaymentReconciliationSweep && l.MetadataJson != null)
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToListAsync();

        var latestSweep = sweepCandidates.FirstOrDefault(l => HasSweepOrigin(l.MetadataJson, "worker.reconciliation"));
        if (latestSweep is null)
        {
            data.LastAutoSweepLabel = "Sem varredura automatica registrada.";
            data.PendingTrendLabel = "Sem dados suficientes para tendencia.";
            return data;
        }

        data.LastAutoSweepLabel = latestSweep.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");

        var cutoff24h = DateTime.UtcNow.AddHours(-24);
        var baselineSweep = sweepCandidates
            .Where(l => l.Timestamp >= cutoff24h && HasSweepOrigin(l.MetadataJson, "worker.reconciliation"))
            .OrderBy(l => l.Timestamp)
            .FirstOrDefault();

        var latestPending = GetSweepMetric(latestSweep.MetadataJson, "stillPending");
        var baselinePending = baselineSweep is null ? (int?)null : GetSweepMetric(baselineSweep.MetadataJson, "stillPending");

        data.PendingTrendLabel = BuildPendingTrendLabel(latestPending, baselinePending, out var pendingDelta);
        data.PendingTrendWarning = pendingDelta >= warningThreshold;
        data.LastHealthRefreshLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        return data;
    }

    private static string BuildPendingTrendLabel(int? latestPending, int? baselinePending, out int pendingDelta)
    {
        pendingDelta = 0;
        if (!latestPending.HasValue) return "Sem dados suficientes para tendencia.";
        if (!baselinePending.HasValue) return $"Atual: {latestPending.Value} pendente(s); sem baseline de 24h.";

        var delta = latestPending.Value - baselinePending.Value;
        pendingDelta = delta;
        if (delta > 0) return $"Subindo (+{delta}) nas ultimas 24h.";
        if (delta < 0) return $"Caindo ({delta}) nas ultimas 24h.";
        return "Estavel (sem variacao nas ultimas 24h).";
    }

    private static int? GetSweepMetric(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (!document.RootElement.TryGetProperty(propertyName, out var property)) return null;
            return property.ValueKind == JsonValueKind.Number ? property.GetInt32() : null;
        }
        catch { return null; }
    }

    private static bool HasSweepOrigin(string? metadataJson, string expectedOrigin)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return false;
        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (!document.RootElement.TryGetProperty("origin", out var originProperty)) return false;
            return string.Equals(originProperty.GetString(), expectedOrigin, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
