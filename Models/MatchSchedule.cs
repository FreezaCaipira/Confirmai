using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    /// <summary>
    /// Define a periodicidade de um racha: um registro por dia da semana/horário.
    /// O BackgroundService usa esses registros para gerar Events automaticamente
    /// com janela deslizante de 8 semanas.
    /// </summary>
    public class MatchSchedule
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        /// <summary>0=Domingo … 6=Sábado</summary>
        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly TimeOfDay { get; set; }

        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }

        public int MaxPlayers { get; set; } = 20;

        /// <summary>Vagas para goleiros; null = sem reserva separada.</summary>
        public int? MaxGoalkeepers { get; set; }

        /// <summary>Como o jogo é chamado na região (racha, pelada, baba…)</summary>
        [StringLength(60)]
        public string? LocalName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public ICollection<Event> GeneratedEvents { get; set; } = new List<Event>();
    }
}
