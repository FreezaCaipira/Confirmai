using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Groups;

public partial class Partidas
{
    [Parameter] public int Id { get; set; }

    private const int UpcomingPreviewCount = 5;

    private Group?                    group            = null;
    private bool                      isLoading        = true;
    private bool                      isMember         = false;
    private string?                   currentUserId    = null;
    private bool                      showPastEvents   = false;
    private bool                      showAllUpcoming  = false;

    private List<Event> recentEvents   = new();
    private List<Event> upcomingEvents = new();
    private List<Event> pastEvents     = new();
    private Dictionary<int, int> eventNumbers = new();

    private List<Event> displayedEvents => showPastEvents
        ? pastEvents
        : (showAllUpcoming ? upcomingEvents : upcomingEvents.Take(UpcomingPreviewCount).ToList());

    private bool hasMoreUpcoming => !showPastEvents && !showAllUpcoming && upcomingEvents.Count > UpcomingPreviewCount;

    private void ToggleUpcomingExpansion() => showAllUpcoming = !showAllUpcoming;
    private int eventsYear => displayedEvents.FirstOrDefault() is { } eventItem
        ? eventItem.StartsAt.ToLocalTime().Year
        : DateTime.Now.Year;

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
            await GroupAccess.IsMemberAsync(db, Id, currentUserId);
        if (!isMember) { isLoading = false; return; }

        recentEvents = await db.Events
            .Where(e => e.GroupId == Id)
            .Include(e => e.Confirmations)
            .Include(e => e.Venue)
            .OrderByDescending(e => e.StartsAt)
            .Take(200)
            .ToListAsync();

        var nowUtc = DateTime.UtcNow;
        upcomingEvents = recentEvents
            .Where(e => e.StartsAt >= nowUtc)
            .OrderBy(e => e.StartsAt)
            .ToList();

        pastEvents = recentEvents
            .Where(e => e.StartsAt < nowUtc)
            .OrderByDescending(e => e.StartsAt)
            .ToList();

        eventNumbers = recentEvents
            .OrderBy(e => e.StartsAt)
            .Select((e, i) => (e.Id, Num: i + 1))
            .ToDictionary(x => x.Id, x => x.Num);

        if (!showPastEvents && upcomingEvents.Count == 0 && pastEvents.Count > 0)
        {
            showPastEvents = true;
        }
        else if (showPastEvents && pastEvents.Count == 0 && upcomingEvents.Count > 0)
        {
            showPastEvents = false;
        }

        isLoading = false;
    }
}
