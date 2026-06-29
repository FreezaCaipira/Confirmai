using System.Security.Claims;
using System.Security.Cryptography;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Poker;

public partial class Create
{
    private sealed class CreatePokerEventForm
    {
        public PokerEventType EventType { get; set; } = PokerEventType.Tournament;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe o nome do evento.")]
        [System.ComponentModel.DataAnnotations.StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe o nome da casa/local.")]
        [System.ComponentModel.DataAnnotations.StringLength(120)]
        public string PokerHouseName { get; set; } = string.Empty;

        public string? Address { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe a cidade.")]
        [System.ComponentModel.DataAnnotations.StringLength(120)]
        public string City { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe a UF.")]
        [System.ComponentModel.DataAnnotations.StringLength(2)]
        public string StateCode { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Informe a data.")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public TimeOnly Time { get; set; } = new(19, 0);

        public DateOnly? LateRegDate { get; set; }
        public TimeOnly? LateRegTime { get; set; }

        public PokerModality Modality { get; set; } = PokerModality.Vanilla;

        [System.ComponentModel.DataAnnotations.Range(1000, int.MaxValue, ErrorMessage = "Mínimo 1.000 fichas.")]
        public int StartingStack { get; set; } = 20000;

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Informe os BBs.")]
        public int InitialBlindBB { get; set; } = 50;

        public int MaxPlayers { get; set; } = 0;

        [System.ComponentModel.DataAnnotations.Range(0, 1000000, ErrorMessage = "Valor inválido.")]
        public decimal BuyInAmount { get; set; } = 0;

        public decimal? GTD { get; set; }
        public decimal? RebuyAmount { get; set; }
        public decimal? RebuyDoubleAmount { get; set; }
        public decimal? AddonAmount { get; set; }
        public decimal? AddonDoubleAmount { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 1000000, ErrorMessage = "Valor inválido.")]
        public decimal CashMinBuyIn { get; set; } = 0;

        [System.ComponentModel.DataAnnotations.Range(0, 1000000, ErrorMessage = "Valor inválido.")]
        public decimal CashMaxBuyIn { get; set; } = 0;

        public string? CashIncludes { get; set; }
        public bool IsPrivate { get; set; } = true;
    }

    private CreatePokerEventForm form = new();
    private Group? preselectedGroup;
    private bool isSaving = false;
    private string saveError = string.Empty;
    private string? collisionHref;

    private static readonly string[] BrazilianStates =
        ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
         "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];

    [SupplyParameterFromQuery(Name = "tipo")]
    public string? TipoParam { get; set; }

    [SupplyParameterFromQuery]
    [Parameter]
    public int? GroupId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        form.EventType = TipoParam switch
        {
            "cashgame" => PokerEventType.CashGame,
            "homegame" => PokerEventType.HomeGame,
            _ => PokerEventType.Tournament,
        };

        if (GroupId.HasValue)
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var db = await DbFactory.CreateDbContextAsync();
            preselectedGroup = await db.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == GroupId.Value
                    && g.Members.Any(m => m.UserId == userId && m.Role == GroupMemberRole.Admin));

            if (preselectedGroup is null)
            {
                NavigationManager.NavigateTo("/grupos");
                return;
            }

            form.Name = preselectedGroup.Name;
            form.City = preselectedGroup.City;
            form.StateCode = preselectedGroup.StateCode;
        }
        else
        {
            NavigationManager.NavigateTo("/grupos");
        }
    }

    private async Task Save()
    {
        isSaving = true;
        saveError = string.Empty;
        collisionHref = null;

        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                saveError = "Você precisa estar autenticado.";
                return;
            }

            var startsAt = DateTime.SpecifyKind(
                form.Date.ToDateTime(form.Time),
                DateTimeKind.Utc);

            await using var db = await DbFactory.CreateDbContextAsync();

            Group group;
            if (preselectedGroup is not null)
            {
                group = preselectedGroup;
            }
            else
            {
                group = new Group
                {
                    Name = form.Name,
                    Sport = Sport.Poker,
                    City = form.City,
                    StateCode = form.StateCode.ToUpperInvariant(),
                    IsActive = true,
                    IsPrivate = true,
                    CreatedByUserId = userId,
                    InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                };
                db.Groups.Add(group);
                await db.SaveChangesAsync();

                db.GroupMembers.Add(new GroupMember
                {
                    GroupId = group.Id,
                    UserId = userId,
                    Role = GroupMemberRole.Admin,
                });
            }

            var collision = await EventCollisionService.FindGroupTimeCollisionAsync(group.Id, startsAt);
            if (collision is not null)
            {
                saveError = Confirmai.Services.Events.EventCollisionService.BuildConflictMessage(collision.StartsAt);
                collisionHref = $"/poker/{collision.EventId}";
                isSaving = false;
                return;
            }

            var ev = new Event
            {
                GroupId = group.Id,
                Sport = Sport.Poker,
                Location = form.Address ?? string.Empty,
                PokerHouseName = form.PokerHouseName,
                StartsAt = startsAt,
                MaxPlayers = form.MaxPlayers,
                PokerEventType = form.EventType,
                Modality = form.EventType != PokerEventType.HomeGame ? form.Modality : null,
                IsActive = true,
                CreatedByUserId = userId,
            };

            switch (form.EventType)
            {
                case PokerEventType.Tournament:
                    ev.BuyInAmount = form.BuyInAmount;
                    ev.GTD = form.GTD;
                    ev.RebuyAmount = form.RebuyAmount;
                    ev.RebuyDoubleAmount = form.RebuyDoubleAmount;
                    ev.AddonAmount = form.AddonAmount;
                    ev.AddonDoubleAmount = form.AddonDoubleAmount;
                    ev.StartingStack = form.StartingStack;
                    ev.InitialBlindBB = form.InitialBlindBB;
                    if (form.LateRegDate.HasValue && form.LateRegTime.HasValue)
                        ev.LateRegEndsAt = DateTime.SpecifyKind(
                            form.LateRegDate.Value.ToDateTime(form.LateRegTime.Value),
                            DateTimeKind.Utc);
                    break;

                case PokerEventType.CashGame:
                    ev.CashMinBuyIn = form.CashMinBuyIn;
                    ev.CashMaxBuyIn = form.CashMaxBuyIn;
                    ev.CashIncludes = form.CashIncludes;
                    break;

                case PokerEventType.HomeGame:
                    ev.HomeGameCode = GenerateHomeGameCode();
                    break;
            }

            db.Events.Add(ev);
            await db.SaveChangesAsync();
            NavigationManager.NavigateTo($"/poker/{ev.Id}");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao criar evento: {ex.Message}";
            isSaving = false;
        }
    }

    private void OnTimeChange(ChangeEventArgs e)
        => form.Time = TimeOnly.TryParse(e.Value?.ToString(), out var t) ? t : new TimeOnly(19, 0);

    private static string GenerateHomeGameCode()
    {
        const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string Digits = "23456789";
        var bytes = RandomNumberGenerator.GetBytes(6);
        return $"{Letters[bytes[0] % Letters.Length]}{Letters[bytes[1] % Letters.Length]}{Digits[bytes[2] % Digits.Length]}-{Digits[bytes[3] % Digits.Length]}{Digits[bytes[4] % Digits.Length]}{Digits[bytes[5] % Digits.Length]}";
    }

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
