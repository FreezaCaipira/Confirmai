using System.Security.Claims;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Factories;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Confirmai.Pages.Payment;

public partial class EventPayment : IAsyncDisposable
{
    private enum PayState { Idle, Generating, AwaitingPayment, Paid }

    [Parameter] public int ConfirmationId { get; set; }

    private EventConfirmation? conf;
    private bool isLoading = true;
    private string errorMsg = string.Empty;
    private string userId = string.Empty;
    private List<EventPaymentGatewayOption> availableGateways = new();
    private readonly List<EventPaymentGatewayOption> comingSoonGateways = [];
    private string selectedGatewayName = string.Empty;
    private bool groupGatewaysEnabled;
    private bool isAdminViewing;
    private bool adminMarkPaidLoading;
    private bool adminMarkPaidError;
    private bool adminConfirmPay;

    /// <summary>
    /// When gateways are enabled (V2), show the gateway selector.
    /// When disabled (V1 manual), hide it entirely.
    /// </summary>
    internal bool ShouldShowGateways => groupGatewaysEnabled;

    /// <summary>
    /// Show manual Pix (direct to organizer) + proof upload when gateways are off (V1),
    /// or when ShowDirectPixToOrganizer is explicitly enabled alongside gateways (V2).
    /// </summary>
    internal bool ShouldShowManualPix => !groupGatewaysEnabled || FeeOptions.Value.ShowDirectPixToOrganizer;

    /// <summary>
    /// True when the fixed platform fee is charged on top of the match price (V1 manual, futsal).
    /// </summary>
    internal bool ManualFeeApplies => ManualPlatformFee.Applies(
        groupGatewaysEnabled,
        conf?.Event?.Sport == Sport.Futsal,
        conf?.Event?.Price ?? 0m,
        FeeOptions.Value.ManualPlatformFeeFixed);

    /// <summary>
    /// Amount the player must transfer. Must match the value encoded in the Pix QR payload.
    /// </summary>
    internal decimal ManualAmountToPay => ManualPlatformFee.TotalToPay(
        groupGatewaysEnabled,
        conf?.Event?.Sport == Sport.Futsal,
        conf?.Event?.Price ?? 0m,
        FeeOptions.Value.ManualPlatformFeeFixed);

    private PayState payState = PayState.Idle;
    private string? brCode;
    private bool copied;
    private bool copiedAdminPix;
    private bool uploadingProof;
    private string proofError = string.Empty;
    private string proofSuccessMessage = string.Empty;

    private CancellationTokenSource? _pollCts;
    private CancellationTokenSource? _copyCts;

    [Inject] private EventPaymentService EventPaymentSvc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

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
            brCode = loadResult.PendingBrCode;
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
                errorMsg = result.ErrorMessage ?? Ui["Payment.GenerateChargeError"];
                payState = PayState.Idle;
                return;
            }

            brCode = result.BrCode;
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
            _logger.LogError(ex, "EventPayment: falha ao gerar cobranca. ConfirmationId={Id}", conf?.Id);
            errorMsg = Ui.Get("Payment.GenerateQrError");
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

    private async Task RunCopyTimerAsync(Func<bool> getFlag, Action<bool> setFlag)
    {
        _copyCts?.Cancel();
        _copyCts = new CancellationTokenSource();
        var ct = _copyCts.Token;
        try { setFlag(true); StateHasChanged(); await Task.Delay(2000, ct); if (!ct.IsCancellationRequested) setFlag(false); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { setFlag(false); }
    }

    private Task CopyBrCode() => RunCopyTimerAsync(() => copied, v => copied = v);

    private async Task CopyAdminPixKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        await RunCopyTimerAsync(() => copiedAdminPix, v => copiedAdminPix = v);
    }

    private void SelectGateway(string gatewayName) => selectedGatewayName = gatewayName;

    private string GetSelectedGatewayDisplayName() =>
        availableGateways.FirstOrDefault(g => string.Equals(g.Name, selectedGatewayName, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? "Pix";

    private static string? GetGroupAdminPixKey(Group group) => EventPaymentService.GetGroupAdminPixKey(group);
    private static string BuildPixStaticPayload(string pixKey, string groupName, string? city, decimal amount)
        => EventPaymentService.BuildPixStaticPayload(pixKey, groupName, city, amount);

    private IReadOnlyList<EventPaymentGatewayOption> GetComingSoonGateways() =>
        availableGateways.Count == 0 ? comingSoonGateways
        : comingSoonGateways.Where(g => !availableGateways.Any(a => string.Equals(a.Name, g.Name, StringComparison.OrdinalIgnoreCase))).ToList();

    private async Task UploadProof(InputFileChangeEventArgs e)
    {
        if (conf is null) return;
        const long MaxBytes = 5 * 1024 * 1024;
        var file = e.File;
        if (file is null) return;

        uploadingProof = true;
        proofError = string.Empty;
        StateHasChanged();

        try
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream(MaxBytes).CopyToAsync(ms);
            var bytes = ms.ToArray();

            var result = await PixProofUploadService.UploadProofAsync(conf.Id, bytes, file.ContentType);
            if (result.Success)
            {
                conf.PixProofImageData = bytes;
                conf.PixProofContentType = file.ContentType;
                conf.PixProofUploadedAt = result.UploadedAt;
                proofSuccessMessage = Ui.Get("Payment.ProofSuccess");
                StateHasChanged();
                await Task.Delay(1500);
                var backUrl = conf.Event.Sport == Sport.Futsal ? $"/futsal/{conf.Event.Id}" : $"/poker/{conf.Event.Id}";
                NavigationManager.NavigateTo(backUrl);
            }
            else
            {
                proofError = result.Message;
            }
        }
        catch (IOException)
        {
            proofError = Ui.Get("Payment.ProofTooLarge");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadProof: falha ao salvar comprovante. ConfirmationId={Id}", conf.Id);
            proofError = Ui.Get("Payment.ProofError");
        }
        finally
        {
            uploadingProof = false;
        }
    }

    private async Task AdminMarkPaid()
    {
        if (conf is null) return;
        adminMarkPaidLoading = true; adminMarkPaidError = false; StateHasChanged();
        try
        {
            var result = await AdminConfirmationService.TogglePaidAsync(conf.Id, userId);
            if (result.Found && result.Updated) { conf.HasPaid = true; conf.PaymentStatus = EventConfirmationPaymentStatus.Paid; payState = PayState.Paid; }
            else adminMarkPaidError = true;
        }
        catch { adminMarkPaidError = true; }
        finally { adminMarkPaidLoading = false; }
    }

    // Callback wrappers
    private async Task CopyBrCodeCallback() => await CopyBrCode();
    private Task SelectGatewayCallback(string n) { SelectGateway(n); return Task.CompletedTask; }
    private async Task GeneratePixChargeCallback() => await GeneratePixCharge();
    private async Task CopyAdminPixKeyCallback() { if (conf is not null) { var key = GetGroupAdminPixKey(conf.Event.Group); if (!string.IsNullOrWhiteSpace(key)) await CopyAdminPixKey(key); } }
    private async Task UploadProofCallback(InputFileChangeEventArgs e) => await UploadProof(e);

    public ValueTask DisposeAsync()
    {
        _pollCts?.Cancel(); _copyCts?.Cancel();
        _pollCts?.Dispose(); _copyCts?.Dispose();
        return ValueTask.CompletedTask;
    }
}
