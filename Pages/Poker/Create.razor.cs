using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Poker;
using Microsoft.AspNetCore.Components;

namespace Confirmai.Pages.Poker;

public partial class Create
{
    private sealed class CreatePokerEventForm
    {
        public PokerEventType EventType { get; set; } = PokerEventType.Tournament;

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
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public TimeOnly Time { get; set; } = new(19, 0);
        public DateOnly? LateRegDate { get; set; }
        public TimeOnly? LateRegTime { get; set; }
        public PokerModality Modality { get; set; } = PokerModality.Vanilla;

        [Range(1000, int.MaxValue, ErrorMessage = "Minimo 1.000 fichas.")]
        public int StartingStack { get; set; } = 20000;

        [Range(1, int.MaxValue, ErrorMessage = "Informe os BBs.")]
        public int InitialBlindBB { get; set; } = 50;

        public int MaxPlayers { get; set; }

        [Range(0, 1000000, ErrorMessage = "Valor invalido.")]
        public decimal BuyInAmount { get; set; }

        public decimal? GTD { get; set; }
        public decimal? RebuyAmount { get; set; }
        public decimal? RebuyDoubleAmount { get; set; }
        public decimal? AddonAmount { get; set; }
        public decimal? AddonDoubleAmount { get; set; }

        [Range(0, 1000000, ErrorMessage = "Valor invalido.")]
        public decimal CashMinBuyIn { get; set; }

        [Range(0, 1000000, ErrorMessage = "Valor invalido.")]
        public decimal CashMaxBuyIn { get; set; }

        public string? CashIncludes { get; set; }
        public bool IsPrivate { get; set; } = true;
    }

    [Inject] private PokerCreateService CreateService { get; set; } = default!;

    private CreatePokerEventForm form = new();
    private Group? preselectedGroup;
    private bool isSaving;
    private string saveError = string.Empty;
    private string? collisionHref;

    private static readonly string[] BrazilianStates =
        ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
         "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];

    [SupplyParameterFromQuery(Name = "tipo")] public string? TipoParam { get; set; }
    [SupplyParameterFromQuery] [Parameter] public int? GroupId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        form.EventType = TipoParam switch
        {
            "cashgame" => PokerEventType.CashGame,
            "homegame" => PokerEventType.HomeGame,
            _ => PokerEventType.Tournament,
        };

        if (!GroupId.HasValue)
        {
            NavigationManager.NavigateTo("/grupos");
            return;
        }

        var data = await CreateService.InitializeAsync(GroupId, form.Name, form.City, form.StateCode);
        if (data.PreselectedGroup is null)
        {
            NavigationManager.NavigateTo("/grupos");
            return;
        }

        preselectedGroup = data.PreselectedGroup;
        form.Name = preselectedGroup.Name;
        form.City = preselectedGroup.City;
        form.StateCode = preselectedGroup.StateCode;
    }

    private async Task Save()
    {
        isSaving = true;
        saveError = string.Empty;
        collisionHref = null;

        try
        {
            var formData = new PokerCreateFormData
            {
                EventType = form.EventType,
                Name = form.Name,
                PokerHouseName = form.PokerHouseName,
                Address = form.Address,
                City = form.City,
                StateCode = form.StateCode,
                Date = form.Date,
                Time = form.Time,
                LateRegDate = form.LateRegDate,
                LateRegTime = form.LateRegTime,
                Modality = form.Modality,
                StartingStack = form.StartingStack,
                InitialBlindBB = form.InitialBlindBB,
                MaxPlayers = form.MaxPlayers,
                BuyInAmount = form.BuyInAmount,
                GTD = form.GTD,
                RebuyAmount = form.RebuyAmount,
                RebuyDoubleAmount = form.RebuyDoubleAmount,
                AddonAmount = form.AddonAmount,
                AddonDoubleAmount = form.AddonDoubleAmount,
                CashMinBuyIn = form.CashMinBuyIn,
                CashMaxBuyIn = form.CashMaxBuyIn,
                CashIncludes = form.CashIncludes,
                IsPrivate = form.IsPrivate,
            };

            var result = await CreateService.SaveAsync(formData, preselectedGroup);

            if (!result.Success)
            {
                saveError = result.Error ?? "Erro ao criar evento.";
                collisionHref = result.CollisionHref;
                return;
            }

            NavigationManager.NavigateTo($"/poker/{result.EventId}");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao criar evento: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private void OnTimeChange(ChangeEventArgs e) =>
        form.Time = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : new TimeOnly(19, 0);

    private static PokerModality[] AvailableModalities(PokerEventType type) =>
        type == PokerEventType.CashGame
            ? Enum.GetValues<PokerModality>()
            : Enum.GetValues<PokerModality>().Where(m => m != PokerModality.DealerChoice).ToArray();

    private static string ModalityLabel(PokerModality m) => m switch
    {
        PokerModality.Vanilla => "Vanilla (Texas NL)",
        PokerModality.PKO => "PKO",
        PokerModality.Freezeout => "Freezeout",
        PokerModality.PLO4 => "PLO 4 cartas",
        PokerModality.PLO5 => "PLO 5 cartas",
        PokerModality.PLO6 => "PLO 6 cartas",
        PokerModality.DealerChoice => "Dealer's Choice",
        _ => m.ToString()
    };

    private static string EventTypeLabel(PokerEventType t) => t switch
    {
        PokerEventType.Tournament => "Torneio",
        PokerEventType.CashGame => "Cash Game",
        PokerEventType.HomeGame => "Home Game",
        _ => "Evento"
    };
}
