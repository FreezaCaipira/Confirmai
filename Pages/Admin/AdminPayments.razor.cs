using Confirmai.Models;
using Confirmai.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.JSInterop;
using Confirmai.Shared.Helpers;
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
        var result = await AdminPaymentsQueryService.GetPaymentsPageAsync(
            filterUserId, filterMinAmount, filterMaxAmount, filterStatus, filterDate,
            currentPage, PageSize);

        totalCount = result.TotalCount;
        totalPages = result.TotalPages;
        if (currentPage > totalPages)
            currentPage = totalPages;

        currentPagePayments = result.Payments;
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
        var summary = await AdminPaymentsSummaryService.LoadSummaryAsync();

        pendingTrendWarningThreshold = summary.WarningThreshold;
        pendingTrendCriticalThreshold = summary.CriticalThreshold;

        reconciliationPendingWithChargeId = summary.PendingWithChargeId;
        reconciliationPendingWithoutChargeId = summary.PendingWithoutChargeId;
        reconciliationStalePending = summary.StalePending;
        reconciliationPaidToday = summary.PaidToday;

        pendingByGatewayLabel = summary.PendingByGatewayLabel;
        paidByGatewayLabel = summary.PaidByGatewayLabel;

        gatewayTelemetry.Clear();
        gatewayTelemetry.AddRange(summary.GatewayTelemetry);

        lastAutomaticSweepLabel = summary.LastAutomaticSweepLabel;
        lastAutomaticSweepDetails = summary.LastAutomaticSweepDetails;
        lastManualSweepLabel = summary.LastManualSweepLabel;
        lastManualSweepDetails = summary.LastManualSweepDetails;

        pendingTrend24hLabel = summary.PendingTrend24hLabel;
        isPendingTrendWarning = summary.IsPendingTrendWarning;
        pendingTrendDelta24h = summary.PendingTrendDelta24h;

        automaticSweepHistory.Clear();
        automaticSweepHistory.AddRange(summary.AutomaticSweepHistory);

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
        var result = SeverityEvaluator.Evaluate(
            reconciliationStalePending,
            pendingTrendDelta24h,
            reconciliationPendingWithChargeId,
            pendingTrendWarningThreshold,
            pendingTrendCriticalThreshold);

        reconciliationSeverityLabel = result.Label;
        reconciliationSeverityClass = result.CssClass;
    }

    private decimal GetPendingHeightPercent(SweepHistoryItem item)
    {
        var maxPending = Math.Max(1, automaticSweepHistory.Max(x => x.StillPending));
        var relative = (decimal)item.StillPending / maxPending;
        var height = Math.Max(12m, relative * 100m);
        return Math.Round(height, 2, MidpointRounding.AwayFromZero);
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
        var (csv, fileName) = await AdminPaymentsQueryService.BuildReconciliationExportAsync();
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
