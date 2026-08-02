using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages;

public partial class Index
{
    private const string DefaultCityKey = "Pouso Alegre|MG";
    private const string DefaultCityName = "Pouso Alegre";
    private const string DefaultStateCode = "MG";
    private const string TabExplore = "explorar";
    private const string TabMyGames = "meus-jogos";

    [Parameter] [SupplyParameterFromQuery(Name = "tab")] public string? Tab { get; set; }

    private string activeTab = TabExplore;
    private List<CityOption> Cities = new();
    private string SelectedCityKey = DefaultCityKey;
    private int? futsalMatchCount, pokerMatchCount, volleyballMatchCount, beachTennisMatchCount, footvolleyMatchCount, chessMatchCount;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private UiTextService Ui { get; set; } = default!;

    private List<EmptyStateAction> EmptyStateActions => new()
    {
        new("⚽", Ui["Index.ViewFutsal"], "/futsal"),
        new("🃏", Ui["Index.ViewPoker"], "/poker", "my-conf-browse-btn--poker")
    };

    private string FutsalHref => BuildHref("/futsal");
    private string PokerHref => BuildHref("/poker");
    private string VolleyballHref => BuildHref("/volleyball");
    private string BeachTennisHref => BuildHref("/beachtennis");
    private string FootvolleyHref => BuildHref("/footvolley");
    private string ChessHref => BuildHref("/chess");

    private CityContext CurrentCityContext
    {
        get
        {
            var opt = Cities.FirstOrDefault(c => c.Key == SelectedCityKey);
            return opt is null ? new(null, null) : new(opt.City, opt.StateCode);
        }
    }

    private string BuildHref(string basePath)
    {
        var opt = Cities.FirstOrDefault(c => c.Key == SelectedCityKey);
        return opt is null ? basePath : $"{basePath}?cidade={Uri.EscapeDataString(opt.City)}&estado={opt.StateCode}";
    }

    private async Task SelectedCityKeyChanged(string value)
    {
        SelectedCityKey = value;
        await UpdateMatchCountsAsync();
    }

    private async Task UpdateMatchCountsAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        await LoadMatchCountsAsync(db);
    }

    private async Task LoadMatchCountsAsync(AppDbContext db)
    {
        var (city, stateCode) = GetSelectedCityAndState();
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(stateCode))
        {
            futsalMatchCount = pokerMatchCount = volleyballMatchCount = beachTennisMatchCount = footvolleyMatchCount = chessMatchCount = null;
            return;
        }

        var now = DateTime.UtcNow;
        var sports = new (Sport Sport, Action<int?> Set)[]
        {
            (Sport.Futsal, v => futsalMatchCount = v),
            (Sport.Poker, v => pokerMatchCount = v),
            (Sport.Volleyball, v => volleyballMatchCount = v),
            (Sport.BeachTennis, v => beachTennisMatchCount = v),
            (Sport.Footvolley, v => footvolleyMatchCount = v),
            (Sport.Chess, v => chessMatchCount = v),
        };

        foreach (var (sport, set) in sports)
        {
            set(await db.Events
                .Where(e => e.Sport == sport && e.IsActive && e.StartsAt >= now)
                .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
                .CountAsync());
        }
    }

    private (string? City, string? StateCode) GetSelectedCityAndState()
    {
        var opt = Cities.FirstOrDefault(c => c.Key == SelectedCityKey);
        return opt is null ? (null, null) : (opt.City, opt.StateCode);
    }

    // ── Meus Jogos state ───────────────────────────────────────────
    private List<EventConfirmation> allUpcoming = new();
    private List<EventConfirmation> allPast = new();
    private List<EventConfirmation> allCancelled = new();
    private bool confLoaded, confLoading;
    private string userId = string.Empty;
    private int userGroupCount;
    private Sport? filterSport;
    private string filterPeriod = "all";

    private List<EventConfirmation> upcoming => FilterConfirmations(allUpcoming);
    private List<EventConfirmation> past => FilterConfirmations(allPast);
    private List<EventConfirmation> cancelled => FilterConfirmations(allCancelled);

    private List<EventConfirmation> FilterConfirmations(List<EventConfirmation> list)
    {
        var filtered = list;
        if (filterSport.HasValue)
            filtered = filtered.Where(c => c.Event.Sport == filterSport.Value).ToList();

        if (filterPeriod != "all")
        {
            var cutoff = DateTime.UtcNow.AddDays(-(filterPeriod == "7days" ? 7 : 30));
            filtered = filtered.Where(c => c.Event.StartsAt >= cutoff).ToList();
        }

        return filtered;
    }

    protected override Task OnParametersSetAsync()
    {
        activeTab = Tab == TabMyGames ? TabMyGames : TabExplore;
        if (activeTab == TabMyGames && !confLoaded && !string.IsNullOrWhiteSpace(userId))
            return LoadConfirmationsAsync();
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var rows = await db.Venues
            .Select(g => new { g.City, g.StateCode })
            .Distinct()
            .OrderBy(c => c.City)
            .ToListAsync();

        Cities = rows.Select(r => new CityOption($"{r.City}|{r.StateCode}", $"{r.City} - {r.StateCode}", r.City, r.StateCode)).ToList();

        if (!Cities.Any(c => c.Key == DefaultCityKey))
            Cities.Insert(0, new(DefaultCityKey, $"{DefaultCityName} - {DefaultStateCode}", DefaultCityName, DefaultStateCode));

        await LoadMatchCountsAsync(db);

        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            userGroupCount = await db.GroupMembers
                .CountAsync(m => m.UserId == userId);
        }

        if (activeTab == TabMyGames && !confLoaded)
            await LoadConfirmationsAsync();
    }

    private async Task SetTabAsync(string tab)
    {
        activeTab = tab;
        NavigationManager.NavigateTo(tab == TabMyGames ? "/eventos?tab=meus-jogos" : "/eventos", replace: true);
        if (tab == TabMyGames && !confLoaded)
            await LoadConfirmationsAsync();
    }

    private async Task LoadConfirmationsAsync()
    {
        if (string.IsNullOrWhiteSpace(userId)) { confLoading = false; return; }

        confLoading = true;
        var now = DateTime.UtcNow;

        await using var db = await DbFactory.CreateDbContextAsync();
        var confs = await db.EventConfirmations
            .Include(c => c.Event).ThenInclude(e => e.Group)
            .Include(c => c.Event).ThenInclude(e => e.Confirmations)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Event.StartsAt)
            .ToListAsync();

        allUpcoming = confs.Where(c => c.Event.StartsAt >= now && c.Event.IsActive).ToList();
        allPast = confs.Where(c => c.Event.StartsAt < now && c.Event.IsActive).OrderByDescending(c => c.Event.StartsAt).ToList();
        allCancelled = confs.Where(c => !c.Event.IsActive).OrderByDescending(c => c.Event.StartsAt).ToList();
        confLoading = false;
        confLoaded = true;
    }
}
