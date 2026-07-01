using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class Admin
{
    private string selectedFiat = "USD";
    private bool isLoading = true;
    private int quoteQueriesCount;
    private int usersCount;
    private string selectedLanguage = Confirmai.Services.User.LanguagePreferenceService.DefaultLanguage;
    private decimal operationFeePercent;
    private string? operationFeeMessage;
    private string? languageMessage;
    private string? sitePixMessage;
    private bool operationFeeSaved;
    private bool sitePixSaved;
    private string siteIntermediaryPixKey = string.Empty;
    private ClaimsPrincipal? currentUser;
    private int adminPendingWithChargeId;
    private int adminStalePending;
    private string adminLastAutoSweepLabel = "Sem varredura automática registrada.";
    private string adminLastHealthRefreshLabel = "Ainda não atualizado.";
    private string adminPendingTrendLabel = "Sem dados suficientes para tendência.";
    private bool adminPendingTrendWarning;
    private int adminWarningThreshold = Confirmai.Services.Admin.AdminSettingsService.DefaultReconciliationWarningThreshold;
    private int adminCriticalThreshold = Confirmai.Services.Admin.AdminSettingsService.DefaultReconciliationCriticalThreshold;
    private string? reconciliationThresholdMessage;
    private bool reconciliationThresholdSaved;

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUser = authState.User;

        selectedFiat = CurrencyPreferenceService.SelectedFiatCurrency;
        var snapshot = await DashboardMetricsService.GetSnapshotAsync();
        usersCount = snapshot.UsersCount;
        quoteQueriesCount = snapshot.QuoteQueriesCount;
        operationFeePercent = await AdminSettingsService.GetOperationFeePercentForAdminAsync(currentUser);
        siteIntermediaryPixKey = await AdminSettingsService.GetSiteIntermediaryPixKeyForAdminAsync(currentUser) ?? string.Empty;
        selectedLanguage = LanguagePreferenceService.SelectedLanguage;
        var thresholds = await AdminSettingsService.GetReconciliationSeverityThresholdsForAdminAsync(currentUser);
        adminWarningThreshold = thresholds.warningThreshold;
        adminCriticalThreshold = thresholds.criticalThreshold;
        await LoadReconciliationHealthAsync();

        isLoading = false;
    }

    private async Task LoadReconciliationHealthAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();

        var staleCutoff = DateTime.UtcNow.AddMinutes(-30);
        adminPendingWithChargeId = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null);
        adminStalePending = await db.EventConfirmations.CountAsync(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending && c.PixTxId != null && c.ConfirmedAt <= staleCutoff);

        var latestWorkerSweepCandidates = await db.Logs
            .AsNoTracking()
            .Where(l =>
                l.EventType == AuditEvents.PaymentReconciliationSweep &&
                l.MetadataJson != null)
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToListAsync();

        var latestWorkerSweep = latestWorkerSweepCandidates
            .FirstOrDefault(l => HasSweepOrigin(l.MetadataJson, "worker.reconciliation"));

        if (latestWorkerSweep is null)
        {
            adminLastAutoSweepLabel = "Sem varredura automática registrada.";
            adminPendingTrendLabel = "Sem dados suficientes para tendência.";
            adminPendingTrendWarning = false;
            return;
        }

        adminLastAutoSweepLabel = latestWorkerSweep.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");

        var cutoff24h = DateTime.UtcNow.AddHours(-24);
        var baselineWorkerSweep = latestWorkerSweepCandidates
            .Where(l => l.Timestamp >= cutoff24h && HasSweepOrigin(l.MetadataJson, "worker.reconciliation"))
            .OrderBy(l => l.Timestamp)
            .FirstOrDefault();

        var latestPending = GetSweepMetric(latestWorkerSweep.MetadataJson, "stillPending");
        var baselinePending = baselineWorkerSweep is null
            ? (int?)null
            : GetSweepMetric(baselineWorkerSweep.MetadataJson, "stillPending");

        adminPendingTrendLabel = BuildPendingTrendLabel(latestPending, baselinePending, out var pendingDelta);
        adminPendingTrendWarning = pendingDelta >= adminWarningThreshold;
        adminLastHealthRefreshLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private async Task SaveReconciliationThresholdsAsync()
    {
        try
        {
            reconciliationThresholdSaved = await AdminSettingsService
                .SetReconciliationSeverityThresholdsForAdminAsync(currentUser, adminWarningThreshold, adminCriticalThreshold);

            if (reconciliationThresholdSaved)
            {
                reconciliationThresholdMessage = "Limiares de reconciliação salvos com sucesso.";
                await LoadReconciliationHealthAsync();
            }
            else
            {
                reconciliationThresholdMessage = "Valores inválidos. O limiar crítico deve ser maior ou igual ao limiar de atenção.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            reconciliationThresholdSaved = false;
            reconciliationThresholdMessage = T["AdminDashboard.AccessDeniedSettings"];
        }
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

    private async Task ApplyCurrencyPreferenceAsync()
    {
        CurrencyPreferenceService.SetCurrency(selectedFiat);
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", "Confirmai.fiatCurrency", CurrencyPreferenceService.SelectedFiatCurrency);
    }

    private async Task SaveOperationFeeAsync()
    {
        try
        {
            var saved = await AdminSettingsService.SetOperationFeePercentForAdminAsync(currentUser, operationFeePercent);
            operationFeeSaved = saved;

            if (saved)
            {
                operationFeeMessage = T["AdminDashboard.OperationFeeSaved"];
                operationFeePercent = await AdminSettingsService.GetOperationFeePercentForAdminAsync(currentUser);
                return;
            }

            operationFeeMessage = T["AdminDashboard.OperationFeeRangeError"];
        }
        catch (UnauthorizedAccessException)
        {
            operationFeeSaved = false;
            operationFeeMessage = T["AdminDashboard.AccessDeniedSettings"];
        }
    }

    private async Task SaveLanguageAsync()
    {
        LanguagePreferenceService.SetLanguage(selectedLanguage);

        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "Confirmai.uiLanguage", LanguagePreferenceService.SelectedLanguage);
        }
        catch
        {
            // Keep runtime update even if storage is unavailable.
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("ConfirmaiSetCookie", "Confirmai.uiLanguage", LanguagePreferenceService.SelectedLanguage, 365);
        }
        catch
        {
            // Keep runtime update even if cookie helper is unavailable.
        }

        selectedLanguage = LanguagePreferenceService.SelectedLanguage;
        languageMessage = T["AdminLanguages.Success"];
    }

    private async Task SaveSitePixKeyAsync()
    {
        try
        {
            var saved = await AdminSettingsService.SetSiteIntermediaryPixKeyForAdminAsync(currentUser, siteIntermediaryPixKey);
            sitePixSaved = saved;

            if (saved)
            {
                siteIntermediaryPixKey = await AdminSettingsService.GetSiteIntermediaryPixKeyForAdminAsync(currentUser) ?? string.Empty;
                sitePixMessage = "Chave PIX do intermedio salva com sucesso.";
                return;
            }

            sitePixMessage = "A chave PIX deve ter no maximo 160 caracteres.";
        }
        catch (UnauthorizedAccessException)
        {
            sitePixSaved = false;
            sitePixMessage = T["AdminDashboard.AccessDeniedSettings"];
        }
    }
}
