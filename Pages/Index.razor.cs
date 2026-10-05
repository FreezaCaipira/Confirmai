using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Groups;
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

    private sealed record GroupSummary(int Id, string Name, Sport Sport, bool IsAdmin)
    {
        public GroupScheduleLine? Schedule { get; set; }
    }

    private bool isLoading = true;
    private string view = ViewPlaying;
    private bool showCancelled;
    private string userId = string.Empty;

    private List<GroupSummary> myGroups = new();
    private List<EventConfirmation> upcoming = new();
    private List<EventConfirmation> past = new();
    private List<EventConfirmation> cancelled = new();
    private List<Event> organizing = new();
    private List<PendingPaymentGroup> pendingPaymentGroups = new();
    private DateTime localNow = DateTime.Now;
    private HomeTodaySummary today = new(HomeGreeting.Morning, [], null, 0, 0);

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

        // C37 F8: agenda fixa por grupo nos cards da home (dias + local).
        var myGroupIds = myGroups.Select(g => g.Id).ToList();
        var schedules = await db.RachaSchedules
            .AsNoTracking()
            .Include(s => s.Venue)
            .Where(s => s.IsActive && myGroupIds.Contains(s.GroupId))
            .ToListAsync();
        var schedulesByGroup = schedules.GroupBy(s => s.GroupId)
            .ToDictionary(g => g.Key, g => GroupScheduleSummary.Build(g));
        foreach (var g in myGroups)
            g.Schedule = schedulesByGroup.GetValueOrDefault(g.Id);

        var withoutSchedule = myGroups.Where(g => g.Schedule is null).Select(g => g.Id).ToList();
        if (withoutSchedule.Count > 0)
        {
            var horizon = now.AddDays(14);
            var upcoming = await db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Where(e => e.IsActive && e.StartsAt > now && e.StartsAt <= horizon
                            && withoutSchedule.Contains(e.GroupId))
                .ToListAsync();
            var upcomingByGroup = upcoming.GroupBy(e => e.GroupId)
                .ToDictionary(g => g.Key, g => GroupScheduleSummary.BuildFromUpcoming(g));
            foreach (var g in myGroups.Where(g => g.Schedule is null))
                g.Schedule = upcomingByGroup.GetValueOrDefault(g.Id);
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

        // C36-C Fase 4: the player's own unpaid priced confirmations, grouped per
        // group for the home banner. Proof already sent is not actionable here.
        pendingPaymentGroups = GroupPaymentsService.PendingByGroup(confs);

        var adminGroupIds = myGroups.Where(g => g.IsAdmin).Select(g => g.Id).ToList();
        organizing = await db.Events
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Confirmations)
            .Where(e => e.IsActive && e.StartsAt >= now
                        && (e.CreatedByUserId == userId || adminGroupIds.Contains(e.GroupId)))
            .OrderBy(e => e.StartsAt)
            .ToListAsync();

        var unread = await db.UserMailboxMessages
            .AsNoTracking()
            .CountAsync(m => m.RecipientUserId == userId && !m.IsRead && !m.IsArchivedByRecipient);

        localNow = DateTime.Now;
        today = HomeToday.Build(upcoming, organizing, pendingPaymentGroups.Sum(p => p.Count), unread, localNow);

        isLoading = false;
    }

    private void SetView(string next)
    {
        view = next;
        NavigationManager.NavigateTo(next == ViewOrganizing ? "/?view=organizo" : "/", replace: true);
    }

    private static string DetailUrl(Event ev) =>
        ev.Sport == Sport.Poker ? $"/poker/{ev.Id}" : $"/futsal/{ev.Id}";

    private string DayShort(DayOfWeek d) => d switch
    {
        DayOfWeek.Sunday    => Ui["Index.DaySun"],
        DayOfWeek.Monday    => Ui["Index.DayMon"],
        DayOfWeek.Tuesday   => Ui["Index.DayTue"],
        DayOfWeek.Wednesday => Ui["Index.DayWed"],
        DayOfWeek.Thursday  => Ui["Index.DayThu"],
        DayOfWeek.Friday    => Ui["Index.DayFri"],
        _                   => Ui["Index.DaySat"],
    };
}
