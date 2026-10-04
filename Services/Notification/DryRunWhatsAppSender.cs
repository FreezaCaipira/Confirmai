namespace Confirmai.Services.Notification;

/// <summary>
/// C31 — default sender: logs the would-be message instead of calling the
/// Evolution API. Lets every trigger ship and be tested before the service
/// exists in production.
/// </summary>
public sealed class DryRunWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<DryRunWhatsAppSender> _logger;

    public DryRunWhatsAppSender(ILogger<DryRunWhatsAppSender> logger)
    {
        _logger = logger;
    }

    public Task<WhatsAppSendResult> SendGroupTextAsync(string groupJid, string text, CancellationToken ct = default)
    {
        _logger.LogInformation("[WhatsApp dry-run] grupo {GroupJid}: {Text}", groupJid, text);
        return Task.FromResult(WhatsAppSendResult.DryRun);
    }
}
