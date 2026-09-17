using System.Globalization;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Admin;

public partial class AdminRevenue
{
    private DateTime? startDate;
    private DateTime? endDate;
    private bool isLoading = false;
    private RevenueSummary? summary;
    private List<GroupRevenueBreakdown> groupBreakdown = new();
    private CultureInfo PtBr { get; } = new CultureInfo("pt-BR");

    // ── Platform fee settlement review queue (Fase B do Ciclo 27) ──────────
    private PlatformFeeReviewQueue? reviewQueue;
    private bool isLoadingQueue;
    private int? reviewingSettlementId;
    private int? rejectingSettlementId;
    private string rejectReason = string.Empty;
    private string reviewMessage = string.Empty;
    private int? viewingProofSettlementId;

    // ── Per-group platform fee waiver (Ciclo 34) ─────────────────────────
    private IReadOnlyList<(int Id, string Name)> waiverGroupOptions = Array.Empty<(int, string)>();
    private IReadOnlyList<GroupFeeWaiverRow> waiverRows = Array.Empty<GroupFeeWaiverRow>();
    private GroupFeeWaivedStats? waivedStats;
    private int waiverGroupId;
    private DateTime waiverUntil = DateTime.UtcNow.Date.AddDays(31);
    private string waiverReason = string.Empty;
    private string waiverMessage = string.Empty;
    private bool waiverSaving;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AdminRevenueReportService RevenueReportService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private PlatformFeeSettlementQueryService FeeQuery { get; set; } = default!;
    [Inject] private PlatformFeeSettlementService FeeSettlement { get; set; } = default!;
    [Inject] private PlatformFeeWaiverService FeeWaiver { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Default to last 30 days
        endDate = DateTime.UtcNow;
        startDate = DateTime.UtcNow.AddDays(-30);
        await Task.WhenAll(LoadReportAsync(), LoadReviewQueueAsync(), LoadWaiverAsync());
    }

    private async Task LoadReportAsync()
    {
        isLoading = true;
        try
        {
            summary = await RevenueReportService.GetRevenueSummaryAsync(startDate, endDate);
            groupBreakdown = await RevenueReportService.GetRevenueByGroupAsync(startDate, endDate);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ClearFilters()
    {
        startDate = null;
        endDate = null;
    }

    // ── Platform fee settlement review (Fase B) ────────────────────────────

    private async Task LoadReviewQueueAsync()
    {
        isLoadingQueue = true;
        try
        {
            reviewQueue = await FeeQuery.GetReviewQueueAsync();
        }
        finally
        {
            isLoadingQueue = false;
        }
    }

    private async Task ConfirmSettlementAsync(int settlementId)
    {
        if (currentUserId is null) return;
        reviewingSettlementId = settlementId;
        reviewMessage = string.Empty;
        try
        {
            var result = await FeeSettlement.ReviewSettlementAsync(settlementId, currentUserId, approved: true);
            reviewMessage = result.Success
                ? T["AdminRevenue.SettlementSuccess"]
                : result.Message;
            if (result.Success)
                await LoadReviewQueueAsync();
        }
        finally
        {
            reviewingSettlementId = null;
        }
    }

    private async Task RejectSettlementAsync(int settlementId)
    {
        if (currentUserId is null) return;
        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            reviewMessage = T["AdminRevenue.RejectReasonRequired"];
            return;
        }
        rejectingSettlementId = settlementId;
        reviewMessage = string.Empty;
        try
        {
            var result = await FeeSettlement.ReviewSettlementAsync(
                settlementId, currentUserId, approved: false, note: rejectReason);
            reviewMessage = result.Success
                ? T["AdminRevenue.SettlementSuccess"]
                : result.Message;
            if (result.Success)
            {
                rejectReason = string.Empty;
                await LoadReviewQueueAsync();
            }
        }
        finally
        {
            rejectingSettlementId = null;
        }
    }

    private Task ViewSettlementProofAsync(int settlementId)
    {
        viewingProofSettlementId = settlementId;
        return Task.CompletedTask;
    }

    // ── Per-group platform fee waiver (Ciclo 34) ─────────────────────────

    private async Task LoadWaiverAsync()
    {
        waiverGroupOptions = await FeeWaiver.GetGroupsAsync();
        waiverRows = await FeeWaiver.GetWaiverOverviewAsync();
        waivedStats = await FeeWaiver.GetWaivedStatsAsync(startDate, endDate);
    }

    private async Task SaveWaiverAsync()
    {
        if (currentUserId is null || waiverGroupId <= 0 || waiverSaving) return;
        waiverSaving = true;
        waiverMessage = string.Empty;
        try
        {
            // The picked day is the last waived day: store the exclusive end (next day 00:00 UTC).
            var untilUtc = DateTime.SpecifyKind(waiverUntil.Date.AddDays(1), DateTimeKind.Utc);
            var result = await FeeWaiver.SetWaiverAsync(waiverGroupId, currentUserId, untilUtc, waiverReason);
            waiverMessage = result.Success
                ? T["AdminRevenue.FeeWaiverSaved"]
                : WaiverErrorText(result.Error);
            if (result.Success)
                await LoadWaiverAsync();
        }
        finally
        {
            waiverSaving = false;
        }
    }

    private async Task RevokeWaiverAsync(int groupId)
    {
        if (currentUserId is null || waiverSaving) return;
        waiverSaving = true;
        waiverMessage = string.Empty;
        try
        {
            var result = await FeeWaiver.ClearWaiverAsync(groupId, currentUserId);
            waiverMessage = result.Success
                ? T["AdminRevenue.FeeWaiverRevoked"]
                : WaiverErrorText(result.Error);
            if (result.Success)
                await LoadWaiverAsync();
        }
        finally
        {
            waiverSaving = false;
        }
    }

    private string WaiverErrorText(PlatformFeeWaiverError error) => error switch
    {
        PlatformFeeWaiverError.NotAuthorized => T["AdminRevenue.FeeWaiverErrNotAuthorized"],
        PlatformFeeWaiverError.ExpirationNotFuture => T["AdminRevenue.FeeWaiverErrExpiration"],
        PlatformFeeWaiverError.ReasonRequired => T["AdminRevenue.FeeWaiverErrReason"],
        PlatformFeeWaiverError.ReasonTooLong => T["AdminRevenue.FeeWaiverErrReasonTooLong"],
        PlatformFeeWaiverError.GroupNotFound => T["AdminRevenue.FeeWaiverErrGroupNotFound"],
        _ => T["AdminRevenue.SettlementError"],
    };

    private string? currentUserId;
}
