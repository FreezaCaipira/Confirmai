using System.Text;
using Confirmai.Models;
using Confirmai.Services.Futsal;

namespace Confirmai.Services.Notification;

/// <summary>
/// C31 — WhatsApp group message bodies (PT, WhatsApp markdown *bold*).
/// Links are appended only when PublicBaseUrl is configured — no tokens,
/// no player names in group messages unless the content is the lineup.
/// </summary>
public static class WhatsAppTexts
{
    private static void AppendLink(StringBuilder sb, string? link)
    {
        if (!string.IsNullOrWhiteSpace(link))
        {
            sb.AppendLine();
            sb.AppendLine(link);
        }
    }

    public static string EventCancelled(Event ev, string? link)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"❌ *Partida cancelada — {ev.Group.Name}*");
        sb.AppendLine($"📅 {ev.StartsAt.ToLocalTime():dd/MM/yyyy 'às' HH:mm}");
        var local = ev.Venue?.Name ?? ev.Location;
        if (!string.IsNullOrWhiteSpace(local)) sb.AppendLine($"📍 {local}");
        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }

    public static string EventRescheduled(Event ev, DateTime oldStartsAtUtc, string? link)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔄 *Horário alterado — {ev.Group.Name}*");
        sb.AppendLine($"Era: {oldStartsAtUtc.ToLocalTime():dd/MM/yyyy 'às' HH:mm}");
        sb.AppendLine($"Agora: {ev.StartsAt.ToLocalTime():dd/MM/yyyy 'às' HH:mm}");
        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }

    public static string LineupConfirmed(Event ev, string? link)
    {
        var sb = new StringBuilder(EscalacaoTextFormatter.BuildWhatsAppText(ev));
        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }

    public static string DayReminder(Event ev, string? link)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📅 *Hoje tem! — {ev.Group.Name}*");
        sb.AppendLine($"🕐 {ev.StartsAt.ToLocalTime():dd/MM/yyyy 'às' HH:mm}");
        var local = ev.Venue?.Name ?? ev.Location;
        if (!string.IsNullOrWhiteSpace(local)) sb.AppendLine($"📍 {local}");
        sb.AppendLine("Confirme sua presença no app.");
        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// The one-hour reminder carries the current lineup whenever one was
    /// confirmed (futsal) — the last pre-game read is where it is most useful.
    /// </summary>
    public static string HourReminder(Event ev, string? link)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"⏰ *Falta 1 hora! — {ev.Group.Name}*");
        var local = ev.Venue?.Name ?? ev.Location;
        if (!string.IsNullOrWhiteSpace(local)) sb.AppendLine($"📍 {local}");

        if (ev.LineupConfirmedAt is not null
            && ev.Sport == Enums.Sport.Futsal
            && ev.Confirmations is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine(EscalacaoTextFormatter.BuildWhatsAppText(ev));
        }

        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }

    /// <summary>No names and no amounts in the group — just a pointer.</summary>
    public static string PaymentPending(string groupName, string? paymentsLink)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"💰 *Pagamentos pendentes — {groupName}*");
        sb.AppendLine("Algumas partidas seguem sem comprovante aprovado. " +
                      "Confira em \"Meus pagamentos\":");
        AppendLink(sb, paymentsLink);
        return sb.ToString().TrimEnd();
    }

    public static string RecurringCreated(Event ev, string? link)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🗓️ *Nova partida agendada — {ev.Group.Name}*");
        sb.AppendLine($"📅 {ev.StartsAt.ToLocalTime():dd/MM/yyyy 'às' HH:mm}");
        var local = ev.Venue?.Name ?? ev.Location;
        if (!string.IsNullOrWhiteSpace(local)) sb.AppendLine($"📍 {local}");
        sb.AppendLine("Confirme sua presença no app.");
        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }

    /// <summary>No promoted name — the announcement is about the spot itself.</summary>
    public static string WaitlistPromoted(Event ev, string? link)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔄 *Vaga preenchida — {ev.Group.Name}*");
        sb.AppendLine($"📅 {ev.StartsAt.ToLocalTime():dd/MM/yyyy 'às' HH:mm}");
        sb.AppendLine("Uma vaga abriu e foi preenchida pela lista de espera.");
        AppendLink(sb, link);
        return sb.ToString().TrimEnd();
    }
}
