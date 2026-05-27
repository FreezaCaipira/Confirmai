using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class WaitingList
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        // Posição na fila — menor número entra primeiro
        public int Position { get; set; }

        /// <summary>Posição desejada no futsal (linha ou goleiro). Null = qualquer.</summary>
        public FutsalPosition? DesiredPosition { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
