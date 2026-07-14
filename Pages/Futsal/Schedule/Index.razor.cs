using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Futsal.Schedule;

public partial class Index : ComponentBase
{
    [SupplyParameterFromQuery]
    [Parameter]
    public int? GroupId { get; set; }

    private List<MatchSchedule> schedules = new();
    private Dictionary<int, int> scheduleFutureCounts = new();
    private Dictionary<int, (DateTime StartsAt, int Confirmed)> scheduleNextEvents = new();
    private bool isLoading = true;
    private int? togglingId = null;

    private static readonly string[] WeekdayLabels =
    [
        "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"
    ];

    protected override async Task OnInitializedAsync()
    {
        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            isLoading = false;
            return;
        }

        await using var db = await DbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        schedules = await db.RachaSchedules
            .Include(s => s.Venue)
            .Include(s => s.Group)
            .Where(s => s.CreatedByUserId == userId && (GroupId == null || s.GroupId == GroupId.Value))
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.DayOfWeek)
            .ThenBy(s => s.TimeOfDay)
            .ToListAsync();

        var scheduleIds = schedules.Select(s => s.Id).ToList();
        if (scheduleIds.Count > 0)
        {
            scheduleFutureCounts = await db.Events
                .Where(e => e.RachaScheduleId.HasValue
                         && scheduleIds.Contains(e.RachaScheduleId!.Value)
                         && e.StartsAt > now
                         && e.IsActive)
                .GroupBy(e => e.RachaScheduleId!.Value)
                .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ScheduleId, x => x.Count);

            var nextRows = await db.Events
                .Where(e => e.RachaScheduleId.HasValue
                         && scheduleIds.Contains(e.RachaScheduleId!.Value)
                         && e.StartsAt > now
                         && e.IsActive)
                .Select(e => new
                {
                    ScheduleId = e.RachaScheduleId!.Value,
                    e.StartsAt,
                    Confirmed  = e.Confirmations.Count,
                })
                .ToListAsync();

            scheduleNextEvents = nextRows
                .GroupBy(e => e.ScheduleId)
                .ToDictionary(
                    g => g.Key,
                    g => { var f = g.OrderBy(e => e.StartsAt).First(); return (f.StartsAt, f.Confirmed); });
        }

        isLoading = false;
    }

    private async Task ToggleSchedule(MatchSchedule s)
    {
        togglingId = s.Id;

        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var schedule = await db.RachaSchedules.FindAsync(s.Id);
            if (schedule is null) return;

            schedule.IsActive = !schedule.IsActive;
            await db.SaveChangesAsync();

            s.IsActive = schedule.IsActive;
        }
        finally
        {
            togglingId = null;
        }
    }
}
