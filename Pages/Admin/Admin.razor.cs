using System.Security.Claims;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class Admin
{
    [Inject] private ReconciliationHealthService ReconciliationHealth { get; set; } = default!;

    private string selectedFiat = "USD";
    private bool isLoading = true;
    private int quoteQueriesCount;
    private int usersCount;
    private string selectedLanguage = LanguagePreferenceService.DefaultLanguage;
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
    private string adminLastAutoSweepLabel = "Sem varredura automatica registrada.";
    private string adminLastHealthRefreshLabel = "Ainda nao atualizado.";
    private string adminPendingTrendLabel = "Sem dados suficientes para tendencia.";
    private bool adminPendingTrendWarning;
    private int adminWarningThreshold = AdminSettingsService.DefaultReconciliationWarningThreshold;
    private int adminCriticalThreshold = AdminSettingsService.DefaultReconciliationCriticalThreshold;
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
        var data = await ReconciliationHealth.LoadAsync(adminWarningThreshold);
        adminPendingWithChargeId = data.PendingWithChargeId;
        adminStalePending = data.StalePending;
        adminLastAutoSweepLabel = data.LastAutoSweepLabel;
        adminLastHealthRefreshLabel = data.LastHealthRefreshLabel;
        adminPendingTrendLabel = data.PendingTrendLabel;
        adminPendingTrendWarning = data.PendingTrendWarning;
    }

    private async Task SaveReconciliationThresholdsAsync()
    {
        try
        {
            reconciliationThresholdSaved = await AdminSettingsService
                .SetReconciliationSeverityThresholdsForAdminAsync(currentUser, adminWarningThreshold, adminCriticalThreshold);

            if (reconciliationThresholdSaved)
            {
                reconciliationThresholdMessage = "Limiares de reconciliacao salvos com sucesso.";
                await LoadReconciliationHealthAsync();
            }
            else
            {
                reconciliationThresholdMessage = "Valores invalidos. O limiar critico deve ser maior ou igual ao limiar de atencao.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            reconciliationThresholdSaved = false;
            reconciliationThresholdMessage = T["AdminDashboard.AccessDeniedSettings"];
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
        try { await JSRuntime.InvokeVoidAsync("localStorage.setItem", "Confirmai.uiLanguage", LanguagePreferenceService.SelectedLanguage); }
        catch { }
        try { await JSRuntime.InvokeVoidAsync("ConfirmaiSetCookie", "Confirmai.uiLanguage", LanguagePreferenceService.SelectedLanguage, 365); }
        catch { }
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
