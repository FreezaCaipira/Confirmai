using System.Globalization;
using System.Net.Http.Json;
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
using Microsoft.JSInterop;

namespace Confirmai.Pages;

public partial class Index
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // ── Constants ─────────────────────────────────────────────────
    private const string DefaultCityKey = "Pouso Alegre|MG";
    private const string DefaultCityName = "Pouso Alegre";
    private const string DefaultStateCode = "MG";
    private const string OtherCityKey = "__other__";
    private const string TabExplore = "explorar";
    private const string TabMyGames = "meus-jogos";

    // ── Tab ────────────────────────────────────────────────────────
    [Parameter]
    [SupplyParameterFromQuery(Name = "tab")]
    public string? Tab { get; set; }

    private string activeTab = TabExplore;

    // ── Explorar state ─────────────────────────────────────────────
    private List<CityOption> Cities = new();
    private string SelectedCityKey = DefaultCityKey;
    private int? futsalMatchCount = null;
    private int? pokerMatchCount = null;
    private int? volleyballMatchCount = null;
    private int? beachTennisMatchCount = null;
    private int? footvolleyMatchCount = null;
    private int? chessMatchCount = null;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private UiTextService Ui { get; set; } = default!;

    // Callbacks for two-way binding with CitySelector
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

    // Empty state actions
    private List<EmptyStateAction> EmptyStateActions => new()
    {
        new EmptyStateAction("⚽", Ui["Index.ViewFutsal"], "/futsal"),
        new EmptyStateAction("🃏", Ui["Index.ViewPoker"], "/poker", "my-conf-browse-btn--poker")
    };

    private string FutsalHref => BuildHref("/futsal");
    private string PokerHref  => BuildHref("/poker");
    private string VolleyballHref => BuildHref("/volleyball");
    private string BeachTennisHref => BuildHref("/beachtennis");
    private string FootvolleyHref => BuildHref("/footvolley");
    private string ChessHref => BuildHref("/chess");

    // ── City Context for CascadingValue ───────────────────────────────
    private CityContext CurrentCityContext
    {
        get
        {
            var opt = Cities.FirstOrDefault(c => c.Key == SelectedCityKey);
            return opt is null ? new CityContext(null, null) : new CityContext(opt.City, opt.StateCode);
        }
    }

    private string BuildHref(string basePath)
    {
        var opt = Cities.FirstOrDefault(c => c.Key == SelectedCityKey);
        return opt is null
            ? basePath
            : $"{basePath}?cidade={Uri.EscapeDataString(opt.City)}&estado={opt.StateCode}";
    }

    // ── Meus Jogos state ───────────────────────────────────────────
    private List<EventConfirmation> allUpcoming  = new();
    private List<EventConfirmation> allPast      = new();
    private List<EventConfirmation> allCancelled = new();
    private bool confLoaded    = false;
    private bool confLoading   = false;
    private string userId      = string.Empty;

    // ── Meus Jogos filters ────────────────────────────────────────
    private Sport? filterSport = null;
    private string filterPeriod = "all"; // all, 7days, 30days

    // ── Filtered lists ───────────────────────────────────────────
    private List<EventConfirmation> upcoming => FilterConfirmations(allUpcoming);
    private List<EventConfirmation> past => FilterConfirmations(allPast);
    private List<EventConfirmation> cancelled => FilterConfirmations(allCancelled);

    private List<EventConfirmation> FilterConfirmations(List<EventConfirmation> list)
    {
        var filtered = list;

        // Filter by sport
        if (filterSport.HasValue)
        {
            filtered = filtered.Where(c => c.Event.Sport == filterSport.Value).ToList();
        }

        // Filter by period
        if (filterPeriod != "all")
        {
            var days = filterPeriod == "7days" ? 7 : 30;
            var cutoff = DateTime.UtcNow.AddDays(-days);
            filtered = filtered.Where(c => c.Event.StartsAt >= cutoff).ToList();
        }

        return filtered;
    }

    private async Task LoadMatchCountsAsync(AppDbContext db)
    {
        var (city, stateCode) = GetSelectedCityAndState();
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(stateCode))
        {
            futsalMatchCount = null;
            pokerMatchCount = null;
            volleyballMatchCount = null;
            beachTennisMatchCount = null;
            footvolleyMatchCount = null;
            chessMatchCount = null;
            return;
        }

        var now = DateTime.UtcNow;
        futsalMatchCount = await db.Events
            .Where(e => e.Sport == Sport.Futsal && e.IsActive && e.StartsAt >= now)
            .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
            .CountAsync();

        pokerMatchCount = await db.Events
            .Where(e => e.Sport == Sport.Poker && e.IsActive && e.StartsAt >= now)
            .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
            .CountAsync();

        volleyballMatchCount = await db.Events
            .Where(e => e.Sport == Sport.Volleyball && e.IsActive && e.StartsAt >= now)
            .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
            .CountAsync();

        beachTennisMatchCount = await db.Events
            .Where(e => e.Sport == Sport.BeachTennis && e.IsActive && e.StartsAt >= now)
            .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
            .CountAsync();

        footvolleyMatchCount = await db.Events
            .Where(e => e.Sport == Sport.Footvolley && e.IsActive && e.StartsAt >= now)
            .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
            .CountAsync();

        chessMatchCount = await db.Events
            .Where(e => e.Sport == Sport.Chess && e.IsActive && e.StartsAt >= now)
            .Where(e => e.Group.City == city && e.Group.StateCode == stateCode)
            .CountAsync();
    }

    private (string? City, string? StateCode) GetSelectedCityAndState()
    {
        var opt = Cities.FirstOrDefault(c => c.Key == SelectedCityKey);
        return opt is null ? (null, null) : (opt.City, opt.StateCode);
    }

    protected override Task OnParametersSetAsync()
    {
        // Sync tab with URL (handles back/forward navigation and redirects from /minhas-confirmacoes).
        // userId may be empty here on first call (before OnInitializedAsync), so data loading
        // is deferred to OnInitializedAsync in that case.
        var newTab = Tab == TabMyGames ? TabMyGames : TabExplore;
        activeTab = newTab;

        if (newTab == TabMyGames && !confLoaded && !string.IsNullOrWhiteSpace(userId))
            return LoadConfirmationsAsync();

        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        // Load city options from venues (quadras cadastradas)
        await using var db = await DbFactory.CreateDbContextAsync();
        var rows = await db.Venues
            .Select(g => new { g.City, g.StateCode })
            .Distinct()
            .OrderBy(c => c.City)
            .ToListAsync();

        Cities = rows
            .Select(r => new CityOption(
                $"{r.City}|{r.StateCode}",
                $"{r.City} - {r.StateCode}",
                r.City,
                r.StateCode))
            .ToList();

        if (!Cities.Any(c => c.Key == DefaultCityKey))
            Cities.Insert(0, new CityOption(DefaultCityKey, $"{DefaultCityName} - {DefaultStateCode}", DefaultCityName, DefaultStateCode));

        // Load match counts for selected city
        await LoadMatchCountsAsync(db);

        // Resolve auth (unavailable during OnParametersSetAsync on first load)
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // OnParametersSetAsync ran before us with empty userId; load now if still needed
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

        allUpcoming  = confs.Where(c => c.Event.StartsAt >= now && c.Event.IsActive).ToList();
        allPast      = confs.Where(c => c.Event.StartsAt <  now && c.Event.IsActive)
                         .OrderByDescending(c => c.Event.StartsAt).ToList();
        allCancelled = confs.Where(c => !c.Event.IsActive)
                         .OrderByDescending(c => c.Event.StartsAt).ToList();
        confLoading = false;
        confLoaded  = true;
    }
}
