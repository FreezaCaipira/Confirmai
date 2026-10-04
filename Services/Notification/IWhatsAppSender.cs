namespace Confirmai.Services.Notification;

/// <summary>Outcome of a group message send attempt.</summary>
public enum WhatsAppSendStatus
{
    Sent,
    DryRun,
    Failed
}

public sealed record WhatsAppSendResult(WhatsAppSendStatus Status, string? Error = null)
{
    public static readonly WhatsAppSendResult Sent = new(WhatsAppSendStatus.Sent);
    public static readonly WhatsAppSendResult DryRun = new(WhatsAppSendStatus.DryRun);
    public static WhatsAppSendResult Failed(string? error = null) => new(WhatsAppSendStatus.Failed, error);
}

/// <summary>
/// C31 — sends a text message to a WhatsApp GROUP (JID ...@g.us).
/// There is intentionally no DM method: no per-user number, no opt-in,
/// nobody adds a DM path by accident.
/// </summary>
public interface IWhatsAppSender
{
    Task<WhatsAppSendResult> SendGroupTextAsync(string groupJid, string text, CancellationToken ct = default);
}
