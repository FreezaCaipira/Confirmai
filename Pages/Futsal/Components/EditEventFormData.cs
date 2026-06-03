using System.ComponentModel.DataAnnotations;

namespace Confirmai.Pages.Futsal.Components
{
    /// <summary>
    /// Form model for editing a Futsal event.
    /// Shared between Edit.razor and EditEventForm component.
    /// </summary>
    public sealed class EditEventFormData
    {
        [Required(ErrorMessage = "Informe o nome da partida.")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string GroupName { get; set; } = string.Empty;

        public string? LocalName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione a quadra.")]
        public int VenueId { get; set; } = 0;

        [Required(ErrorMessage = "Informe a data.")]
        public DateOnly Date { get; set; }

        public TimeOnly Time { get; set; } = new TimeOnly(20, 0);

        public int DurationMinutes { get; set; } = 90;

        [Range(1, 100, ErrorMessage = "Mínimo 1 jogador.")]
        public int MaxPlayers { get; set; } = 10;

        public int MaxGoalkeepers { get; set; } = 0;

        public bool RotateInGoal { get; set; } = false;

        public int PlayersPerSide { get; set; } = 5;

        [Range(0, 10000, ErrorMessage = "Valor inválido.")]
        public decimal Price { get; set; } = 0;
    }
}
