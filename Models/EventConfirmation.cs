using System.ComponentModel.DataAnnotations;
using Confirmai.Enums;

namespace Confirmai.Models
{
    public class EventConfirmation
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        // Futsal: define se o jogador confirmou como goleiro ou linha
        public FutsalPosition? Position { get; set; }

        /// <summary>Escalação: 0 = Time A, 1 = Time B, null = não escalado</summary>
        public int? TeamId { get; set; }

        public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Linha players only. Set to true when payment is confirmed (gateway TBD).</summary>
        public bool HasPaid { get; set; }

        /// <summary>EfiBank Pix charge txId linked to this confirmation (32-char hex). Set when charge is created.</summary>
        [StringLength(35)]
        public string? PixTxId { get; set; }
    }

    public enum FutsalPosition
    {
        Outfield = 0,
        Goalkeeper = 1
    }
}
