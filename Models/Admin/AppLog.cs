using System;
using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models
{
    public class AppLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string Level { get; set; } = "Info"; // Info, Warning, Error
        public string Source { get; set; } = "";    // Ex: "Payment", "Auth"
        public string Message { get; set; } = "";
        public string? Exception { get; set; }

        // ── Audit trail (April 2026) ──
        // Nullable so legacy LogAsync calls keep working unchanged.

        /// <summary>
        /// Stable machine-readable event name (e.g. "user.registered",
        /// "payment.confirmed"). See <see cref="Confirmai.Services.AuditEvents"/>.
        /// </summary>
        [StringLength(80)]
        public string? EventType { get; set; }

        /// <summary>
        /// Logical entity type the event refers to (e.g. "User", "Order",
        /// "Payment", "Product", "Server", "ItemOffer"). See
        /// <see cref="Confirmai.Services.AuditEntities"/>.
        /// </summary>
        [StringLength(40)]
        public string? EntityType { get; set; }

        /// <summary>Entity primary key as string (polymorphic).</summary>
        [StringLength(64)]
        public string? EntityId { get; set; }

        /// <summary>Caller IP address (truncated; supports IPv6).</summary>
        [StringLength(64)]
        public string? IpAddress { get; set; }

        /// <summary>HTTP TraceIdentifier / Activity id for cross-log correlation.</summary>
        [StringLength(80)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Free-form structured payload as compact JSON. Useful for
        /// before/after diffs, amounts, status transitions, etc.
        /// </summary>
        public string? MetadataJson { get; set; }
    }
}

