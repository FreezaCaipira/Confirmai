using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Components.Groups;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Groups;

public partial class Ranking
{
    [Parameter] public int Id { get; set; }

    private enum RankingView { Month, Year, AllTime }

    private sealed class RankingRow
    {
        public string UserId      { get; init; } = string.Empty;
        public string UserName    { get; init; } = string.Empty;
        public int    GamesPlayed { get; init; }
        public int    GamesPaid   { get; init; }
        public int    Wins        { get; init; }
        public int    Draws       { get; init; }
        public int    Highlights  { get; init; }
    }

    private Group?          group        = null;
    private bool            isLoading    = true;
    private bool            isMember     = false;
    private string?         currentUserId = null;
    private RankingView     viewMode     = RankingView.Month;
    private int             currentYear  = DateTime.Now.Year;
    private List<RankingRow> rankingRows = new();

    private List<RankingTable.RankingRow> rankingRowsTable =>
        rankingRows.Select(r => new RankingTable.RankingRow(
            r.UserId,
            r.UserName,
            r.GamesPlayed,
            r.Highlights,
            r.Wins
        )).ToList();

    private List<(int EventId, DateTime StartsAt, string WinnerUserId)> allMvpWinners = new();

    // All-time raw data, filtered client-side per view
    private List<(string UserId, string UserName, DateTime StartsAt, bool HasPaid, int? TeamId, int? ScoreTeamA, int? ScoreTeamB)> allConfirmations = new();

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

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

        if (!isMember || !group.EnablePostMatchRanking) { isLoading = false; return; }

        // Load all past event confirmations for this group
        var now = DateTime.UtcNow;
        var raw = await db.EventConfirmations
            .Where(c => c.Event.GroupId == Id
                     && c.Event.IsActive
                     && c.Event.StartsAt < now
                     && c.User != null)
            .Select(c => new
            {
                c.UserId,
                UserName = c.User!.FullName ?? c.User.UserName ?? c.UserId,
                c.Event.StartsAt,
                c.HasPaid,
                c.TeamId,
                c.Event.ScoreTeamA,
                c.Event.ScoreTeamB,
            })
            .ToListAsync();

        allConfirmations = raw
            .Select(x => (x.UserId, x.UserName, x.StartsAt, x.HasPaid, x.TeamId, x.ScoreTeamA, x.ScoreTeamB))
            .ToList();

        // Load MVP highlights per event
        var votesRaw = await db.PostMatchVotes
            .Where(v => v.Event.GroupId == Id && v.Event.IsActive && v.Event.StartsAt < now)
            .Select(v => new { v.EventId, v.VotedForUserId, v.Event.StartsAt })
            .ToListAsync();

        allMvpWinners = votesRaw
            .GroupBy(v => v.EventId)
            .Select(g => (
                EventId: g.Key,
                StartsAt: g.First().StartsAt,
                WinnerUserId: g.GroupBy(v => v.VotedForUserId)
                               .OrderByDescending(vg => vg.Count())
                               .First().Key
            ))
            .ToList();

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
        var query = allConfirmations.AsEnumerable();

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

        rankingRows = query
            .GroupBy(x => x.UserId)
            .Select(g => new RankingRow
            {
                UserId      = g.Key,
                UserName    = g.First().UserName,
                GamesPlayed = g.Count(),
                GamesPaid   = g.Count(x => x.HasPaid),
                Wins        = g.Count(x =>
                    (x.TeamId == 0 && x.ScoreTeamA > x.ScoreTeamB) ||
                    (x.TeamId == 1 && x.ScoreTeamB > x.ScoreTeamA)),
                Draws       = g.Count(x =>
                    (x.TeamId == 0 || x.TeamId == 1) &&
                    x.ScoreTeamA.HasValue && x.ScoreTeamB.HasValue &&
                    x.ScoreTeamA == x.ScoreTeamB),
                Highlights  = highlightCounts.GetValueOrDefault(g.Key, 0),
            })
            .OrderByDescending(r => r.Wins)
            .ThenByDescending(r => r.GamesPlayed)
            .ThenByDescending(r => r.GamesPaid)
            .ToList();
    }
}
