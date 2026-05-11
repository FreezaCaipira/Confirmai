using System.ComponentModel.DataAnnotations;

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

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
