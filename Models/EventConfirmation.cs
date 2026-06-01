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

        /// <summary>
        /// Canonical payment status contract for event confirmations.
        /// </summary>
        public EventConfirmationPaymentStatus PaymentStatus { get; set; } = EventConfirmationPaymentStatus.Pending;

        /// <summary>
        /// Legacy compatibility flag. Prefer PaymentStatus for new code.
        /// </summary>
        public bool HasPaid { get; set; }

        /// <summary>EfiBank Pix charge txId linked to this confirmation (32-char hex). Set when charge is created.</summary>
        [StringLength(35)]
        public string? PixTxId { get; set; }

        /// <summary>Pix Copia e Cola (brcode) stored at charge creation so the payment page can reuse it without an extra API call.</summary>
        public string? PixBrCode { get; set; }

        /// <summary>Gateway used to create the charge (e.g. EfiBank, Pix).</summary>
        [StringLength(50)]
        public string? PaymentGatewayName { get; set; }

        /// <summary>UserId of the admin who manually marked this confirmation as paid (null if paid via gateway).</summary>
        [StringLength(450)]
        public string? MarkedPaidByUserId { get; set; }

        /// <summary>UTC timestamp when an admin manually marked this confirmation as paid.</summary>
        public DateTime? MarkedPaidAt { get; set; }

        /// <summary>Raw bytes of the Pix payment proof image uploaded by the payer.</summary>
        public byte[]? PixProofImageData { get; set; }

        /// <summary>MIME type of the proof image (e.g. "image/jpeg").</summary>
        [StringLength(100)]
        public string? PixProofContentType { get; set; }

        /// <summary>UTC timestamp when the payer last uploaded a proof image.</summary>
        public DateTime? PixProofUploadedAt { get; set; }

        public bool IsPaymentPending => PaymentStatus == EventConfirmationPaymentStatus.Pending;
        public bool IsPaymentPaid => PaymentStatus == EventConfirmationPaymentStatus.Paid;
    }

    public enum FutsalPosition
    {
        Outfield = 0,
        Goalkeeper = 1
    }
}
