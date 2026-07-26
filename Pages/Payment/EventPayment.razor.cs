using System.Security.Claims;
using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Factories;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace Confirmai.Pages.Payment;

public partial class EventPayment : IAsyncDisposable
{
    private enum PayState { Idle, Generating, AwaitingPayment, Paid }

    [Parameter] public int ConfirmationId { get; set; }

    private EventConfirmation? conf;
    private bool    isLoading = true;
    private string  errorMsg  = string.Empty;
    private string  userId    = string.Empty;
    private List<EventPaymentGatewayOption> availableGateways = new();
    private readonly List<EventPaymentGatewayOption> comingSoonGateways = [];
    private string selectedGatewayName = string.Empty;
    private bool groupGatewaysEnabled;
    private bool isAdminViewing;
    private bool adminMarkPaidLoading;
    private bool adminMarkPaidError;
    private bool adminConfirmPay;

    private PayState payState = PayState.Idle;
    private string?  brCode;
    private bool     copied;
    private bool     copiedAdminPix;
    private bool     uploadingProof;
    private string   proofError = string.Empty;
    private string   proofSuccessMessage = string.Empty;

    private CancellationTokenSource? _pollCts;
    private CancellationTokenSource? _copyBrCodeCts;
    private CancellationTokenSource? _copyAdminPixCts;

    [Inject] private IOptions<FeeOptions> FeeOptions { get; set; } = default!;
    [Inject] private EventPaymentService EventPaymentSvc { get; set; } = default!;

    private Task? _copyBrCodeTask = null;
    private Task? _copyAdminPixTask = null;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        userId   = auth.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var loadResult = await EventPaymentSvc.LoadConfirmationAsync(ConfirmationId, userId);

        conf = loadResult.Confirmation;
        isAdminViewing = loadResult.IsAdminViewing;
        groupGatewaysEnabled = loadResult.GroupGatewaysEnabled;
        availableGateways = loadResult.AvailableGateways;
        selectedGatewayName = availableGateways.FirstOrDefault()?.Name ?? string.Empty;

        if (loadResult.RedirectUrl is not null)
        {
            NavigationManager.NavigateTo(loadResult.RedirectUrl);
            return;
        }

        if (loadResult.PendingBrCode is not null && loadResult.PendingChargeId is not null)
        {
            brCode   = loadResult.PendingBrCode;
            payState = PayState.AwaitingPayment;
            _ = StartPollingAsync(loadResult.PendingChargeId, loadResult.PendingGatewayName ?? string.Empty);
        }

