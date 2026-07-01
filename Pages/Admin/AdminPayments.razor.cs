using Confirmai.Models;
using Confirmai.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Confirmai.Shared.Helpers;
using Confirmai.Data;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Confirmai.Pages.Admin;

public partial class AdminPayments : IAsyncDisposable
{
    private int totalCount;
    private int currentPage = 1;
    private int totalPages = 1;
    private List<PaymentRecord> currentPagePayments = new();
    private const int PageSize = 20;

    private string filterUserId = "";
    private decimal? filterMinAmount;
    private decimal? filterMaxAmount;
    private string filterStatus = "";
    private DateTime? filterDate;

    private bool filtersLoaded;
    private bool showRestoredFiltersNotice;
    private decimal? btcUsdRate;
    private decimal? btcBrlRate;
    private int reconciliationPendingWithChargeId;
    private int reconciliationPendingWithoutChargeId;
    private int reconciliationStalePending;
    private int reconciliationPaidToday;
    private string lastAutomaticSweepLabel = "Sem varredura automática registrada.";
    private string lastAutomaticSweepDetails = string.Empty;
    private string lastManualSweepLabel = "Sem varredura manual registrada.";
    private string lastManualSweepDetails = string.Empty;
    private string pendingTrend24hLabel = "Sem dados suficientes para tendência.";
    private bool isPendingTrendWarning;
    private string pendingByGatewayLabel = "Sem pendências com gateway identificado.";
    private string paidByGatewayLabel = "Sem confirmações pagas com gateway identificado.";
    private readonly List<GatewayTelemetryItem> gatewayTelemetry = new();
    private string reconciliationSeverityLabel = "OK";
    private string reconciliationSeverityClass = "admin-payments-severity-pill--ok";
    private int pendingTrendDelta24h;
    private readonly List<SweepHistoryItem> automaticSweepHistory = new();
    private bool isAutoRefreshEnabled = true;
    private bool isSummaryRefreshing;
    private DateTime? lastSummaryRefreshAt;
    private string lastSummaryRefreshLabel = "Ainda não atualizado.";
    private string lastSummaryAgeLabel = "n/d";
    private string lastSummaryAgeClass = string.Empty;
    private bool isAutoRefreshPausedByVisibility;
    private string lastAutoRefreshPauseLabel = "Nenhuma pausa registrada.";
    private DateTime? summaryStalenessStartedAt;
    private bool summaryStalenessIncidentLogged;
    private CancellationTokenSource? summaryRefreshCts;
    private Task? summaryRefreshTask;
    private CancellationTokenSource? summaryAgeCts;
    private Task? summaryAgeTask;
    private const int SummaryRefreshSeconds = 30;
    private const int SummaryStalenessWarningSeconds = SummaryRefreshSeconds * 2;
    private const int SummaryStalenessAuditSeconds = 5 * 60;
    private int pendingTrendWarningThreshold = AdminSettingsService.DefaultReconciliationWarningThreshold;
    private int pendingTrendCriticalThreshold = AdminSettingsService.DefaultReconciliationCriticalThreshold;

