using System.Text.RegularExpressions;

namespace Confirmai.Services.Notification;

/// <summary>WhatsApp group JID validation shared by UI/service and tests.</summary>
public static class WhatsAppGroupJid
{
    private static readonly Regex Pattern = new(@"^[0-9-]+@g\.us$", RegexOptions.Compiled);

    /// <summary>Null/empty means "feature off for the group" — that is valid input.</summary>
    public static bool IsValidOrEmpty(string? jid) =>
        string.IsNullOrWhiteSpace(jid) || Pattern.IsMatch(jid);

    public static bool IsValid(string? jid) =>
        !string.IsNullOrWhiteSpace(jid) && Pattern.IsMatch(jid);
}
