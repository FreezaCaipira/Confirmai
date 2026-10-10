using System.Security.Claims;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Groups;

public record GroupPaymentsData(
    List<UserDelinquency> DelinquencyList,
    List<PaymentHistoryEntry> PaymentHistory,
    List<PendingProofEntry> PendingProofList);

/// <summary>Pending-payment count for one group (home banner, C36-C Fase 4).</summary>
public record PendingPaymentGroup(int GroupId, string GroupName, int Count);

/// <summary>
/// One row of the member-facing "Meus pagamentos" view: only the caller's own
/// confirmations, so a player never sees another member's payment state.
/// </summary>
public record MyPaymentEntry(
    int ConfirmationId,
    int EventId,
    DateTime EventDate,
    decimal TotalToPay,
    bool HasPaid,
    bool ProofPending,
    string EventHref,
    string PayHref);

public sealed class GroupPaymentsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly EventNotificationService _notificationService;
    private readonly LogService _logService;
    private readonly PlatformFeeLedgerService _feeLedger;
    private readonly PlatformFeePolicy _feePolicy;
    private readonly ILogger<GroupPaymentsService> _logger;
    private readonly Services.Notification.WhatsAppDispatchService? _whatsApp;

    public GroupPaymentsService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        EventNotificationService notificationService,
        LogService logService,
        PlatformFeeLedgerService feeLedger,
        PlatformFeePolicy feePolicy,
        ILogger<GroupPaymentsService> logger,
        Services.Notification.WhatsAppDispatchService? whatsApp = null)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _notificationService = notificationService;
        _logService = logService;
        _feeLedger = feeLedger;
        _whatsApp = whatsApp;
        _feePolicy = feePolicy;
        _logger = logger;
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        var auth = await _authStateProvider.GetAuthenticationStateAsync();
        return auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<(Group? Group, bool IsAdmin)> LoadGroupAndCheckAdminAsync(int groupId, string? currentUserId)
    {
        var (group, isAdmin, _) = await LoadGroupAccessAsync(groupId, currentUserId);
        return (group, isAdmin);
    }

    /// <summary>
    /// Loads the group and resolves the caller's relationship to it in one query.
    /// The page uses IsMember to gate the whole route and IsAdmin to choose
    /// between the management view and the member's own "Meus pagamentos".
    /// </summary>
    public async Task<(Group? Group, bool IsAdmin, bool IsMember)> LoadGroupAccessAsync(
        int groupId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group is null) return (null, false, false);

        var isMember = currentUserId is not null &&
                       group.Members.Any(m => m.UserId == currentUserId);
        var isAdmin = isMember &&
                      group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);

        return (group, isAdmin, isMember);
    }

    /// <summary>
    /// How many of the caller's confirmations in this group are actionable debts
    /// (same rule as <see cref="PendingByGroup"/>). Feeds the "Pagamentos" badge
    /// on the group hub — membership is revalidated here, never trusted from UI.
    /// </summary>
    public async Task<int> CountMyPendingAsync(int groupId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (currentUserId is null ||
            !await GroupAccess.IsMemberAsync(db, groupId, currentUserId))
            return 0;

        // C39-A: preco canonico — carimbo da confirmacao vence (cash cobra
        // pela mesa sem Event.Price); EF traduz para COALESCE.
        var candidates = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.UserId == currentUserId &&
                c.Event.IsActive &&
                (c.ChargedPrice ?? c.Event.Price) > 0 &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                !c.HasPaid &&
                c.Position != FutsalPosition.Goalkeeper &&
                c.PixProofUploadedAt == null)
            .Select(c => new { c.Id, c.EventId, Sport = c.Event.Sport, MaxPlayers = c.Event.MaxPlayers })
            .ToListAsync();

        // C39-A F5: quem esta na espera do poker nao conta como pendente.
        var waitlisted = await PokerWaitlistedIdsAsync(db, candidates.Select(c => (c.EventId, c.Sport, c.MaxPlayers)));
        return candidates.Count(c => !waitlisted.Contains(c.Id));
    }

    /// <summary>
    /// C39-A F5: ids das confirmacoes alem do MaxPlayers nos eventos de poker
    /// do lote — inadimplencia, saida-com-divida e "Meus pagamentos" ignoram
    /// quem esta na espera (sem vaga, sem divida).
    /// </summary>
    private static async Task<HashSet<int>> PokerWaitlistedIdsAsync(
        AppDbContext db,
        IEnumerable<(int EventId, Sport Sport, int MaxPlayers)> events)
    {
        var pokerEvents = events
            .Where(e => e.Sport == Sport.Poker && e.MaxPlayers > 0)
            .GroupBy(e => e.EventId)
            .ToDictionary(g => g.Key, g => g.First().MaxPlayers);
        if (pokerEvents.Count == 0) return new HashSet<int>();

        var all = (await db.EventConfirmations
            .Where(c => pokerEvents.Keys.Contains(c.EventId))
            .Select(c => new { c.Id, c.EventId, c.ConfirmedAt })
            .ToListAsync())
            .Select(c => (c.Id, c.EventId, c.ConfirmedAt));
        return EventCharge.WaitlistedIds(all, pokerEvents);
    }

    /// <summary>
    /// The caller's own payment state inside the group: every priced, non-goalkeeper
    /// confirmation they hold, newest first. Membership is enforced here — the page
    /// must not rely on a UI check to decide whose rows are returned.
    /// The stamped platform fee wins (C36-C Fase 0); legacy rows resolve by ConfirmedAt.
    /// </summary>
    public async Task<List<MyPaymentEntry>> LoadMyPaymentsAsync(int groupId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (currentUserId is null ||
            !await GroupAccess.IsMemberAsync(db, groupId, currentUserId))
            return new List<MyPaymentEntry>();

        var group = await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null) return new List<MyPaymentEntry>();

        var confirmations = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.UserId == currentUserId &&
                (c.ChargedPrice ?? c.Event.Price) > 0 &&
                c.Position != FutsalPosition.Goalkeeper)
            .Include(c => c.Event)
            .OrderByDescending(c => c.Event.StartsAt)
            .Take(100)
            .ToListAsync();

        // C39-A F5: inscricao na espera nao aparece como pagamento devido.
        var waitlisted = await PokerWaitlistedIdsAsync(db,
            confirmations.Select(c => (c.EventId, c.Event.Sport, c.Event.MaxPlayers)));
        confirmations = confirmations.Where(c => !waitlisted.Contains(c.Id)).ToList();

        var isFutsal = group.Sport == Sport.Futsal;
        return confirmations.Select(c =>
        {
            var href = isFutsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
            // The stamp is what the player was charged — it always wins.
            var price = EventCharge.PriceOf(c)!.Value;
            var total = c.PlatformFeeAmount.HasValue
                ? price + c.PlatformFeeAmount.Value
                : ManualPlatformFee.TotalToPay(group.EnablePaymentGateways, price,
                    _feePolicy.ResolveStampForNewConfirmation(
                        group, price, c.ConfirmedAt, c.Event.PlatformFeePercent) ?? 0m);
            return new MyPaymentEntry(
                c.Id, c.EventId, c.Event.StartsAt, total,
                c.HasPaid, c.PixProofUploadedAt != null && !c.HasPaid,
                href, $"/pagamento/evento/{c.Id}");
        }).ToList();
    }

    public async Task<GroupPaymentsData> LoadPaymentsDataAsync(int groupId, Group group)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var nowUtc = DateTime.UtcNow;
        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
        var sport = group.Sport;
        var gatewaysEnabled = group.EnablePaymentGateways;
        var isFutsal = sport == Sport.Futsal;

        // The player pays Price + ManualPlatformFeeFixed when the manual fee
        // applies (futsal, no gateways, price > 0, fee > 0). The admin screens
        // must show the same total the player was asked to pay — otherwise the
        // proof says R$ 15,75 but the review screen shows R$ 15,00.
        // C36-C Fase 0: the fee is per-confirmation — the stamped value wins;
        // legacy unstamped rows resolve by ConfirmedAt (a later waiver must not
        // rewrite what the player was charged).
        // C36-C Fase 0 / review: a stamped fee is what the player was actually
        // charged — it always wins, even if the group later enables gateways.
        // Only unstamped (legacy) rows go through the live Applies() check.
        decimal TotalToPay(decimal basePrice, DateTime confirmedAt, decimal? stampedFee, decimal? feePercent = null)
            => stampedFee.HasValue
                ? basePrice + stampedFee.Value
                : ManualPlatformFee.TotalToPay(gatewaysEnabled, basePrice,
                    _feePolicy.ResolveStampForNewConfirmation(
                        group, basePrice, confirmedAt, feePercent) ?? 0m);

        var unpaidConfirmations = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.Event.StartsAt < nowUtc &&
                (c.ChargedPrice ?? c.Event.Price) > 0 &&
                !c.HasPaid &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderBy(c => c.Event.StartsAt)
            .Select(c => new {
                c.Id, c.UserId, c.EventId, c.Event, c.User, c.PaymentGatewayName, c.Position,
                c.PlatformFeeAmount, c.ConfirmedAt, c.ChargedPrice,
                HasProof = c.PixProofUploadedAt != null,
            })
            .ToListAsync();

        // C39-A F5: quem esta na espera do poker nao e inadimplente.
        var waitlistedIds = await PokerWaitlistedIdsAsync(db,
            unpaidConfirmations.Select(c => (c.EventId, c.Event!.Sport, c.Event.MaxPlayers)));

        var delinquencyList = unpaidConfirmations
            .Where(c => !waitlistedIds.Contains(c.Id))
            .GroupBy(c => c.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                var userName = user?.FullName ?? user?.UserName ?? "Jogador";
                var entries = g.Select(c =>
                {
                    var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
                    return new DelinquencyEntry(c.Id, c.EventId, c.Event.StartsAt,
                        TotalToPay((c.ChargedPrice ?? c.Event!.Price)!.Value, c.ConfirmedAt, c.PlatformFeeAmount, c.Event!.PlatformFeePercent), href, c.HasProof);
                }).ToList();
                return new UserDelinquency(g.Key, userName, entries);
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();

        var manualPaid = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.HasPaid &&
                c.MarkedPaidByUserId != null &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderByDescending(c => c.MarkedPaidAt)
            .Take(50)
            .ToListAsync();

        var adminIds = manualPaid.Select(c => c.MarkedPaidByUserId!).Distinct().ToList();
        var adminUsers = await db.Users
            .Where(u => adminIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? u.Email })
            .ToListAsync();
        var adminMap = adminUsers.ToDictionary(u => u.Id, u => u.Name ?? "Admin");

        var paymentHistory = manualPaid.Select(c =>
        {
            var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
            var userName = c.User?.FullName ?? c.User?.UserName ?? "Jogador";
            var adminName = adminMap.TryGetValue(c.MarkedPaidByUserId!, out var n) ? n : "Admin";
            // History shows what was actually charged: the stamped fee snapshot when
            // it exists (a waiver granted later must not rewrite past charges).
            var amount = TotalToPay(EventCharge.PriceOf(c) ?? 0, c.ConfirmedAt, c.PlatformFeeAmount, c.Event?.PlatformFeePercent);
            return new PaymentHistoryEntry(userName, c.Event!.StartsAt, amount, href, adminName, c.MarkedPaidAt!.Value, c.Id, c.PixProofImageData != null && c.PixProofImageData.Length > 0);
        }).ToList();

        var pendingProofs = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.PixProofUploadedAt != null &&
                !c.HasPaid &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderByDescending(c => c.PixProofUploadedAt)
            .ToListAsync();

        var pendingProofList = pendingProofs.Select(c =>
        {
            var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
            var userName = c.User?.FullName ?? c.User?.UserName ?? "Jogador";
            var eventName = c.Event?.Location ?? "Partida";
            return new PendingProofEntry(c.Id, c.UserId, userName, c.EventId, eventName, group.Name, c.Event!.StartsAt, TotalToPay(EventCharge.PriceOf(c) ?? 0, c.ConfirmedAt, c.PlatformFeeAmount, c.Event!.PlatformFeePercent), href, c.PixProofUploadedAt!.Value);
        }).ToList();

        return new GroupPaymentsData(delinquencyList, paymentHistory, pendingProofList);
    }

    /// <summary>
    /// The player's own unpaid priced confirmations, grouped per group — feeds the
    /// home pending-payments banner (C36-C Fase 4). A proof already uploaded is
    /// under review, not an actionable debt; cancelled events and goalkeepers
    /// never owe. Operates on already-loaded confirmations of a single user, so
    /// no other member's data can leak into the result.
    /// </summary>
    public static List<PendingPaymentGroup> PendingByGroup(IEnumerable<EventConfirmation> confirmations)
        => confirmations
            .Where(c => c.Event.IsActive &&
                        EventCharge.PriceOf(c) > 0 &&
                        c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                        !c.HasPaid &&
                        c.Position != FutsalPosition.Goalkeeper &&
                        c.PixProofUploadedAt == null)
            .GroupBy(c => new { c.Event.GroupId, c.Event.Group.Name })
            .Select(g => new PendingPaymentGroup(g.Key.GroupId, g.Key.Name, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

    public async Task MarkPaidAsync(int confirmationId, string userId, string? currentUserId, int groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await GroupAccess.IsGroupAdminAsync(db, groupId, currentUserId)) return;
        var conf = await db.EventConfirmations
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);
        // The confirmation must belong to the group the caller administers —
        // otherwise an admin of group A could mark payments in group B.
        if (conf is not null && conf.Event?.GroupId == groupId && !conf.HasPaid && conf.PaymentGatewayName == null)
        {
            conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
            conf.HasPaid = true;
            conf.MarkedPaidByUserId = currentUserId;
            conf.MarkedPaidAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await _logService.AuditAsync(
                AuditEvents.EventConfirmationPaidManual,
                AuditEntities.EventConfirmation,
                confirmationId.ToString(),
                $"Admin marcou confirmacao #{confirmationId} do jogador {userId} como paga manualmente (grupo #{groupId})",
                currentUserId, "GroupAdmin");

            // Stamp the platform fee on this confirmation (V1 manual, futsal only).
            // Same call AdminConfirmationService.TogglePaidAsync makes — without it,
            // the fee never accrues and the organizer never sees these matches in
            // the "Taxa da plataforma" tab.
            try
            {
                await _feeLedger.StampFeeOnPaidAsync(confirmationId);
            }
            catch (Exception ex)
            {
                // Non-blocking for the admin, but never silent: an unstamped fee is revenue lost.
                _logger.LogError(ex,
                    "Falha ao carimbar a taxa da plataforma na confirmacao {ConfirmationId}", confirmationId);
            }
        }
    }

    public async Task NotifyDelinquencyAsync(UserDelinquency d, string? currentUserId, string groupName, int groupId)
    {
        if (currentUserId is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await GroupAccess.IsGroupAdminAsync(db, groupId, currentUserId)) return;

        var entries = d.Entries.Select(e => (e.EventDate, e.EventPrice)).ToList();

        await _notificationService.NotifyDelinquencyAsync(
            adminUserId: currentUserId,
            targetUserId: d.UserId,
            groupName: groupName,
            entries: entries);

        await _logService.AuditAsync(
            AuditEvents.DelinquencyNotified,
            AuditEntities.Group,
            groupId.ToString(),
            $"Admin notificou jogador {d.UserId} ({d.UserName}) sobre {d.Entries.Count} partida(s) em aberto - R$ {d.TotalAmount:F2} (grupo #{groupId})",
            currentUserId, "GroupAdmin");

        if (_whatsApp is not null)
            await _whatsApp.DispatchPaymentPendingAsync(groupId, groupName);
    }

    /// <summary>
    /// Rejects a player's uploaded Pix proof: clears the proof image and timestamp,
    /// keeping the confirmation (player stays confirmed but unpaid).
    /// </summary>
    public async Task RejectProofAsync(int confirmationId, string? currentUserId, int groupId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await GroupAccess.IsGroupAdminAsync(db, groupId, currentUserId)) return;
        var conf = await db.EventConfirmations
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);
        if (conf is null || conf.Event?.GroupId != groupId || conf.PixProofUploadedAt is null) return;

        conf.PixProofImageData = null;
        conf.PixProofContentType = null;
        conf.PixProofUploadedAt = null;
        await db.SaveChangesAsync();

        await _logService.AuditAsync(
            AuditEvents.EventConfirmationProofRejected,
            AuditEntities.EventConfirmation,
            confirmationId.ToString(),
            $"Admin rejeitou comprovante da confirmacao #{confirmationId} (grupo #{groupId})",
            currentUserId, "GroupAdmin");
    }
}