    private string reconcileChargeId = string.Empty;
    private bool isReconciling;
    private bool isRunningSweep;
    private string reconcileResultMessage = string.Empty;
    private bool reconcileResultIsError;
    private int? reconcileResultConfirmationId;
    private string sweepResultMessage = string.Empty;
    private bool sweepResultIsError;
    private int? timelineConfirmationId;
    private int? statusTransitionConfirmationId = null;
    private string statusTransitionTarget = string.Empty;
    private string statusTransitionReason = string.Empty;
    private bool isStatusTransitioning;
    private string statusTransitionResultMessage = string.Empty;
    private bool statusTransitionResultIsError;
    private bool isAdvancedToolsModalOpen;
    private bool shouldActivateAdvancedToolsModalA11y;
    private bool isAdvancedToolsModalA11yActive;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var quote = await BitcoinQuoteService.GetQuoteAsync();
        btcUsdRate = quote?.btc_usd;
        btcBrlRate = quote?.btc_brl;
        await RefreshOperationalPanelAsync(includePaymentsTable: false);
        await LoadPaymentsCountAsync();
        StartSummaryRefreshLoop();
        StartSummaryAgeLoop();
    }

    private void StartSummaryRefreshLoop()
    {
        summaryRefreshCts = new CancellationTokenSource();
        summaryRefreshTask = RunSummaryRefreshLoopAsync(summaryRefreshCts.Token);
    }

    private void StartSummaryAgeLoop()
    {
        summaryAgeCts = new CancellationTokenSource();
        summaryAgeTask = RunSummaryAgeLoopAsync(summaryAgeCts.Token);
    }

    private async Task RunSummaryRefreshLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(SummaryRefreshSeconds), cancellationToken);

                if (!isAutoRefreshEnabled)
                {
                    if (isAutoRefreshPausedByVisibility)
                    {
                        await InvokeAsync(() =>
                        {
                            isAutoRefreshPausedByVisibility = false;
                        });
                    }

                    continue;
                }

                if (!await IsDocumentVisibleAsync())
                {
                    await InvokeAsync(() =>
                    {
                        isAutoRefreshPausedByVisibility = true;
                        lastAutoRefreshPauseLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    });

                    continue;
                }

                if (isAutoRefreshPausedByVisibility)
                {
                    await InvokeAsync(() =>
                    {
                        isAutoRefreshPausedByVisibility = false;
                    });
                }

                await InvokeAsync(async () =>
                {
                    await RefreshOperationalPanelAsync(includePaymentsTable: false);
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunSummaryAgeLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                await InvokeAsync(async () =>
                {
                    UpdateLastSummaryAgeLabel();
                    await TryWriteStalenessIncidentAuditAsync();
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (shouldActivateAdvancedToolsModalA11y && isAdvancedToolsModalOpen)
        {
            shouldActivateAdvancedToolsModalA11y = false;

            try
            {
                await JS.InvokeVoidAsync("ConfirmaiModal.open", "#admin-payments-advanced-modal");
                isAdvancedToolsModalA11yActive = true;
            }
            catch
            {
                // No-op: modal a11y integration should not break page behavior.
            }
        }

        if (!firstRender || filtersLoaded) return;

        await LoadFilterStateFromStorageAsync();
        currentPage = 1;
        await LoadPaymentsCountAsync();
        filtersLoaded = true;
    }

    private async Task LoadPaymentsCountAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();

        var query = db.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterUserId))
            query = query.Where(p => (p.User != null && p.User.UserName != null && p.User.UserName.Contains(filterUserId)) || (p.UserId != null && p.UserId.Contains(filterUserId)));
        if (filterMinAmount.HasValue)
            query = query.Where(p => p.Amount >= filterMinAmount.Value);
        if (filterMaxAmount.HasValue)
            query = query.Where(p => p.Amount <= filterMaxAmount.Value);
        if (!string.IsNullOrWhiteSpace(filterStatus))
            query = filterStatus == "paid" ? query.Where(p => p.IsPaid) : query.Where(p => !p.IsPaid);
        if (filterDate.HasValue)
            query = query.Where(p => p.CreatedAt.Date == filterDate.Value.Date);

        totalCount = await query.CountAsync();
        totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        if (currentPage > totalPages)
            currentPage = totalPages;

        currentPagePayments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.User)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    private async Task GoToPrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            await LoadPaymentsCountAsync();
        }
    }

    private async Task GoToNextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            await LoadPaymentsCountAsync();
        }
    }

    private void ViewPayment(int id) => NavigationManager.NavigateTo($"/payments/view/{id}");

    private async Task ApplyFiltersAndPersist()
    {
        showRestoredFiltersNotice = false;
        currentPage = 1;
        await LoadPaymentsCountAsync();
        await PersistFilterStateAsync();
    }

    private async Task ClearFilters()
    {
        filterUserId = "";
        filterMinAmount = null;
        filterMaxAmount = null;
        filterStatus = "";
        filterDate = null;
        showRestoredFiltersNotice = false;
        currentPage = 1;
        await LoadPaymentsCountAsync();
        await PersistFilterStateAsync();
    }

    private async Task LoadFilterStateFromStorageAsync()
    {
        var state = await AdminPaymentsFilterStateService.LoadAsync();

        filterUserId = state.UserId;
        filterMinAmount = state.MinAmount;
        filterMaxAmount = state.MaxAmount;
        filterStatus = state.Status;
        filterDate = state.Date;

        showRestoredFiltersNotice =
            !string.IsNullOrWhiteSpace(filterUserId)
            || filterMinAmount.HasValue
            || filterMaxAmount.HasValue
            || !string.IsNullOrWhiteSpace(filterStatus)
            || filterDate.HasValue;
    }

    private async Task PersistFilterStateAsync()
    {
        await AdminPaymentsFilterStateService.SaveAsync(new AdminPaymentsFilterState
        {
            UserId = filterUserId,
            ProductId = string.Empty,
            MinAmount = filterMinAmount,
            MaxAmount = filterMaxAmount,
            Status = filterStatus,
            Date = filterDate
        });
    }

    private void DismissRestoredNotice()
    {
        showRestoredFiltersNotice = false;
    }

    private async Task HandleFiltersApplied((string UserId, decimal? MinAmount, decimal? MaxAmount, string Status, DateTime? Date) filters)
    {
        filterUserId = filters.UserId;
        filterMinAmount = filters.MinAmount;
        filterMaxAmount = filters.MaxAmount;
        filterStatus = filters.Status;
        filterDate = filters.Date;
        await ApplyFiltersAndPersist();
    }

    private MarkupString FormatBtcWithUsd(decimal amount)
    {
        return BtcUsdFormatter.FormatMarkup(amount, btcUsdRate, btcBrlRate, CurrencyPreferenceService.SelectedFiatCurrency);
    }

    private async Task ReconcileChargeAsync()
    {
        var chargeId = reconcileChargeId?.Trim();
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            reconcileResultIsError = true;
            reconcileResultMessage = "Informe um chargeId/txId para revalidar.";
            return;
        }

        isReconciling = true;
        reconcileResultMessage = string.Empty;
        reconcileResultConfirmationId = null;

        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var actorUserId = auth.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            var result = await EventPaymentReconciliationService.ReconcileByChargeIdAsync(chargeId, actorUserId);
            reconcileResultIsError = !result.Found || !result.IsPaid;
            reconcileResultMessage = result.Message;
            reconcileResultConfirmationId = result.ConfirmationId;

            if (result.ConfirmationId.HasValue)
            {
                timelineConfirmationId = result.ConfirmationId.Value;
            }

            await LoadOperationalSummaryAsync();
            await LoadPaymentsCountAsync();
        }
        catch (Exception ex)
        {
            reconcileResultIsError = true;
            reconcileResultMessage = $"Erro ao revalidar cobrança: {ex.Message}";
        }
        finally
        {
            isReconciling = false;
        }
    }

    private void OpenAdvancedToolsModal()
    {
        isAdvancedToolsModalOpen = true;
        shouldActivateAdvancedToolsModalA11y = true;
    }

    private async Task CloseAdvancedToolsModalAsync()
    {
        isAdvancedToolsModalOpen = false;
        shouldActivateAdvancedToolsModalA11y = false;

        if (!isAdvancedToolsModalA11yActive)
            return;

        try
        {
            await JS.InvokeVoidAsync("ConfirmaiModal.close");
        }
        catch
        {
            // Ignore JS interop errors during teardown.
        }
        finally
        {
            isAdvancedToolsModalA11yActive = false;
        }
    }

    private void HandleAdvancedToolsKeyDown(KeyboardEventArgs args)
    {
        if (string.Equals(args.Key, "Escape", StringComparison.OrdinalIgnoreCase))
        {
            _ = CloseAdvancedToolsModalAsync();
        }
    }

    private async Task LoadOperationalSummaryAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();

        var thresholds = await AdminSettingsService.GetReconciliationSeverityThresholdsAsync();
        pendingTrendWarningThreshold = thresholds.warningThreshold;
        pendingTrendCriticalThreshold = thresholds.criticalThreshold;

        var staleCutoff = DateTime.UtcNow.AddMinutes(-30);
        var todayUtc = DateTime.UtcNow.Date;

        reconciliationPendingWithChargeId = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null);
        reconciliationPendingWithoutChargeId = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId == null);
        reconciliationStalePending = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.ConfirmedAt <= staleCutoff);
        reconciliationPaidToday = await db.Logs.CountAsync(l => l.EventType == AuditEvents.PaymentConfirmed && l.Timestamp >= todayUtc);

        var pendingByGateway = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.PaymentGatewayName != null)
            .GroupBy(c => c.PaymentGatewayName!)
            .Select(g => new { Gateway = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Gateway)
            .ToListAsync();

        pendingByGatewayLabel = pendingByGateway.Count == 0
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

        paidByGatewayLabel = paidByGateway.Count == 0
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

        gatewayTelemetry.Clear();
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

        if (latestAutomaticSweep is null)
        {
            lastAutomaticSweepLabel = "Sem varredura automática registrada.";
            lastAutomaticSweepDetails = string.Empty;
        }
        else
        {
            lastAutomaticSweepLabel = latestAutomaticSweep.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            lastAutomaticSweepDetails = BuildSweepDetails(latestAutomaticSweep.MetadataJson);
        }

        var latestManualSweep = latestAutomaticSweepCandidates
            .FirstOrDefault(l => HasSweepOrigin(l.MetadataJson, "admin.sweep"));

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

        var latestWorkerSweep = latestAutomaticSweep;

        if (latestWorkerSweep is null)
        {
            pendingTrend24hLabel = "Sem dados suficientes para tendência.";
            isPendingTrendWarning = false;
            pendingTrendDelta24h = 0;
        }
        else
        {
            var cutoff24h = DateTime.UtcNow.AddHours(-24);
            var baselineWorkerSweep = latestAutomaticSweepCandidates
                .Where(l => l.Timestamp >= cutoff24h && HasSweepOrigin(l.MetadataJson, "worker.reconciliation"))
                .OrderBy(l => l.Timestamp)
                .FirstOrDefault();

            var latestPending = GetSweepMetric(latestWorkerSweep.MetadataJson, "stillPending");
            var baselinePending = baselineWorkerSweep is null
                ? (int?)null
                : GetSweepMetric(baselineWorkerSweep.MetadataJson, "stillPending");

            pendingTrend24hLabel = BuildPendingTrendLabel(latestPending, baselinePending, out var pendingDelta);
            pendingTrendDelta24h = pendingDelta;
            isPendingTrendWarning = pendingDelta >= pendingTrendWarningThreshold;
        }

        var latestAutomaticSweeps = latestAutomaticSweepCandidates
            .Where(l => HasSweepOrigin(l.MetadataJson, "worker.reconciliation"))
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToList();

        automaticSweepHistory.Clear();
        foreach (var log in latestAutomaticSweeps)
        {
            automaticSweepHistory.Add(new SweepHistoryItem(
                log.Timestamp.ToLocalTime().ToString("dd/MM HH:mm"),
                GetSweepMetric(log.MetadataJson, "considered") ?? 0,
                GetSweepMetric(log.MetadataJson, "updated") ?? 0,
                GetSweepMetric(log.MetadataJson, "stillPending") ?? 0,
                GetSweepMetric(log.MetadataJson, "notFound") ?? 0));
        }

        UpdateSeverity();
    }

    private async Task RefreshOperationalPanelAsync(bool includePaymentsTable)
    {
        if (isSummaryRefreshing)
            return;

        isSummaryRefreshing = true;

        try
        {
            await LoadOperationalSummaryAsync();

            if (includePaymentsTable)
                await LoadPaymentsCountAsync();

            lastSummaryRefreshAt = DateTime.UtcNow;
            lastSummaryRefreshLabel = lastSummaryRefreshAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            UpdateLastSummaryAgeLabel();
        }
        finally
        {
            isSummaryRefreshing = false;
        }
    }

    private async Task RefreshSummaryNowAsync()
    {
        await RefreshOperationalPanelAsync(includePaymentsTable: true);
    }

    private async Task<bool> IsDocumentVisibleAsync()
    {
        try
        {
            return await JS.InvokeAsync<bool>("ConfirmaiIsDocumentVisible");
        }
        catch
        {
            return true;
        }
    }

    private void UpdateLastSummaryAgeLabel()
    {
        if (!lastSummaryRefreshAt.HasValue)
        {
            lastSummaryAgeLabel = "n/d";
            lastSummaryAgeClass = string.Empty;
            summaryStalenessStartedAt = null;
            summaryStalenessIncidentLogged = false;
            return;
        }

        var elapsed = DateTime.UtcNow - lastSummaryRefreshAt.Value;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        if (elapsed.TotalHours >= 1)
        {
            lastSummaryAgeLabel = $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s";
        }
        else if (elapsed.TotalMinutes >= 1)
        {
            lastSummaryAgeLabel = $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds:D2}s";
        }
        else
        {
            lastSummaryAgeLabel = $"{elapsed.Seconds}s";
        }

        var isWarning = elapsed.TotalSeconds >= SummaryStalenessWarningSeconds;
        lastSummaryAgeClass = isWarning
            ? "admin-payments-summary-age admin-payments-summary-age--warning"
            : "admin-payments-summary-age";

        if (isWarning)
        {
            summaryStalenessStartedAt ??= DateTime.UtcNow;
            return;
        }

        summaryStalenessStartedAt = null;
        summaryStalenessIncidentLogged = false;
    }

    private async Task TryWriteStalenessIncidentAuditAsync()
    {
        if (summaryStalenessIncidentLogged || !summaryStalenessStartedAt.HasValue)
            return;

        var elapsedSinceStaleness = DateTime.UtcNow - summaryStalenessStartedAt.Value;
        if (elapsedSinceStaleness.TotalSeconds < SummaryStalenessAuditSeconds)
            return;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var actorUserId = authState.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

        await LogService.AuditAsync(
            eventType: AuditEvents.PaymentReconciliationPanelStale,
            entityType: AuditEntities.Payment,
            entityId: null,
            message: "Painel de reconciliação permaneceu sem atualização efetiva por mais de 5 minutos.",
            actorUserId: actorUserId,
            source: AdminAuditSources.Payments,
            level: "Warning",
            metadata: new
            {
                origin = "admin.payments.panel",
                staleForSeconds = (int)Math.Round(elapsedSinceStaleness.TotalSeconds, MidpointRounding.AwayFromZero),
                staleAuditThresholdSeconds = SummaryStalenessAuditSeconds,
                warningThresholdSeconds = SummaryStalenessWarningSeconds,
                autoRefreshEnabled = isAutoRefreshEnabled,
                pausedByVisibility = isAutoRefreshPausedByVisibility,
                lastSummaryRefreshAtUtc = lastSummaryRefreshAt
            });

        summaryStalenessIncidentLogged = true;
    }

    private void UpdateSeverity()
    {
        var isCritical = reconciliationStalePending >= 10 || pendingTrendDelta24h >= 10 || reconciliationPendingWithChargeId >= 25;
        var isWarning = reconciliationStalePending >= 3 || pendingTrendDelta24h >= pendingTrendWarningThreshold || reconciliationPendingWithChargeId >= 10;

        if (pendingTrendDelta24h >= pendingTrendCriticalThreshold)
        {
            isCritical = true;
        }

        if (isCritical)
        {
            reconciliationSeverityLabel = "Crítico";
            reconciliationSeverityClass = "admin-payments-severity-pill--critical";
            return;
        }

        if (isWarning)
        {
            reconciliationSeverityLabel = "Atenção";
            reconciliationSeverityClass = "admin-payments-severity-pill--warning";
            return;
        }

        reconciliationSeverityLabel = "OK";
        reconciliationSeverityClass = "admin-payments-severity-pill--ok";
    }

    private decimal GetPendingHeightPercent(SweepHistoryItem item)
    {
        var maxPending = Math.Max(1, automaticSweepHistory.Max(x => x.StillPending));
        var relative = (decimal)item.StillPending / maxPending;
        var height = Math.Max(12m, relative * 100m);
        return Math.Round(height, 2, MidpointRounding.AwayFromZero);
    }

    public sealed record SweepHistoryItem(
        string TimestampLabel,
        int Considered,
        int Updated,
        int StillPending,
        int NotFound);

    public sealed record GatewayTelemetryItem(
        string Gateway,
        int Pending,
        int StalePending,
        int PaidTotal,
        string PendingShareLabel,
        string SeverityLabel,
        string SeverityClass);

    private static (string Label, string CssClass) GetGatewaySeverity(int pending, int stalePending)
    {
        if (stalePending >= 3 || pending >= 10)
            return ("Crítico", "admin-payments-gateway-severity--critical");

        if (stalePending >= 1 || pending >= 5)
            return ("Atenção", "admin-payments-gateway-severity--warning");

        return ("OK", "admin-payments-gateway-severity--ok");
    }

    private static string BuildPendingTrendLabel(int? latestPending, int? baselinePending, out int pendingDelta)
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

    private static int? GetSweepMetric(string? metadataJson, string propertyName)
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

    private static bool HasSweepOrigin(string? metadataJson, string expectedOrigin)
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

    private static string BuildSweepDetails(string? metadataJson)
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

    private static string BuildStalenessLogsHref()
    {
        return AdminLogsDeepLinkBuilder.BuildPaymentPanelStaleLink(DateTime.Today);
    }

    private async Task RunSweepNowAsync()
    {
        isRunningSweep = true;
        sweepResultMessage = string.Empty;

        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var actorUserId = auth.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            var result = await EventPaymentReconciliationService.ReconcilePendingConfirmationsAsync(50, actorUserId);
            sweepResultIsError = result.StillPending > 0 || result.NotFound > 0;
            sweepResultMessage = result.Considered == 0
                ? "Nenhuma confirmação pendente para revalidar agora."
                : $"Varredura concluída: {result.Updated} atualizada(s), {result.StillPending} pendente(s), {result.NotFound} não encontrada(s).";

            await LoadOperationalSummaryAsync();
            await LoadPaymentsCountAsync();
        }
        catch (Exception ex)
        {
            sweepResultIsError = true;
            sweepResultMessage = $"Erro ao varrer pendências: {ex.Message}";
        }
        finally
        {
            isRunningSweep = false;
        }
    }

    private void DismissReconcileMessage()
    {
        reconcileResultMessage = string.Empty;
        reconcileResultConfirmationId = null;
    }

    private async Task ExportReconciliationCsvAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();

        var rawRows = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PixTxId != null || c.PaymentStatus != EventConfirmationPaymentStatus.Pending)
            .OrderByDescending(c => c.ConfirmedAt)
            .Select(c => new
            {
                c.Id,
                c.EventId,
                c.UserId,
                c.ConfirmedAt,
                c.PaymentStatus,
                c.PixTxId,
                c.PaymentGatewayName
            })
            .ToListAsync();

        var rows = rawRows.Select(c => new EventConfirmationReconciliationExportRow(
            c.Id,
            c.EventId,
            c.UserId,
            c.ConfirmedAt,
            c.PaymentStatus.ToString(),
            c.PixTxId,
            c.PaymentGatewayName));

        var csv = AdminLogsExportService.BuildEventConfirmationReconciliationCsv(rows);
        var fileName = AdminLogsExportService.BuildEventConfirmationReconciliationFileName();
        await JS.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, csv, "text/csv;charset=utf-8;");
    }

    private async Task ApplyStatusTransitionAsync()
    {
        if (!statusTransitionConfirmationId.HasValue || statusTransitionConfirmationId.Value <= 0)
        {
            statusTransitionResultIsError = true;
            statusTransitionResultMessage = "Informe um ConfirmationId válido.";
            return;
        }

        if (!Enum.TryParse<EventConfirmationPaymentStatus>(statusTransitionTarget, ignoreCase: true, out var targetStatus))
        {
            statusTransitionResultIsError = true;
            statusTransitionResultMessage = "Selecione um status alvo válido.";
            return;
        }

        isStatusTransitioning = true;
        statusTransitionResultMessage = string.Empty;

        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var actorUserId = auth.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            var result = await EventConfirmationPaymentStatusService.TransitionStatusAsync(
                confirmationId: statusTransitionConfirmationId.Value,
                targetStatus: targetStatus,
                actorUserId: actorUserId,
                reason: statusTransitionReason);

            statusTransitionResultIsError = !result.Found || !result.Updated;
            statusTransitionResultMessage = result.Message;

            if (result.ConfirmationId.HasValue)
            {
                timelineConfirmationId = result.ConfirmationId.Value;
            }

            await LoadOperationalSummaryAsync();
            await LoadPaymentsCountAsync();
        }
        catch (Exception ex)
        {
            statusTransitionResultIsError = true;
            statusTransitionResultMessage = $"Erro ao aplicar transição de status: {ex.Message}";
        }
        finally
        {
            isStatusTransitioning = false;
        }
    }

    private void OpenConfirmationTimeline()
    {
        if (timelineConfirmationId is null || timelineConfirmationId <= 0)
            return;

        NavigationManager.NavigateTo(BuildConfirmationTimelineHref(timelineConfirmationId.Value));
    }

    private static string BuildConfirmationTimelineHref(int confirmationId)
    {
        return $"/admin/audit/Payment/{confirmationId}";
    }

    private void DismissSweepMessage()
    {
        sweepResultMessage = string.Empty;
    }

    private void DismissStatusTransitionMessage()
    {
        statusTransitionResultMessage = string.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        if (isAdvancedToolsModalA11yActive)
        {
            try
            {
                await JS.InvokeVoidAsync("ConfirmaiModal.close");
            }
            catch
            {
                // Ignore JS interop errors during component disposal.
            }
            finally
            {
                isAdvancedToolsModalA11yActive = false;
            }
        }

        if (summaryAgeCts is not null)
        {
            summaryAgeCts.Cancel();

            if (summaryAgeTask is not null)
            {
                try
                {
                    await summaryAgeTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when disposing.
                }
            }

            summaryAgeCts.Dispose();
            summaryAgeCts = null;
        }

        if (summaryRefreshCts is not null)
        {
            summaryRefreshCts.Cancel();

            if (summaryRefreshTask is not null)
            {
                try
                {
                    await summaryRefreshTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when disposing.
                }
            }

            summaryRefreshCts.Dispose();
            summaryRefreshCts = null;
        }
    }
}
