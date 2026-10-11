using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Confirmai.Shared.Components.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Groups;

public partial class Ranking
{
    [Parameter] public int Id { get; set; }

    private enum RankingView { Month, Year, AllTime }

    private Group?          group        = null;
    private bool            isLoading    = true;
    private bool            isMember     = false;
    private bool            isAdmin      = false;
    private bool            copied       = false;
    private string?         currentUserId = null;
    private RankingView     viewMode     = RankingView.Month;
    private int             currentYear  = DateTime.Now.Year;
    private List<GroupRanking.Row> rankingRows = new();

    private List<RankingTable.RankingRow> rankingRowsTable =>
        rankingRows.Select(r => new RankingTable.RankingRow(
            r.UserId, r.UserName, r.Position, r.GamesPlayed,
            r.Wins, r.Draws, r.Losses, r.Points, r.Highlights
        )).ToList();

    private List<GroupRanking.Entry> allEntries = new();
    private List<(int EventId, DateTime StartsAt, string WinnerUserId)> allMvpWinners = new();

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private GroupRankingService RankingService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        await using var db = await DbFactory.CreateDbContextAsync();

        group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == Id);

        if (group is null) { isLoading = false; return; }

        isMember = currentUserId is not null &&
                   group.Members.Any(m => m.UserId == currentUserId);
        isAdmin = currentUserId is not null &&
                  group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);

        if (!isMember || !group.EnablePostMatchRanking) { isLoading = false; return; }

        var data = await RankingService.LoadAsync(Id, DateTime.UtcNow);
        allEntries    = data.Entries;
        allMvpWinners = data.MvpWinners;

        BuildRanking();
        isLoading = false;
    }

    private void SetView(RankingView mode)
    {
        viewMode = mode;
        BuildRanking();
    }

    private void HandleViewModeChanged(string modeString)
    {
        if (Enum.TryParse<RankingView>(modeString, out var mode))
        {
            SetView(mode);
        }
    }

    private void BuildRanking()
    {
        var now   = DateTime.UtcNow;
        var query = allEntries.AsEnumerable();

        if (viewMode == RankingView.Month)
            query = query.Where(x => x.StartsAt.Year == now.Year && x.StartsAt.Month == now.Month);
        else if (viewMode == RankingView.Year)
            query = query.Where(x => x.StartsAt.Year == currentYear);

        var mvpQuery = allMvpWinners.AsEnumerable();
        if (viewMode == RankingView.Month)
            mvpQuery = mvpQuery.Where(x => x.StartsAt.Year == now.Year && x.StartsAt.Month == now.Month);
        else if (viewMode == RankingView.Year)
            mvpQuery = mvpQuery.Where(x => x.StartsAt.Year == currentYear);

        var highlightCounts = mvpQuery
            .GroupBy(x => x.WinnerUserId)
            .ToDictionary(g => g.Key, g => g.Count());

        rankingRows = GroupRanking.Build(query, highlightCounts);
    }

    private async Task CopyRankingAsync()
    {
        if (group is null || rankingRows.Count == 0) return;
        await JS.InvokeVoidAsync("navigator.clipboard.writeText",
            GroupRanking.BuildWhatsAppText(rankingRows, group.Name));
        copied = true;
    }
}
