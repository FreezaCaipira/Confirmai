using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Payments
{
    [Parameter] public int Id { get; set; }
    [Inject] private GroupPaymentsService GroupPayments { get; set; } = default!;
    [Inject] private PlatformFeeSettlementQueryService FeeQuery { get; set; } = default!;
    [Inject] private PlatformFeeSettlementService FeeSettlement { get; set; } = default!;

    public record HistoryEntry(DateTime EventDate, decimal EventPrice, string AdminName, string EventHref, bool HasProof, int? ConfirmationId);
    public record HistoryGroup(string UserName, List<HistoryEntry> Entries, decimal TotalAmount);

    private Group? group;
    private bool isLoading = true;
    private bool isAdmin;
    private string? currentUserId;
    private bool isLoadingPayments;
    private List<UserDelinquency> delinquencyList = new();
    private List<PaymentHistoryEntry> paymentHistory = new();
    private List<PendingProofEntry> pendingProofList = new();
    private string paymentTab = "proofs";
    private string? selectedPaymentUserId;
    private int? markingPaidId;
    private string? notifyingUserId;
    private HashSet<string> notifiedUserIds = new();
    private int? viewingProofConfirmationId;
    private string historyFilterUserId = "";
    private int? rejectingProofId;

    // ── Platform fee settlement (Fase A do Ciclo 27) ──────────────────────
    private PlatformFeeOverview? feeOverview;
    private bool isLoadingFee;
    private string settlementError = string.Empty;
    private string settlementSuccess = string.Empty;
    private bool isSubmittingSettlement;
    private int? viewingSettlementProofId;
    private const int FeeMatchesPreviewCount = 4;
    private bool showAllFeeMatches;
    private HashSet<int> selectedFeeEventIds = new();
    private IBrowserFile? selectedProofFile;
    private bool showSettlementConfirmModal;

    /// <summary>Matches pending settlement (only those with Pendente status are selectable).</summary>
    private IReadOnlyList<PlatformFeeMatch> pendingFeeMatches =>
        feeOverview is null
            ? new List<PlatformFeeMatch>()
            : feeOverview.Matches.Where(m => m.Status == PlatformFeeMatchStatus.Pendente).ToList();

    /// <summary>Matches already settled (shown but not selectable).</summary>
    private IReadOnlyList<PlatformFeeMatch> settledFeeMatches =>
        feeOverview is null
            ? new List<PlatformFeeMatch>()
            : feeOverview.Matches.Where(m => m.Status == PlatformFeeMatchStatus.Pago).ToList();

    private IReadOnlyList<PlatformFeeMatch> displayedFeeMatches =>
        feeOverview is null
            ? new List<PlatformFeeMatch>()
            : showAllFeeMatches
                ? feeOverview.Matches
                : feeOverview.Matches.Take(FeeMatchesPreviewCount).ToList();

    /// <summary>Sum of fees for the currently selected pending matches.</summary>
    private decimal SelectedFeeAmount =>
        feeOverview is null
            ? 0m
            : PlatformFeeSelectionState.SelectedAmount(feeOverview.Matches, selectedFeeEventIds);

    private void ToggleFeeMatchSelection(int eventId)
    {
        if (!selectedFeeEventIds.Remove(eventId))
            selectedFeeEventIds.Add(eventId);
    }

    private void SelectAllPendingFeeMatches()
    {
        selectedFeeEventIds = pendingFeeMatches.Select(m => m.EventId).ToHashSet();
    }

    private void ClearFeeSelection()
    {
        selectedFeeEventIds.Clear();
    }

    private void ToggleFeeMatchesExpansion()
    {
        showAllFeeMatches = !showAllFeeMatches;
    }

    /// <summary>True when the platform fee tab should be offered (futsal, manual mode).</summary>
    private bool ShouldShowPlatformFeeTab =>
        group is not null && group.Sport == Sport.Futsal && !group.EnablePaymentGateways;

    /// <summary>Pix BR Code payload for the platform key, charging the due amount.</summary>
    private string? PlatformFeePixPayload
    {
        get
        {
            if (feeOverview is null || string.IsNullOrWhiteSpace(feeOverview.PlatformPixKey))
                return string.Empty;
            return EventPaymentService.BuildPixStaticPayload(
                feeOverview.PlatformPixKey,
                "Confirmai",
                feeOverview.PlatformPixCity,
                feeOverview.Due);
        }
    }

    private List<PaymentHistoryEntry>? filteredHistory =>
        string.IsNullOrEmpty(historyFilterUserId)
            ? paymentHistory
            : paymentHistory?.Where(h => h.UserName == historyFilterUserId).ToList();

    private List<HistoryGroup>? groupedHistory =>
        filteredHistory?
            .GroupBy(h => h.UserName)
            .Select(g => new HistoryGroup(
                g.Key,
                g.Select(h => new HistoryEntry(h.EventDate, h.EventPrice, h.AdminName, h.EventHref, h.HasProof, h.ConfirmationId)).ToList(),
                g.Sum(h => h.EventPrice)))
            .OrderByDescending(g => g.TotalAmount)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        currentUserId = await GroupPayments.GetCurrentUserIdAsync();
        (group, isAdmin) = await GroupPayments.LoadGroupAndCheckAdminAsync(Id, currentUserId);
        if (group is null || !isAdmin)
        {
            isLoading = false;
            return;
        }
        await LoadPaymentsData();
        isLoading = false;
    }

    private async Task HandleTabChange(string tab)
    {
        paymentTab = tab;
        selectedPaymentUserId = null;
        showAllFeeMatches = false;
        selectedFeeEventIds.Clear();
        if (tab == "platformfee" && feeOverview is null)
            await LoadFeeOverviewAsync();
    }

    private async Task RefreshPayments()
    {
        isLoadingPayments = true;
        await LoadPaymentsData();
        isLoadingPayments = false;
    }

    private async Task LoadPaymentsData()
    {
        if (group is null) return;
        var data = await GroupPayments.LoadPaymentsDataAsync(Id, group);
        delinquencyList = data.DelinquencyList;
        paymentHistory = data.PaymentHistory;
        pendingProofList = data.PendingProofList;
    }

    private void SelectPaymentUser(string userId)
    {
        selectedPaymentUserId = selectedPaymentUserId == userId ? null : userId;
    }

    private async Task HandleMarkPaidAsync((int ConfirmationId, string UserId) args)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", "Tem certeza que deseja confirmar este pagamento?");
        if (confirmed)
            await AdminMarkPaid(args.ConfirmationId, args.UserId);
    }

    private async Task HandleNotifyDelinquencyAsync(UserDelinquency delinquency)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", $"Deseja notificar {delinquency.UserName} sobre os pagamentos em aberto? Uma mensagem sera enviada pelo sistema interno.");
        if (confirmed)
            await AdminNotifyDelinquency(delinquency);
    }

    private Task HandleViewProof(int confirmationId)
    {
        viewingProofConfirmationId = confirmationId;
        return Task.CompletedTask;
    }

    private async Task AdminMarkPaid(int confirmationId, string userId)
    {
        markingPaidId = confirmationId;
        await GroupPayments.MarkPaidAsync(confirmationId, userId, currentUserId, Id);
        markingPaidId = null;
        await LoadPaymentsData();
        if (!delinquencyList.Any(d => d.UserId == userId))
            selectedPaymentUserId = null;
    }

    private async Task HandleAcceptProofAsync(PendingProofEntry proof)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", Ui["Group.PaymentsProofAcceptConfirm"]);
        if (!confirmed) return;
        await AdminMarkPaid(proof.ConfirmationId, proof.UserId);
    }

    private async Task HandleRejectProofAsync(PendingProofEntry proof)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", Ui["Group.PaymentsProofRejectConfirm"]);
        if (!confirmed) return;
        rejectingProofId = proof.ConfirmationId;
        await GroupPayments.RejectProofAsync(proof.ConfirmationId, currentUserId, Id);
        rejectingProofId = null;
        await LoadPaymentsData();
    }

    private async Task AdminNotifyDelinquency(UserDelinquency d)
    {
        if (currentUserId is null || group is null) return;
        notifyingUserId = d.UserId;
        await GroupPayments.NotifyDelinquencyAsync(d, currentUserId, group.Name, Id);
        notifyingUserId = null;
        notifiedUserIds.Add(d.UserId);
    }

    private async Task OpenWhatsAppDelinquency(UserDelinquency delinquency)
    {
        if (group is null) return;
        var count = delinquency.Entries.Count;
        var total = delinquency.TotalAmount.ToString("F2");
        var plural = count != 1 ? "s" : "";
        var msg = Uri.EscapeDataString(
            $"Ola, {delinquency.UserName}! 👋\n\n" +
            $"Voce tem {count} partida{plural} sem pagamento em \"{group.Name}\":\n\n" +
            string.Join("\n", delinquency.Entries.Select(e =>
                $"• {e.EventDate.ToLocalTime():dd/MM/yyyy} — R$ {e.EventPrice:F2}")) +
            $"\n\nTotal: R$ {total}\n\n" +
            $"Quando puder, regularize no Confirmai 🙏");
        var url = $"https://wa.me/?text={msg}";
        await JS.InvokeVoidAsync("open", url, "_blank", "noopener,noreferrer");
    }

    // ── Platform fee settlement (Fase A do Ciclo 27) ──────────────────────

    private async Task LoadFeeOverviewAsync()
    {
        isLoadingFee = true;
        try
        {
            feeOverview = await FeeQuery.GetGroupFeeOverviewAsync(Id);
        }
        finally
        {
            isLoadingFee = false;
        }
    }

    private async Task RefreshFeeOverview()
    {
        isLoadingFee = true;
        try
        {
            feeOverview = await FeeQuery.GetGroupFeeOverviewAsync(Id);
        }
        finally
        {
            isLoadingFee = false;
        }
    }

    private void HandleProofFileSelected(InputFileChangeEventArgs e)
    {
        selectedProofFile = e.File;
        settlementError = string.Empty;
        settlementSuccess = string.Empty;
    }

    private void ShowSettlementConfirmModal()
    {
        if (!PlatformFeeSelectionState.CanSubmit(selectedFeeEventIds))
        {
            settlementError = Ui["Group.PlatformFeeSelectMatches"];
            return;
        }
        if (selectedProofFile is null) return;
        showSettlementConfirmModal = true;
    }

    private void CancelSettlementConfirm()
    {
        showSettlementConfirmModal = false;
    }

    private async Task ConfirmSettlementSubmit()
    {
        if (currentUserId is null || group is null || feeOverview is null || selectedProofFile is null) return;

        const long MaxBytes = 5 * 1024 * 1024;
        var file = selectedProofFile;

        settlementError = string.Empty;
        settlementSuccess = string.Empty;
        isSubmittingSettlement = true;

        try
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream(MaxBytes).CopyToAsync(ms);
            var bytes = ms.ToArray();

            var result = await FeeSettlement.SubmitSettlementAsync(
                group.Id, currentUserId, SelectedFeeAmount, bytes, file.ContentType,
                selectedFeeEventIds.ToList());

            if (result.Success)
            {
                settlementSuccess = Ui["Group.PlatformFeeSubmitSuccess"];
                selectedFeeEventIds.Clear();
                selectedProofFile = null;
                showSettlementConfirmModal = false;
                await RefreshFeeOverview();
            }
            else
            {
                settlementError = result.Message;
                showSettlementConfirmModal = false;
            }
        }
        catch (IOException)
        {
            settlementError = Ui["Group.PlatformFeeSubmitError"];
            showSettlementConfirmModal = false;
        }
        catch (Exception)
        {
            settlementError = Ui["Group.PlatformFeeSubmitError"];
            showSettlementConfirmModal = false;
        }
        finally
        {
            isSubmittingSettlement = false;
        }
    }

    private Task ViewSettlementProof(int settlementId)
    {
        viewingSettlementProofId = settlementId;
        return Task.CompletedTask;
    }
}
