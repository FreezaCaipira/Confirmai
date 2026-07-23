using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Futsal.Schedule;

public partial class Edit
{
    [Parameter] public int Id { get; set; }

    // ── Form model ────────────────────────────────────────────────────────

    private sealed class EditScheduleForm
    {
        public string? LocalName { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly TimeOfDay { get; set; }

        public int DurationMinutes { get; set; } = 90;

        [Range(1, int.MaxValue, ErrorMessage = "Selecione a quadra.")]
        public int VenueId { get; set; }

        [Range(1, 100, ErrorMessage = "Mínimo 1 jogador.")]
        public int MaxPlayers { get; set; } = 10;

        public int MaxGoalkeepers { get; set; } = 0;

        [Range(0, 10000, ErrorMessage = "Valor inválido.")]
        public decimal Price { get; set; }
    }

    // ── State ─────────────────────────────────────────────────────────────

    private EditScheduleForm form        = new();
    private List<Venue>      venues      = new();
    private bool             isLoading   = true;
    private bool             isSaving    = false;
    private bool             notFound    = false;
    private bool             accessDenied = false;
    private string           saveError   = string.Empty;
    private int              scheduleGroupId;

    // ── Options ───────────────────────────────────────────────────────────

    private static readonly (int Min, string Label)[] DurationOptions =
    [
        (60,  "1h"),
        (75,  "1h15min"),
        (90,  "1h30min"),
        (105, "1h45min"),
        (120, "2h"),
        (150, "2h30min"),
        (180, "3h"),
    ];

    private static readonly (DayOfWeek Dow, string Label)[] WeekdayOptions =
    [
        (DayOfWeek.Monday,    "Segunda-feira"),
        (DayOfWeek.Tuesday,   "Terça-feira"),
        (DayOfWeek.Wednesday, "Quarta-feira"),
        (DayOfWeek.Thursday,  "Quinta-feira"),
        (DayOfWeek.Friday,    "Sexta-feira"),
        (DayOfWeek.Saturday,  "Sábado"),
        (DayOfWeek.Sunday,    "Domingo"),
    ];

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        await using var db = await DbFactory.CreateDbContextAsync();

        var schedule = await db.RachaSchedules.FindAsync(Id);

        if (schedule is null)
        {
            notFound  = true;
            isLoading = false;
            return;
        }

        if (schedule.CreatedByUserId != userId)
        {
            accessDenied = true;
            isLoading    = false;
            return;
        }

        form = new EditScheduleForm
        {
            LocalName       = schedule.LocalName,
            DayOfWeek       = schedule.DayOfWeek,
            TimeOfDay       = schedule.TimeOfDay,
            DurationMinutes = schedule.DurationMinutes,
            VenueId         = schedule.VenueId,
            MaxPlayers      = schedule.MaxPlayers,
            MaxGoalkeepers  = schedule.MaxGoalkeepers ?? 0,
            Price           = schedule.Price,
        };

        scheduleGroupId = schedule.GroupId;

        venues = await db.Venues
            .Where(v => v.IsActive)
            .OrderBy(v => v.City)
            .ThenBy(v => v.Name)
            .ToListAsync();

        isLoading = false;
    }

    private void OnTimeChange(ChangeEventArgs e)
        => form.TimeOfDay = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : new TimeOnly(20, 0);

    private async Task Save()
    {
        isSaving  = true;
        saveError = string.Empty;

        try
        {
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var db = await DbFactory.CreateDbContextAsync();
            var schedule = await db.RachaSchedules.FindAsync(Id);

            if (schedule is null)      { notFound = true; return; }
            if (schedule.CreatedByUserId != userId) { accessDenied = true; return; }

            schedule.LocalName       = form.LocalName;
            schedule.DayOfWeek       = form.DayOfWeek;
            schedule.TimeOfDay       = form.TimeOfDay;
            schedule.DurationMinutes = form.DurationMinutes;
            schedule.VenueId         = form.VenueId;
            schedule.MaxPlayers      = form.MaxPlayers;
            schedule.MaxGoalkeepers  = form.MaxGoalkeepers > 0 ? form.MaxGoalkeepers : null;
            schedule.Price           = form.Price;

            await db.SaveChangesAsync();
            NavigationManager.NavigateTo($"/grupo/{schedule.GroupId}/partidas");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao salvar: {ex.Message}";
            isSaving  = false;
        }
    }
}
