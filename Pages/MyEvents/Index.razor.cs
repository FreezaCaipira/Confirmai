using System.Globalization;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.MyEvents;

public partial class Index
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    [SupplyParameterFromQuery(Name = "esporte")] public string? EsporteQuery { get; set; }

    private Sport SelectedSport = Sport.Futsal;

    private List<Event>              events              = new();
    private List<MatchSchedule>      schedules           = new();
    private Dictionary<int, int>     scheduleFutureCounts = new();
    private Dictionary<int, (int EventId, DateTime StartsAt, int Confirmed)> scheduleNextEvents = new();
    private bool                     isLoading           = true;
    private string?                  scheduleCityFilter  = null;
    private int?                     cancellingId        = null;
    private int?                     togglingScheduleId  = null;
    private string                   view                = "events";
    private string                   currentUserId       = string.Empty;

    private List<string> ScheduleCities =>
        schedules
            .Select(s => s.Venue?.City)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList()!;

    private IEnumerable<MatchSchedule> FilteredSchedules =>
        scheduleCityFilter is null
            ? schedules
            : schedules.Where(s => string.Equals(s.Venue?.City, scheduleCityFilter, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        SelectedSport = EsporteQuery?.ToLowerInvariant() switch
        {
            "poker"    => Sport.Poker,
            "futsal"   => Sport.Futsal,
            _           => Sport.Futsal
        };

        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            isLoading = false;
            return;
        }

        currentUserId = userId;

        await using var db = await DbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var raw = await db.Events
            .Include(e => e.Group)
            .Include(e => e.Confirmations)
            .Where(e => e.CreatedByUserId == userId && e.Sport == SelectedSport)
            .ToListAsync();

        events = raw
            .OrderBy(e => e.StartsAt < now)                                             // upcoming first
            .ThenBy(e => e.StartsAt < now ? long.MaxValue - e.StartsAt.Ticks            // past: more recent first
                                          : e.StartsAt.Ticks)                           // upcoming: nearest first
            .ToList();

        if (SelectedSport == Sport.Futsal)
        {
            schedules = await db.RachaSchedules
                .Include(s => s.Group)
                .Include(s => s.Venue)
                .Where(s => s.CreatedByUserId == userId)
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
                        ScheduleId     = e.RachaScheduleId!.Value,
                        e.Id,
                        e.StartsAt,
                        Confirmed      = e.Confirmations.Count,
                    })
                    .ToListAsync();

                scheduleNextEvents = nextRows
                    .GroupBy(e => e.ScheduleId)
                    .ToDictionary(
                        g => g.Key,
                        g => { var f = g.OrderBy(e => e.StartsAt).First(); return (f.Id, f.StartsAt, f.Confirmed); });
            }
        }

        isLoading = false;
    }

    private async Task CancelEvent(Event ev)
    {
        cancellingId = ev.Id;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var entity = await db.Events.FindAsync(ev.Id);
            if (entity is not null)
            {
                entity.IsActive = false;
                await db.SaveChangesAsync();
                ev.IsActive = false;
            }

            if (!string.IsNullOrWhiteSpace(currentUserId))
                await NotificationService.NotifyEventCancelledAsync(ev.Id, currentUserId);
        }
        finally
        {
            cancellingId = null;
        }
    }

    private async Task ToggleSchedule(MatchSchedule s)
    {
        togglingScheduleId = s.Id;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var entity = await db.RachaSchedules.FindAsync(s.Id);
            if (entity is not null && entity.CreatedByUserId == currentUserId)
            {
                entity.IsActive = !entity.IsActive;
                await db.SaveChangesAsync();
                s.IsActive = entity.IsActive;
            }
        }
        finally
        {
            togglingScheduleId = null;
        }
    }

    private static string PokerTypeLabel(PokerEventType t) => t switch
    {
        PokerEventType.CashGame  => "Cash Game",
        PokerEventType.HomeGame  => "Home Game",
        _                        => "Torneio",
    };

    private static string DayName(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday    => "Segunda-feira",
        DayOfWeek.Tuesday   => "Terça-feira",
        DayOfWeek.Wednesday => "Quarta-feira",
        DayOfWeek.Thursday  => "Quinta-feira",
        DayOfWeek.Friday    => "Sexta-feira",
        DayOfWeek.Saturday  => "Sábado",
        _                   => "Domingo",
    };
}
