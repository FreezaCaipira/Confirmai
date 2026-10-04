using System.ComponentModel.DataAnnotations;

namespace Confirmai.Models;

/// <summary>Kinds of WhatsApp group messages. Unique per (EventId, MessageKind).</summary>
public enum WhatsAppMessageKind
{
    EventCancelled,
    EventUpdated,
    LineupConfirmed,
    DayReminder,
    HourReminder,
    PaymentPending,
    RecurringCreated,
    WaitlistPromoted
}

public enum WhatsAppDispatchStatus
{
    Sent,
    Failed
}

/// <summary>
/// C31 — dispatch log enforcing idempotency: a unique index on
/// (EventId, MessageKind) means a restart or a second sweep can never
/// re-send the same message kind for the same event.
/// </summary>
public class WhatsAppDispatch
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [StringLength(40)]
    public string MessageKind { get; set; } = string.Empty;

    [StringLength(64)]
    public string GroupJid { get; set; } = string.Empty;

    public WhatsAppDispatchStatus Status { get; set; } = WhatsAppDispatchStatus.Failed;

    public int Attempts { get; set; } = 0;

    public DateTime FirstAttemptAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? SentAtUtc { get; set; }
}
