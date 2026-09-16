using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Futsal;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Futsal;

public partial class Create
{
    [SupplyParameterFromQuery]
    [Parameter]
    public int? GroupId { get; set; }

    [Inject] private FutsalCreateService CreateService { get; set; } = default!;

    public sealed class CreateMatchForm
    {
        [StringLength(120, ErrorMessage = "Maximo 120 caracteres.")]
        public string GroupName { get; set; } = string.Empty;

        public string? LocalName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione a quadra.")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Informe a data.")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public TimeOnly Time { get; set; } = new(20, 0);
        public int DurationMinutes { get; set; } = 90;
        public int MaxPlayers { get; set; } = 10;
        public int MaxGoalkeepers { get; set; } = 2;
        public bool RotateInGoal { get; set; }
        public decimal Price { get; set; }
        public bool RecurrenceEnabled { get; set; }
        public HashSet<DayOfWeek> SelectedDays { get; set; } = new();
        public bool IsPrivate { get; set; } = true;
    }

    private CreateMatchForm form = new();
    private List<Venue> venues = new();
    private Venue? selectedVenue;
    private Group? preselectedGroup;
    private bool isLoading = true;
    private bool isSaving;
    private string saveError = string.Empty;
    private string? collisionHref;
    private string subFormat = "futsal";

    private static readonly (int Min, string Label)[] DurationOptions =
    [
        (60,  "1h"), (75, "1h15min"), (90, "1h30min"),
        (105, "1h45min"), (120, "2h"), (150, "2h30min"), (180, "3h"),
    ];

    private static readonly (DayOfWeek Dow, string Label)[] WeekdayOptions =
    [
        (DayOfWeek.Monday, "Seg"), (DayOfWeek.Tuesday, "Ter"),
        (DayOfWeek.Wednesday, "Qua"), (DayOfWeek.Thursday, "Qui"),
        (DayOfWeek.Friday, "Sex"), (DayOfWeek.Saturday, "Sab"),
        (DayOfWeek.Sunday, "Dom"),
    ];

    protected override async Task OnInitializedAsync()
    {
        if (!GroupId.HasValue)
        {
            NavigationManager.NavigateTo("/grupos");
            return;
        }

        var data = await CreateService.InitializeAsync(GroupId);
        if (data.PreselectedGroup is null)
        {
            NavigationManager.NavigateTo("/grupos");
            return;
        }

        preselectedGroup = data.PreselectedGroup;
        venues = data.Venues;
        isLoading = false;
    }

    private void OnVenueChanged() =>
        selectedVenue = venues.FirstOrDefault(v => v.Id == form.VenueId);

    private void OnRotateInGoalChanged()
    {
        if (form.RotateInGoal) form.MaxGoalkeepers = 0;
        else if (form.MaxGoalkeepers == 0) form.MaxGoalkeepers = 2;
    }

    private void OnSubFormatChanged(ChangeEventArgs e)
    {
        subFormat = e.Value?.ToString() ?? "futsal";
        ApplySubFormatDefaults();
    }

    private void ApplySubFormatDefaults()
    {
        (form.MaxPlayers, form.MaxGoalkeepers) = subFormat switch
        {
            "society" => (14, 2),
            "campo"   => (24, 2),
            _         => (10, 2),
        };
    }

    private void ToggleDay(DayOfWeek dow, bool on)
    {
        if (on) form.SelectedDays.Add(dow);
        else    form.SelectedDays.Remove(dow);
    }

    private void OnTimeChange(ChangeEventArgs e) =>
        form.Time = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : new TimeOnly(19, 0);

    private Task SubFormatChangedCallback(string newFormat)
    {
        subFormat = newFormat;
        ApplySubFormatDefaults();
        return Task.CompletedTask;
    }

    private Task VenueChangedCallback(int venueId)
    {
        form.VenueId = venueId;
        OnVenueChanged();
        return Task.CompletedTask;
    }

    private Task TimeChangeCallback(TimeOnly newTime)
    { form.Time = newTime; return Task.CompletedTask; }

    private Task RotateInGoalChangedCallback()
    { OnRotateInGoalChanged(); return Task.CompletedTask; }

    private Task ToggleDayCallback((DayOfWeek dow, bool on) args)
    { ToggleDay(args.dow, args.on); return Task.CompletedTask; }

    private async Task Save()
    {
        isSaving = true;
        saveError = string.Empty;
        collisionHref = null;

        try
        {
            var formData = new CreateMatchFormData
            {
                GroupName = form.GroupName,
                LocalName = form.LocalName,
                VenueId = form.VenueId,
                Date = form.Date,
                Time = form.Time,
                DurationMinutes = form.DurationMinutes,
                MaxPlayers = form.MaxPlayers,
                MaxGoalkeepers = form.MaxGoalkeepers,
                RotateInGoal = form.RotateInGoal,
                Price = form.Price,
                RecurrenceEnabled = form.RecurrenceEnabled,
                SelectedDays = form.SelectedDays,
                IsPrivate = form.IsPrivate,
            };

            var result = await CreateService.SaveAsync(formData, venues, preselectedGroup, subFormat);

            if (!result.Success)
            {
                saveError = result.Error ?? Ui["Futsal.CreateMatchError"];
                collisionHref = result.CollisionHref;
                return;
            }

            NavigationManager.NavigateTo($"/futsal/{result.EventId}");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao criar partida: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private static string VenueTypeLabel(VenueType t) => t switch
    {
        VenueType.Quadra  => "Quadra",
        VenueType.Society => "Society",
        VenueType.Campo   => "Campo",
        _                 => t.ToString()
    };
}
