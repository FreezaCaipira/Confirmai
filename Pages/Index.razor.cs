using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages;

public partial class Index
{
    private const string ViewPlaying = "jogo";
    private const string ViewOrganizing = "organizo";
    private const int HistoryLimit = 20;

    [Parameter] [SupplyParameterFromQuery(Name = "view")] public string? ViewQuery { get; set; }

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private UiTextService Ui { get; set; } = default!;

    private sealed record GroupSummary(int Id, string Name, Sport Sport, bool IsAdmin);

    private bool isLoading = true;
    private string view = ViewPlaying;
    private bool showCancelled;
    private string userId = string.Empty;

    private List<GroupSummary> myGroups = new();
    private List<EventConfirmation> upcoming = new();
    private List<EventConfirmation> past = new();
    private List<EventConfirmation> cancelled = new();
    private List<Event> organizing = new();

    private bool organizesAny => myGroups.Any(g => g.IsAdmin) || organizing.Count > 0;

    protected override void OnParametersSet()
    {
        view = ViewQuery == ViewOrganizing ? ViewOrganizing : ViewPlaying;
    }

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthStateProvider.GetAuthenticationStateAsync();
        userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
        {
            isLoading = false;
            return;
        }

        await using var db = await DbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        myGroups = await db.GroupMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Group.Name)
            .Select(m => new GroupSummary(m.GroupId, m.Group.Name, m.Group.Sport, m.Role == GroupMemberRole.Admin))
            .ToListAsync();

        if (myGroups.Count == 0)
        {
            isLoading = false;
            return;
        }

        // Tracking query: Confirmation -> Event -> Confirmations is a cycle EF rejects with AsNoTracking.
        var confs = await db.EventConfirmations
            .Include(c => c.Event).ThenInclude(e => e.Group)
            .Include(c => c.Event).ThenInclude(e => e.Confirmations)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Event.StartsAt)
            .ToListAsync();

        upcoming  = confs.Where(c => c.Event.IsActive && c.Event.StartsAt >= now).ToList();
        past      = confs.Where(c => c.Event.IsActive && c.Event.StartsAt < now).OrderByDescending(c => c.Event.StartsAt).ToList();
        cancelled = confs.Where(c => !c.Event.IsActive).OrderByDescending(c => c.Event.StartsAt).ToList();

        var adminGroupIds = myGroups.Where(g => g.IsAdmin).Select(g => g.Id).ToList();
        organizing = await db.Events
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Confirmations)
            .Where(e => e.IsActive && e.StartsAt >= now
                        && (e.CreatedByUserId == userId || adminGroupIds.Contains(e.GroupId)))
            .OrderBy(e => e.StartsAt)
            .ToListAsync();

        isLoading = false;
    }

    private void SetView(string next)
    {
        view = next;
        NavigationManager.NavigateTo(next == ViewOrganizing ? "/?view=organizo" : "/", replace: true);
    }

    private static string DetailUrl(Event ev) =>
        ev.Sport == Sport.Poker ? $"/poker/{ev.Id}" : $"/futsal/{ev.Id}";
}
