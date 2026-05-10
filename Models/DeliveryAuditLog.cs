using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    /// <summary>
    /// Structured audit trail for every in-game delivery step.
    /// Each row represents one discrete event in the Model-B P2P transfer pipeline.
    /// </summary>
    public class DeliveryAuditLog
    {
        public int Id { get; set; }

        /// <summary>Server where the event occurred.</summary>
        public int ServerId { get; set; }
        public TibiaServer? Server { get; set; }

        /// <summary>Marketplace order this event belongs to (null for orphan/debug events).</summary>
        public int? OrderId { get; set; }
        public OrderModel? Order { get; set; }

        /// <summary>
        /// Discrete event type. Known values:
        /// CheckInbox, RemoveItem, DeliverItem, ConfirmTrade,
        /// DeliveryFailed, SellerOffline, TokenGenerated, TokenConsumed
        /// </summary>
        [StringLength(60)]
        public string EventType { get; set; } = "";

        /// <summary>In-game name of the player who triggered the action (usually the seller).</summary>
        [StringLength(120)]
        public string? ActorPlayerName { get; set; }

        /// <summary>In-game name of the player who receives the item (usually the buyer).</summary>
        [StringLength(120)]
        public string? TargetPlayerName { get; set; }

        [StringLength(120)]
        public string? ItemKey { get; set; }

        [StringLength(120)]
        public string? ItemName { get; set; }

        public int? Quantity { get; set; }

        public bool Success { get; set; } = true;

        /// <summary>Free-form detail text — error messages, found quantities, etc.</summary>
        [StringLength(500)]
        public string? Detail { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
