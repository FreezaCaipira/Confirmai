using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Payments
{
    [Parameter] public int Id { get; set; }
    [Inject] private GroupPaymentsService GroupPayments { get; set; } = default!;

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
    private string paymentTab = "delinquent";
    private string? selectedPaymentUserId;
    private int? markingPaidId;
    private string? notifyingUserId;
    private HashSet<string> notifiedUserIds = new();
    private int? viewingProofConfirmationId;
    private string historyFilterUserId = "";

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

    private void HandleTabChange(string tab)
    {
        paymentTab = tab;
        selectedPaymentUserId = null;
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
}
