using System.Security.Claims;
using Confirmai.Enums;
using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Confirmai.Services.Admin;

public sealed record ReconcileChargeResult(
    bool IsError,
    string Message,
    int? ConfirmationId);

public sealed record SweepResult(
    bool IsError,
    string Message);

public sealed record StatusTransitionResult(
    bool IsError,
    string Message,
    int? ConfirmationId);

public sealed class AdminPaymentsCommandService
{
    private readonly EventPaymentReconciliationService _reconciliationService;
    private readonly EventConfirmationPaymentStatusService _statusService;
    private readonly AdminPaymentsQueryService _queryService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IJSRuntime _js;

    public AdminPaymentsCommandService(
        EventPaymentReconciliationService reconciliationService,
        EventConfirmationPaymentStatusService statusService,
        AdminPaymentsQueryService queryService,
        AuthenticationStateProvider authStateProvider,
        IJSRuntime js)
    {
        _reconciliationService = reconciliationService;
        _statusService = statusService;
        _queryService = queryService;
        _authStateProvider = authStateProvider;
        _js = js;
    }

    public async Task<ReconcileChargeResult> ReconcileChargeAsync(string chargeIdInput)
    {
        var chargeId = chargeIdInput?.Trim();
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            return new ReconcileChargeResult(true, "Informe um chargeId/txId para revalidar.", null);
        }

        try
        {
            var actorUserId = await GetActorUserIdAsync();
            var result = await _reconciliationService.ReconcileByChargeIdAsync(chargeId, actorUserId);
            return new ReconcileChargeResult(
                !result.Found || !result.IsPaid,
                result.Message,
                result.ConfirmationId);
        }
        catch (Exception ex)
        {
            return new ReconcileChargeResult(true, $"Erro ao revalidar cobrança: {ex.Message}", null);
        }
    }

    public async Task<SweepResult> RunSweepAsync()
    {
        try
        {
            var actorUserId = await GetActorUserIdAsync();
            var result = await _reconciliationService.ReconcilePendingConfirmationsAsync(50, actorUserId);
            var isError = result.StillPending > 0 || result.NotFound > 0;
            var message = result.Considered == 0
                ? "Nenhuma confirmação pendente para revalidar agora."
                : $"Varredura concluída: {result.Updated} atualizada(s), {result.StillPending} pendente(s), {result.NotFound} não encontrada(s).";
            return new SweepResult(isError, message);
        }
        catch (Exception ex)
        {
            return new SweepResult(true, $"Erro ao varrer pendências: {ex.Message}");
        }
    }

    public async Task<StatusTransitionResult> ApplyStatusTransitionAsync(
        int? confirmationId,
        string targetStatusString,
        string reason)
    {
        if (!confirmationId.HasValue || confirmationId.Value <= 0)
        {
            return new StatusTransitionResult(true, "Informe um ConfirmationId válido.", null);
        }

        if (!Enum.TryParse<EventConfirmationPaymentStatus>(targetStatusString, ignoreCase: true, out var targetStatus))
        {
            return new StatusTransitionResult(true, "Selecione um status alvo válido.", null);
        }

        try
        {
            var actorUserId = await GetActorUserIdAsync();
            var result = await _statusService.TransitionStatusAsync(
                confirmationId: confirmationId.Value,
                targetStatus: targetStatus,
                actorUserId: actorUserId,
                reason: reason);

            return new StatusTransitionResult(
                !result.Found || !result.Updated,
                result.Message,
                result.ConfirmationId);
        }
        catch (Exception ex)
        {
            return new StatusTransitionResult(true, $"Erro ao aplicar transição de status: {ex.Message}", null);
        }
    }

    public async Task ExportReconciliationCsvAsync()
    {
        var (csv, fileName) = await _queryService.BuildReconciliationExportAsync();
        await _js.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, csv, "text/csv;charset=utf-8;");
    }

    private async Task<string?> GetActorUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
    }
}
