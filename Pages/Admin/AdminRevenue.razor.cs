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

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AdminRevenueReportService RevenueReportService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private PlatformFeeSettlementQueryService FeeQuery { get; set; } = default!;
    [Inject] private PlatformFeeSettlementService FeeSettlement { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Default to last 30 days
        endDate = DateTime.UtcNow;
        startDate = DateTime.UtcNow.AddDays(-30);
        await Task.WhenAll(LoadReportAsync(), LoadReviewQueueAsync());
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

    private string? currentUserId;
}
