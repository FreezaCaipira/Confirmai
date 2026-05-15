using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class Event
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public Sport Sport { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public DateTime StartsAt { get; set; }

        public int MaxPlayers { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        // ── Futsal ────────────────────────────────────────────────────────────

        public int? MaxGoalkeepers { get; set; }

        /// <summary>Como o jogo é chamado na região (racha, pelada, baba…)</summary>
        [StringLength(60)]
        public string? LocalName { get; set; }

        /// <summary>Quadra/Society/Campo onde a partida acontece</summary>
        public int? VenueId { get; set; }
        public Venue? Venue { get; set; }

        /// <summary>Duração em minutos</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>Valor cobrado por jogador (R$)</summary>
        public decimal? Price { get; set; }

        /// <summary>Se gerado por periodicidade, referência ao schedule de origem</summary>
        public int? RachaScheduleId { get; set; }
        public MatchSchedule? RachaSchedule { get; set; }

        // ── Poker — comum ─────────────────────────────────────────────────────

        public PokerEventType? PokerEventType { get; set; }

        public PokerModality? Modality { get; set; }

        /// <summary>Nome da casa de poker (texto livre)</summary>
        [StringLength(120)]
        public string? PokerHouseName { get; set; }

        // ── Poker — Torneio ───────────────────────────────────────────────────

        public DateTime? LateRegEndsAt { get; set; }

        /// <summary>Fichas iniciais</summary>
        public int? StartingStack { get; set; }

        /// <summary>Preço do blind inicial em BBs</summary>
        public int? InitialBlindBB { get; set; }

        /// <summary>Premiação garantida (GTD)</summary>
        public decimal? GTD { get; set; }

        public decimal? BuyInAmount { get; set; }
        public decimal? RebuyAmount { get; set; }
        public decimal? AddonAmount { get; set; }

        /// <summary>Rebuy duplo — pode ser menor que Rebuy*2 por razão mercadológica</summary>
        public decimal? RebuyDoubleAmount { get; set; }

        /// <summary>Addon duplo — opcional por torneio</summary>
        public decimal? AddonDoubleAmount { get; set; }

        // ── Poker — Cash Game ─────────────────────────────────────────────────

        public decimal? CashMinBuyIn { get; set; }
        public decimal? CashMaxBuyIn { get; set; }

        /// <summary>O que está incluso no cash game (janta, cerveja, refri…)</summary>
        [StringLength(300)]
        public string? CashIncludes { get; set; }

        // ── Poker — Home Game ─────────────────────────────────────────────────

        /// <summary>Código auto-gerado para acesso ao home game (ex: XK7-492)</summary>
        [StringLength(10)]
        public string? HomeGameCode { get; set; }

        // ── Legado (mantido para compatibilidade) ─────────────────────────────

        [StringLength(50)]
        public string? BuyIn { get; set; }

        [StringLength(50)]
        public string? Rebuy { get; set; }

        [StringLength(50)]
        public string? Addon { get; set; }

        [StringLength(300)]
        public string? BonusInfo { get; set; }

        // ── Navegação ─────────────────────────────────────────────────────────

        public ICollection<EventConfirmation> Confirmations { get; set; } = new List<EventConfirmation>();
        public ICollection<WaitingList> WaitingList { get; set; } = new List<WaitingList>();
    }
}
