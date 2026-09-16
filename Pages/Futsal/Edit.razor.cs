using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Pages.Futsal.Components;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Futsal;

public partial class Edit
{
    [Parameter]
    public int Id { get; set; }

    private List<Venue> venues = new();
    private Venue? selectedVenue = null;
    private Event? ev = null;
    private string currentUserId = string.Empty;
    private bool isSystemAdmin = false;
    private bool isLoading = true;
    private bool isSaving = false;
    private bool showCancelConfirm = false;
    private bool notFound = false;
    private bool accessDenied = false;
    private string saveError = string.Empty;
    private string? collisionHref = null;
    private string cancelError = string.Empty;
    private EditEventFormData form = new();

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private EventNotificationService NotificationService { get; set; } = default!;
    [Inject] private EventCollisionService EventCollisionService { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        currentUserId = userId ?? string.Empty;
        isSystemAdmin = auth.User.IsInRole("admin");

        await using var db = await DbFactory.CreateDbContextAsync();

        ev = await db.Events
            .Include(e => e.Group)
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == Id && e.Sport == Sport.Futsal);

        if (ev is null)
        {
            notFound  = true;
            isLoading = false;
            return;
        }

        if (ev.CreatedByUserId != userId && !isSystemAdmin)
        {
            accessDenied = true;
            isLoading    = false;
            return;
        }

        venues = await db.Venues
            .Where(v => v.IsActive)
            .OrderBy(v => v.City)
            .ThenBy(v => v.Name)
            .ToListAsync();

        var startsLocal = ev.StartsAt.ToLocalTime();
        form = new EditEventFormData
        {
            GroupName       = ev.Group.Name,
            LocalName       = ev.LocalName,
            VenueId         = ev.VenueId ?? 0,
            Date            = DateOnly.FromDateTime(startsLocal),
            Time            = TimeOnly.FromDateTime(startsLocal),
            DurationMinutes = ev.DurationMinutes ?? 90,
            MaxPlayers      = ev.MaxPlayers,
            MaxGoalkeepers  = ev.MaxGoalkeepers ?? 0,
            RotateInGoal    = (ev.MaxGoalkeepers ?? 0) == 0,
            PlayersPerSide  = ev.PlayersPerSide ?? 5,
            Price           = ev.Price ?? 0,
        };

        selectedVenue = venues.FirstOrDefault(v => v.Id == form.VenueId);
        isLoading = false;
    }

    private async Task SaveCallback() => await Save();

    private async Task VenueSelectedCallback()
        => await Task.Run(() => selectedVenue = venues.FirstOrDefault(v => v.Id == form.VenueId));

    private async Task RotateToggledCallback()
        => await Task.Run(() =>
        {
            if (form.RotateInGoal) form.MaxGoalkeepers = 0;
            else if (form.MaxGoalkeepers == 0) form.MaxGoalkeepers = 2;
        });

    private async Task TimeChangedCallback(ChangeEventArgs e)
        => await Task.Run(() => form.Time = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : form.Time);

    private async Task CancelConfirmShowChangedCallback(bool show)
        => await Task.Run(() => showCancelConfirm = show);

    private async Task CancelConfirmedCallback() => await CancelEvent();

    private async Task Save()
    {
        isSaving  = true;
        saveError = string.Empty;
        collisionHref = null;

        try
        {
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var db = await DbFactory.CreateDbContextAsync();

            var ev = await db.Events
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.Id == Id && e.Sport == Sport.Futsal);

            if (ev is null || (ev.CreatedByUserId != userId && !auth.User.IsInRole("admin")))
            {
                saveError = Ui["Futsal.AccessDenied"];
                isSaving  = false;
                return;
            }

            var venue = venues.FirstOrDefault(v => v.Id == form.VenueId);
            if (venue is null) { saveError = Ui["Futsal.InvalidVenue"]; isSaving = false; return; }

            var oldStartsAt = ev.StartsAt;
            var newStartsAt = DateTime.SpecifyKind(form.Date.ToDateTime(form.Time), DateTimeKind.Utc);

            var collision = await EventCollisionService.FindGroupTimeCollisionAsync(ev.GroupId, newStartsAt, ev.Id);
            if (collision is not null)
            {
                saveError = EventCollisionService.BuildConflictMessage(collision.StartsAt);
                collisionHref = $"/futsal/{collision.EventId}";
                isSaving  = false;
                return;
            }

            ev.Group.Name     = form.GroupName;
            ev.LocalName      = form.LocalName;
            ev.VenueId        = form.VenueId;
            ev.Location       = venue.Address;
            ev.StartsAt       = newStartsAt;
            ev.DurationMinutes = form.DurationMinutes;
            ev.MaxPlayers     = form.MaxPlayers;
            ev.MaxGoalkeepers = form.MaxGoalkeepers > 0 ? form.MaxGoalkeepers : null;
            ev.PlayersPerSide = form.PlayersPerSide;
            ev.Price          = form.Price;

            // Propagate MaxGoalkeepers back to the parent recurring schedule
            if (ev.RachaScheduleId.HasValue)
            {
                var schedule = await db.RachaSchedules.FindAsync(ev.RachaScheduleId.Value);
                if (schedule is not null)
                    schedule.MaxGoalkeepers = ev.MaxGoalkeepers;
            }

            await db.SaveChangesAsync();
            await NotificationService.NotifyEventUpdatedAsync(Id, userId!, oldStartsAt);
            await LogService.AuditAsync(
                AuditEvents.EventUpdated,
                AuditEntities.Event,
                Id.ToString(),
                $"Partida #{Id} editada (novo horário: {newStartsAt:dd/MM/yyyy HH:mm})",
                userId, "EventEdit");
            NavigationManager.NavigateTo($"/futsal/{Id}");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao salvar: {ex.Message}";
            isSaving  = false;
        }
    }

    private async Task CancelEvent()
    {
        cancelError = string.Empty;
        try
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var dbEv = await db.Events.FirstOrDefaultAsync(e => e.Id == Id);
            if (dbEv is null) return;
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (dbEv.CreatedByUserId != userId && !auth.User.IsInRole("admin")) return;
            dbEv.IsActive = false;
            await db.SaveChangesAsync();
            await NotificationService.NotifyEventCancelledAsync(Id, userId!);
            await LogService.AuditAsync(
                AuditEvents.EventCancelled,
                AuditEntities.Event,
                Id.ToString(),
                $"Partida #{Id} cancelada pelo admin/criador",
                userId, "EventEdit");
            NavigationManager.NavigateTo($"/futsal/{Id}");
        }
        catch (Exception ex)
        {
            cancelError = $"Erro ao cancelar: {ex.Message}";
        }
    }
}
