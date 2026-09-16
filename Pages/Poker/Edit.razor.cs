using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Poker;

public partial class Edit
{
    [Parameter] public int Id { get; set; }

    private sealed class EditPokerEventForm
    {
        [Required(ErrorMessage = "Informe o nome do evento.")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o nome da casa/local.")]
        [StringLength(120)]
        public string PokerHouseName { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required(ErrorMessage = "Informe a cidade.")]
        [StringLength(120)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a UF.")]
        [StringLength(2)]
        public string StateCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a data.")]
        public DateOnly Date { get; set; }

        public TimeOnly Time { get; set; } = new TimeOnly(19, 0);

        public DateOnly? LateRegDate { get; set; }
        public TimeOnly? LateRegTime { get; set; }

        public PokerModality Modality { get; set; } = PokerModality.Vanilla;

        [Range(1000, int.MaxValue, ErrorMessage = "Mínimo 1.000 fichas.")]
        public int StartingStack { get; set; } = 20000;

        [Range(1, int.MaxValue, ErrorMessage = "Informe os BBs.")]
        public int InitialBlindBB { get; set; } = 50;

        public int MaxPlayers { get; set; } = 0;

        [Range(0, 1000000, ErrorMessage = "Valor inválido.")]
        public decimal BuyInAmount { get; set; } = 0;

        public decimal? GTD               { get; set; }
        public decimal? RebuyAmount       { get; set; }
        public decimal? RebuyDoubleAmount { get; set; }
        public decimal? AddonAmount       { get; set; }
        public decimal? AddonDoubleAmount { get; set; }

        [Range(0, 1000000, ErrorMessage = "Valor inválido.")]
        public decimal CashMinBuyIn { get; set; } = 0;

        [Range(0, 1000000, ErrorMessage = "Valor inválido.")]
        public decimal CashMaxBuyIn { get; set; } = 0;

        public string? CashIncludes { get; set; }
    }

    private EditPokerEventForm form              = new();
    private PokerEventType     eventType         = PokerEventType.Tournament;
    private string?            currentHomeGameCode;
    private bool               isLoading         = true;
    private bool               notFound          = false;
    private bool               accessDenied      = false;
    private bool               isSaving          = false;
    private string             saveError         = string.Empty;
    private string?            collisionHref;

    protected override async Task OnInitializedAsync()
    {
        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        await using var db = await DbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
            .FirstOrDefaultAsync(e => e.Id == Id && e.Sport == Sport.Poker);

        if (ev is null)
        {
            notFound  = true;
            isLoading = false;
            return;
        }

        if (ev.CreatedByUserId != userId)
        {
            accessDenied = true;
            isLoading    = false;
            return;
        }

        eventType         = ev.PokerEventType ?? PokerEventType.Tournament;
        currentHomeGameCode = ev.HomeGameCode;

        var startsLocal = ev.StartsAt.ToLocalTime();
        form = new EditPokerEventForm
        {
            Name          = ev.Group.Name,
            PokerHouseName = ev.PokerHouseName ?? string.Empty,
            Address       = ev.Location,
            City          = ev.Group.City,
            StateCode     = ev.Group.StateCode,
            Date          = DateOnly.FromDateTime(startsLocal),
            Time          = TimeOnly.FromDateTime(startsLocal),
            Modality      = ev.Modality ?? PokerModality.Vanilla,
            StartingStack = ev.StartingStack ?? 20000,
            InitialBlindBB = ev.InitialBlindBB ?? 50,
            MaxPlayers    = ev.MaxPlayers,
            BuyInAmount   = ev.BuyInAmount ?? 0,
            GTD           = ev.GTD,
            RebuyAmount   = ev.RebuyAmount,
            RebuyDoubleAmount = ev.RebuyDoubleAmount,
            AddonAmount   = ev.AddonAmount,
            AddonDoubleAmount = ev.AddonDoubleAmount,
            CashMinBuyIn  = ev.CashMinBuyIn ?? 0,
            CashMaxBuyIn  = ev.CashMaxBuyIn ?? 0,
            CashIncludes  = ev.CashIncludes,
        };

        if (ev.LateRegEndsAt.HasValue)
        {
            var lateLocal = ev.LateRegEndsAt.Value.ToLocalTime();
            form.LateRegDate = DateOnly.FromDateTime(lateLocal);
            form.LateRegTime = TimeOnly.FromDateTime(lateLocal);
        }

        isLoading = false;
    }

    private void OnTimeChange(ChangeEventArgs e)
        => form.Time = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : form.Time;

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
                .FirstOrDefaultAsync(e => e.Id == Id && e.Sport == Sport.Poker);

            if (ev is null || ev.CreatedByUserId != userId)
            {
                saveError = Ui["Poker.AccessDenied"];
                isSaving  = false;
                return;
            }

            ev.Group.Name    = form.Name;
            ev.Group.City    = form.City;
            ev.Group.StateCode = form.StateCode.ToUpperInvariant();
            ev.PokerHouseName = form.PokerHouseName;
            ev.Location      = eventType != PokerEventType.HomeGame ? (form.Address ?? string.Empty) : string.Empty;

            var oldStartsAt = ev.StartsAt;
            var newStartsAt = DateTime.SpecifyKind(form.Date.ToDateTime(form.Time), DateTimeKind.Utc);

            var collision = await EventCollisionService.FindGroupTimeCollisionAsync(ev.GroupId, newStartsAt, ev.Id);
            if (collision is not null)
            {
                saveError = Confirmai.Services.Events.EventCollisionService.BuildConflictMessage(collision.StartsAt);
                collisionHref = $"/poker/{collision.EventId}";
                isSaving  = false;
                return;
            }

            ev.StartsAt     = newStartsAt;

            switch (eventType)
            {
                case PokerEventType.Tournament:
                    ev.Modality        = form.Modality;
                    ev.StartingStack   = form.StartingStack;
                    ev.InitialBlindBB  = form.InitialBlindBB;
                    ev.MaxPlayers      = form.MaxPlayers;
                    ev.BuyInAmount     = form.BuyInAmount;
                    ev.GTD             = form.GTD;
                    ev.RebuyAmount     = form.RebuyAmount;
                    ev.RebuyDoubleAmount = form.RebuyDoubleAmount;
                    ev.AddonAmount     = form.AddonAmount;
                    ev.AddonDoubleAmount = form.AddonDoubleAmount;
                    ev.LateRegEndsAt   = form.LateRegDate.HasValue && form.LateRegTime.HasValue
                        ? DateTime.SpecifyKind(form.LateRegDate.Value.ToDateTime(form.LateRegTime.Value), DateTimeKind.Utc)
                        : null;
                    break;

                case PokerEventType.CashGame:
                    ev.Modality      = form.Modality;
                    ev.CashMinBuyIn  = form.CashMinBuyIn;
                    ev.CashMaxBuyIn  = form.CashMaxBuyIn;
                    ev.CashIncludes  = form.CashIncludes;
                    break;

                // HomeGame: apenas identidade e data/hora são editáveis; código mantido
            }

            await db.SaveChangesAsync();
            await NotificationService.NotifyEventUpdatedAsync(Id, userId!, oldStartsAt);
            NavigationManager.NavigateTo($"/poker/{Id}");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao salvar: {ex.Message}";
            isSaving  = false;
        }
    }

    private static PokerModality[] AvailableModalities(PokerEventType type) =>
        type == PokerEventType.CashGame
            ? Enum.GetValues<PokerModality>()
            : Enum.GetValues<PokerModality>().Where(m => m != PokerModality.DealerChoice).ToArray();

    private static string ModalityLabel(PokerModality m) => m switch
    {
        PokerModality.Vanilla      => "Vanilla (Texas NL)",
        PokerModality.PKO          => "PKO",
        PokerModality.Freezeout    => "Freezeout",
        PokerModality.PLO4         => "PLO 4 cartas",
        PokerModality.PLO5         => "PLO 5 cartas",
        PokerModality.PLO6         => "PLO 6 cartas",
        PokerModality.DealerChoice => "Dealer's Choice",
        _                          => m.ToString()
    };
}
