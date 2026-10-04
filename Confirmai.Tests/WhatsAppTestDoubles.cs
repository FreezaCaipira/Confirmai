using Confirmai.Services.Notification;

namespace Confirmai.Tests;

/// <summary>Recording IWhatsAppSender used across the C31 test files.</summary>
internal sealed class RecordingWhatsAppSender : IWhatsAppSender
{
    public List<(string Jid, string Text)> Calls { get; } = new();
    public WhatsAppSendStatus NextStatus { get; set; } = WhatsAppSendStatus.DryRun;

    public Task<WhatsAppSendResult> SendGroupTextAsync(string groupJid, string text, CancellationToken ct = default)
    {
        Calls.Add((groupJid, text));
        return Task.FromResult(new WhatsAppSendResult(NextStatus,
            NextStatus == WhatsAppSendStatus.Failed ? "boom" : null));
    }
}
