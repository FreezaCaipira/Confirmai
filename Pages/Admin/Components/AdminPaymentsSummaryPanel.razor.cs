using Confirmai.Services.Admin;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Admin.Components;

public partial class AdminPaymentsSummaryPanel
{
    // Summary metrics
    [Parameter] public int PendingWithChargeId { get; set; }
    [Parameter] public int PendingWithoutChargeId { get; set; }
    [Parameter] public int StalePending { get; set; }
    [Parameter] public int PaidToday { get; set; }

    // UI state
    [Parameter] public bool IsRunningSweep { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public bool AutoRefreshEnabled { get; set; }
    [Parameter] public bool IsAutoRefreshPaused { get; set; }

    // Severity & trends
    [Parameter] public string SeverityLabel { get; set; } = "OK";
    [Parameter] public string SeverityClass { get; set; } = "admin-payments-severity-pill--ok";
    [Parameter] public string PendingTrend24hLabel { get; set; } = string.Empty;
    [Parameter] public bool IsPendingTrendWarning { get; set; }
    [Parameter] public string PendingByGatewayLabel { get; set; } = string.Empty;
    [Parameter] public string PaidByGatewayLabel { get; set; } = string.Empty;

    // Timestamps
    [Parameter] public string LastRefreshLabel { get; set; } = "Ainda não atualizado.";
    [Parameter] public string LastRefreshAgeLabel { get; set; } = "n/d";
    [Parameter] public string LastRefreshAgeClass { get; set; } = string.Empty;
    [Parameter] public string LastAutoRefreshPauseLabel { get; set; } = string.Empty;
    [Parameter] public string LastAutomaticSweepLabel { get; set; } = "Nenhum sweep automático registrado.";
    [Parameter] public string LastAutomaticSweepDetails { get; set; } = string.Empty;
    [Parameter] public string LastManualSweepLabel { get; set; } = "Nenhum sweep manual registrado.";
    [Parameter] public string LastManualSweepDetails { get; set; } = string.Empty;

    // Collections (simplified for component interface)
    [Parameter] public IReadOnlyList<GatewayTelemetryItem>? GatewayTelemetry { get; set; }
    [Parameter] public IReadOnlyList<SweepHistoryItem>? SweepHistory { get; set; }

    // Result messages
    [Parameter] public string SweepResultMessage { get; set; } = string.Empty;
    [Parameter] public bool SweepResultIsError { get; set; }
    [Parameter] public string StalenessLogsHref { get; set; } = "/admin/logs";

    // Callbacks
    [Parameter] public EventCallback OnRunSweep { get; set; }
    [Parameter] public EventCallback OnRefreshSummary { get; set; }
    [Parameter] public EventCallback OnExportCsv { get; set; }
    [Parameter] public EventCallback OnOpenAdvancedTools { get; set; }
    [Parameter] public EventCallback OnDismissSweepMessage { get; set; }

    private int GetPendingHeightPercent(SweepHistoryItem item)
    {
        int max = 50;
        var height = Math.Max(12m, ((decimal)item.StillPending / max) * 100m);
        return (int)Math.Round(height, 0, MidpointRounding.AwayFromZero);
    }
}