        isLoading = false;
    }

    private async Task GeneratePixCharge()
    {
        if (conf is null || conf.Event.Price is null) return;

        payState = PayState.Generating;
        errorMsg = string.Empty;

        try
        {
            var result = await EventPaymentSvc.GeneratePixChargeAsync(conf.Id, selectedGatewayName, groupGatewaysEnabled);

            if (!result.Success)
            {
                errorMsg = result.ErrorMessage ?? "Erro ao gerar cobrança.";
                payState = PayState.Idle;
                return;
            }

            brCode   = result.BrCode;
            payState = PayState.AwaitingPayment;
            conf.PixTxId = result.ChargeId;
            conf.PixBrCode = result.BrCode;
            conf.PaymentGatewayName = result.GatewayName;
            conf.PaymentStatus = EventConfirmationPaymentStatus.Pending;
            conf.HasPaid = false;
            _ = StartPollingAsync(result.ChargeId!, result.GatewayName!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventPayment: falha ao gerar cobrança. ConfirmationId={Id}", conf?.Id);
            errorMsg = "Não foi possível gerar o QR Code. Tente novamente em instantes.";
            payState = PayState.Idle;
        }
    }

    private async Task StartPollingAsync(string chargeId, string gatewayName)
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        var ct = _pollCts.Token;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);

                var pollResult = await EventPaymentSvc.CheckPaymentStatusAsync(conf!.Id, chargeId, gatewayName);
                if (pollResult.IsPaid)
                {
                    conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                    conf.HasPaid = true;
                    payState = PayState.Paid;
                    return;
                }
            }
        }
        catch (TaskCanceledException) { }
    }

    private async Task CopyBrCode()
    {
        // No JS interop available without IJSRuntime injection;
        // simply set the copied flag for visual feedback.
        // The brCode is selectable via user-select:all CSS.
        _copyBrCodeCts?.Cancel();
        _copyBrCodeCts = new CancellationTokenSource();
        var ct = _copyBrCodeCts.Token;

        try
        {
            copied = true;
            StateHasChanged();
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested)
            {
                copied = false;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copied = false;
        }
    }

    private void SelectGateway(string gatewayName)
    {
        selectedGatewayName = gatewayName;
    }

    private string GetSelectedGatewayDisplayName()
    {
        return availableGateways
            .FirstOrDefault(g => string.Equals(g.Name, selectedGatewayName, StringComparison.OrdinalIgnoreCase))
            ?.DisplayName
            ?? "Pix";
    }

    /// <summary>
    /// Returns the Pix key of the group's designated payment receiver.
    /// Falls back to the first admin that has a PixKey configured.
    /// </summary>
    private static string? GetGroupAdminPixKey(Group group)
        => EventPaymentService.GetGroupAdminPixKey(group);

    private async Task CopyAdminPixKey(string key)
    {
        _copyAdminPixCts?.Cancel();
        _copyAdminPixCts = new CancellationTokenSource();
        var ct = _copyAdminPixCts.Token;

        try
        {
            copiedAdminPix = true;
            StateHasChanged();
            await Task.Delay(2000, ct);
            if (!ct.IsCancellationRequested)
            {
                copiedAdminPix = false;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            copiedAdminPix = false;
        }
    }

    private async Task UploadProof(InputFileChangeEventArgs e)
    {
        if (conf is null) return;
        const long MaxBytes = 5 * 1024 * 1024; // 5 MB
        var file = e.File;
        if (file is null) return;

        uploadingProof = true;
        proofError     = string.Empty;
        StateHasChanged();

        try
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream(MaxBytes).CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Use PixProofUploadService for consistent validation and persistence
            var result = await PixProofUploadService.UploadProofAsync(conf.Id, bytes, file.ContentType);

            if (result.Success)
            {
                conf.PixProofImageData = bytes;
                conf.PixProofContentType = file.ContentType;
                conf.PixProofUploadedAt = result.UploadedAt;
                proofSuccessMessage = "Comprovante enviado com sucesso! Redirecionando…";
                StateHasChanged();

                await Task.Delay(1500);

                var backUrl = conf.Event.Sport == Sport.Futsal
                    ? $"/futsal/{conf.Event.Id}"
                    : $"/poker/{conf.Event.Id}";
                NavigationManager.NavigateTo(backUrl);
            }
            else
            {
                proofError = result.Message;
            }
        }
        catch (IOException)
        {
            proofError = "Arquivo muito grande ou inválido. Máximo 5 MB.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadProof: falha ao salvar comprovante. ConfirmationId={Id}", conf.Id);
            proofError = "Erro ao enviar o comprovante. Tente novamente.";
        }
        finally
        {
            uploadingProof = false;
        }
    }

    /// <summary>Generates a Pix static BR Code (EMVco) payload for direct transfer QR codes.</summary>
    private async Task DismissProofSuccessAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(5000, ct);
            if (!ct.IsCancellationRequested)
            {
                proofSuccessMessage = string.Empty;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private static string BuildPixStaticPayload(string pixKey, string groupName, string? city, decimal amount)
        => EventPaymentService.BuildPixStaticPayload(pixKey, groupName, city, amount);

    private IReadOnlyList<EventPaymentGatewayOption> GetComingSoonGateways()
    {
        if (availableGateways.Count == 0)
        {
            return comingSoonGateways;
        }

        var availableNames = availableGateways
            .Select(g => g.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return comingSoonGateways
            .Where(g => !availableNames.Contains(g.Name))
            .ToList();
    }

    public async ValueTask DisposeAsync()
    {
        // Cancel all running tasks
        _pollCts?.Cancel();
        _copyBrCodeCts?.Cancel();
        _copyAdminPixCts?.Cancel();

        // Await running tasks to ensure cancellation is processed
        if (_copyBrCodeTask is not null)
        {
            try { await _copyBrCodeTask; }
            catch (OperationCanceledException) { }
        }

        if (_copyAdminPixTask is not null)
        {
            try { await _copyAdminPixTask; }
            catch (OperationCanceledException) { }
        }

        // Dispose all CancellationTokenSource instances
        _pollCts?.Dispose();
        _copyBrCodeCts?.Dispose();
        _copyAdminPixCts?.Dispose();
    }

    private async Task AdminMarkPaid()
    {
        if (conf is null) return;
        adminMarkPaidLoading = true;
        adminMarkPaidError = false;
        StateHasChanged();
        try
        {
            var result = await AdminConfirmationService.TogglePaidAsync(conf.Id, userId);
            if (result.Found && result.Updated)
            {
                conf.HasPaid = true;
                conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                payState = PayState.Paid;
            }
            else
            {
                adminMarkPaidError = true;
            }
        }
        catch
        {
            adminMarkPaidError = true;
        }
        finally
        {
            adminMarkPaidLoading = false;
        }
    }

    // Callback wrappers for child components
    private async Task CopyBrCodeCallback()
        => await CopyBrCode();

    private Task SelectGatewayCallback(string gatewayName)
    { SelectGateway(gatewayName); return Task.CompletedTask; }

    private async Task GeneratePixChargeCallback()
        => await GeneratePixCharge();

    private async Task CopyAdminPixKeyCallback()
    {
        if (conf is null) return;
        var key = GetGroupAdminPixKey(conf.Event.Group);
        if (!string.IsNullOrWhiteSpace(key))
            await CopyAdminPixKey(key);
    }

    private async Task UploadProofCallback(InputFileChangeEventArgs e)
        => await UploadProof(e);
}
