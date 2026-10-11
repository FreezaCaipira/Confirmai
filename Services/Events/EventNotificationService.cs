using System.Web;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Notification;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Events;

/// <summary>
/// Notifica os participantes confirmados de um evento via mailbox interno e email
/// quando o evento é cancelado ou tem sua data/hora alterada.
/// </summary>
public class EventNotificationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEmailSender                   _emailSender;
    private readonly ILogger<EventNotificationService> _logger;
    private readonly WhatsAppDispatchService?       _whatsApp;
    private readonly EmailTemplateService?          _emailTemplate;
    private readonly UiTextService?                 _uiText;
    private readonly AppLinks?                      _links;

    public EventNotificationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IEmailSender emailSender,
        ILogger<EventNotificationService> logger,
        WhatsAppDispatchService? whatsApp = null,
        EmailTemplateService? emailTemplate = null,
        UiTextService? uiText = null,
        AppLinks? links = null)
    {
        _dbFactory   = dbFactory;
        _emailSender = emailSender;
        _logger      = logger;
        _whatsApp    = whatsApp;
        _emailTemplate = emailTemplate;
        _uiText      = uiText;
        _links       = links;
    }

    // ── C40 F1 — link + CTA nos emails ──────────────────────────────────────

    private string? EventCtaUrl(Event ev) => _links?.EventUrl(ev);

    private string CtaText(string key, string fallback) => _uiText?[key] ?? fallback;

    /// <summary>Linha do link no corpo do mailbox (texto puro) quando ha base.</summary>
    private static string MailboxLinkLine(string? ctaText, string? ctaUrl)
        => string.IsNullOrWhiteSpace(ctaUrl) ? string.Empty : $"\n\n{ctaText}: {ctaUrl}";

    /// <summary>Paragrafos do corpo — codificados (o template nao encoda paragrafos).</summary>
    private static IEnumerable<string> BodyParagraphs(string bodyText)
        => bodyText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => HttpUtility.HtmlEncode(p).Replace("\n", "<br/>"));

    private (string Html, string? Text) RenderEmail(
        string subject, string bodyText, string? ctaText, string? ctaUrl)
    {
        var paragraphs = BodyParagraphs(bodyText).ToList();
        if (_emailTemplate is null)
            return ($"<p>{string.Join("</p><p>", paragraphs)}</p>", null);

        return (
            _emailTemplate.RenderHtml(subject, preheader: null, paragraphs, ctaText, ctaUrl),
            _emailTemplate.RenderText(subject, paragraphs, ctaText, ctaUrl));
    }

    private async Task SendEmailBestEffortAsync(
        string email, string subject, string bodyText, string? ctaText, string? ctaUrl, string context)
    {
        var (html, text) = RenderEmail(subject, bodyText, ctaText, ctaUrl);
        try
        {
            if (_emailSender is IdentityEmailSender sender && text is not null)
                await sender.SendEmailAsync(email, subject, html, text);
            else
                await _emailSender.SendEmailAsync(email, subject, html);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Falha ao enviar e-mail ({Context}) para {Email}", context, email); }
    }

    /// <summary>
    /// Envia notificação de cancelamento a todos os participantes confirmados,
    /// exceto o próprio organizador que cancelou.
    /// </summary>
    public async Task NotifyEventCancelledAsync(int eventId, string cancelledByUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
            .Include(e => e.Venue)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return;

        var sportLabel = ev.Sport == Sport.Futsal ? "partida" : "evento";
        var startsStr  = ev.StartsAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");

        var subject  = $"[Confirmai] Cancelamento: {ev.Group.Name}";
        var bodyText = $"Olá!\n\n" +
                       $"A {sportLabel} \"{ev.Group.Name}\", " +
                       $"que estava marcada para {startsStr}, foi cancelada pelo organizador.\n\n" +
                       $"Equipe Confirmai";
        var ctaText  = CtaText("Email.Cta.OpenGroup", "Abrir o grupo");
        var ctaUrl   = _links?.GroupUrl(ev.GroupId);

        await SendToParticipantsAsync(db, ev, cancelledByUserId, subject, bodyText, ctaText, ctaUrl);

        if (_whatsApp is not null)
            await _whatsApp.DispatchAsync(ev.Id, WhatsAppMessageKind.EventCancelled,
                WhatsAppTexts.EventCancelled(ev, _whatsApp.EventLink(ev)));
    }

    /// <summary>
    /// Envia notificação de atualização a todos os participantes confirmados
    /// quando a data/hora do evento muda (diferença ≥ 1 minuto).
    /// <paramref name="oldStartsAt"/> deve ser o valor UTC antes do save.
    /// </summary>
    public async Task NotifyEventUpdatedAsync(int eventId, string updatedByUserId, DateTime oldStartsAt)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
            .Include(e => e.Venue)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return;

        var dateChanged = Math.Abs((ev.StartsAt - oldStartsAt).TotalMinutes) >= 1;
        if (!dateChanged) return;

        var sportLabel = ev.Sport == Sport.Futsal ? "partida" : "evento";
        var oldStr     = oldStartsAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
        var newStr     = ev.StartsAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");

        var subject  = $"[Confirmai] Atualização: {ev.Group.Name}";
        var bodyText = $"Olá!\n\n" +
                       $"A {sportLabel} \"{ev.Group.Name}\" teve sua data alterada pelo organizador.\n\n" +
                       $"• Data anterior: {oldStr}\n" +
                       $"• Nova data:     {newStr}\n\n" +
                       $"Equipe Confirmai";
        var ctaText  = CtaText("Email.Cta.ViewEvent", "Ver partida e confirmar");
        var ctaUrl   = EventCtaUrl(ev);

        await SendToParticipantsAsync(db, ev, updatedByUserId, subject, bodyText, ctaText, ctaUrl);

        if (_whatsApp is not null)
            await _whatsApp.DispatchAsync(ev.Id, WhatsAppMessageKind.EventUpdated,
                WhatsAppTexts.EventRescheduled(ev, oldStartsAt, _whatsApp.EventLink(ev)),
                slot: WhatsAppDispatchService.StartSlot(ev.StartsAt));
    }

    // ── internos ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Notifica todos os membros do grupo (exceto o criador) quando o scheduler
    /// gera automaticamente uma nova ocorrência de evento recorrente.
    /// </summary>
    public async Task NotifyNewRecurringEventAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return;

        var members = await db.GroupMembers
            .Include(m => m.User)
            .Where(m => m.GroupId == ev.GroupId && m.UserId != ev.CreatedByUserId)
            .ToListAsync();

        if (members.Count == 0) return;

        var senderUser = await db.Users.FindAsync(ev.CreatedByUserId) as ApplicationUser;
        var senderDisplayName = senderUser?.UserName;
        var startsStr = ev.StartsAt.ToLocalTime().ToString("ddd, dd/MM/yyyy 'às' HH:mm",
            System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        var location  = ev.Venue?.Name ?? ev.Location;
        var subject   = $"[Confirmai] Nova partida: {ev.Group.Name}";
        var bodyText  = $"Olá!\n\n" +
                        $"Uma nova ocorrência de \"{ev.Group.Name}\" foi agendada automaticamente.\n\n" +
                        $"📅 {startsStr}\n" +
                        $"📍 {location}\n\n" +
                        $"Acesse o Confirmai para confirmar sua presença.\n\n" +
                        $"Equipe Confirmai";
        var ctaText   = CtaText("Email.Cta.ViewEvent", "Ver partida e confirmar");
        var ctaUrl    = EventCtaUrl(ev);
        var mailboxBody = bodyText + MailboxLinkLine(ctaText, ctaUrl);

        foreach (var member in members)
        {
            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId      = ev.CreatedByUserId ?? string.Empty,
                SenderDisplayName = senderDisplayName,
                RecipientUserId   = member.UserId,
                RecipientDisplayName = member.User?.UserName,
                Subject           = subject,
                Body              = mailboxBody,
                CreatedAt         = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        foreach (var member in members)
        {
            if (string.IsNullOrWhiteSpace(member.User?.Email)) continue;
            await SendEmailBestEffortAsync(member.User.Email, subject, bodyText, ctaText, ctaUrl, "nova partida");
        }

        if (_whatsApp is not null)
            await _whatsApp.DispatchAsync(ev.Id, WhatsAppMessageKind.RecurringCreated,
                WhatsAppTexts.RecurringCreated(ev, _whatsApp.EventLink(ev)));
    }

    private async Task SendToParticipantsAsync(
        AppDbContext db, Event ev, string senderId, string subject, string bodyText,
        string? ctaText = null, string? ctaUrl = null)
    {
        var recipients = ev.Confirmations
            .Where(c => c.UserId != senderId && c.User is not null)
            .Select(c => c.User!)
            .ToList();

        if (recipients.Count == 0) return;

        var senderUser = await db.Users.FindAsync(senderId) as ApplicationUser;
        var senderDisplayName = senderUser?.UserName;
        var mailboxBody = bodyText + MailboxLinkLine(ctaText, ctaUrl);

        foreach (var user in recipients)
        {
            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId         = senderId,
                SenderDisplayName    = senderDisplayName,
                RecipientUserId      = user.Id,
                RecipientDisplayName = user.UserName,
                Subject              = subject,
                Body                 = mailboxBody,
                CreatedAt            = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        // Email best-effort — falhas são silenciosas para não bloquear o fluxo principal
        foreach (var user in recipients)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;
            await SendEmailBestEffortAsync(user.Email, subject, bodyText, ctaText, ctaUrl, "notificação de evento");
        }
    }

    /// <summary>
    /// Notifica o usuário que foi promovido da lista de espera para a partida.
    /// </summary>
    public async Task NotifyWaitlistPromotedAsync(int eventId, string promotedUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return;

        var user = await db.Users.FindAsync(promotedUserId) as ApplicationUser;
        if (user is null) return;

        var startsStr = ev.StartsAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
        var subject   = $"[Confirmai] Vaga aberta: {ev.Group.Name}";
        var bodyText  = $"Olá!\n\n" +
                        $"Uma vaga abriu e você foi promovido da lista de espera para a partida " +
                        $"\"{ev.Group.Name}\" em {startsStr}.\n\n" +
                        $"Equipe Confirmai";
        var ctaText = CtaText("Email.Cta.ViewEvent", "Ver partida e confirmar");
        var ctaUrl  = EventCtaUrl(ev);

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId         = ev.CreatedByUserId ?? string.Empty,
            SenderDisplayName    = (await db.Users.FindAsync(ev.CreatedByUserId) as ApplicationUser)?.UserName,
            RecipientUserId      = promotedUserId,
            RecipientDisplayName = user.UserName,
            Subject              = subject,
            Body                 = bodyText + MailboxLinkLine(ctaText, ctaUrl),
            CreatedAt            = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(user.Email))
            await SendEmailBestEffortAsync(user.Email, subject, bodyText, ctaText, ctaUrl, "promoção de espera");

        if (_whatsApp is not null)
            await _whatsApp.DispatchAsync(ev.Id, WhatsAppMessageKind.WaitlistPromoted,
                WhatsAppTexts.WaitlistPromoted(ev, _whatsApp.EventLink(ev)));
    }

    /// <summary>
    /// Notifica um jogador sobre suas partidas não pagas em um grupo.
    /// Salva mensagem no mailbox interno e envia e-mail best-effort.
    /// Retorna o nome e e-mail do destinatário para uso no frontend (ex: link WhatsApp).
    /// </summary>
    public async Task<(string UserName, string? Email, string? Phone)> NotifyDelinquencyAsync(
        string adminUserId,
        string targetUserId,
        string groupName,
        int groupId,
        IReadOnlyList<(DateTime Date, decimal Price)> entries)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users.FindAsync(targetUserId) as ApplicationUser;
        if (user is null) return (string.Empty, null, null);

        var userName = user.FullName ?? user.UserName ?? "Jogador";
        var total    = entries.Sum(e => e.Price);

        var lines = string.Join("\n", entries.Select(e =>
            $"• {e.Date.ToLocalTime():dd/MM/yyyy 'às' HH:mm} — R$ {e.Price:F2}"));

        var bodyText =
            $"Olá, {userName}!\n\n" +
            $"Identificamos que você possui {entries.Count} partida{(entries.Count != 1 ? "s" : "")} " +
            $"sem pagamento em \"{groupName}\":\n\n" +
            $"{lines}\n\n" +
            $"Total em aberto: R$ {total:F2}\n\n" +
            $"Por favor, regularize o quanto antes.\n\n" +
            $"Equipe Confirmai";

        var subject  = $"[Confirmai] Pagamentos pendentes — {groupName}";
        var ctaText  = CtaText("Email.Cta.MyPayments", "Ver meus pagamentos");
        var ctaUrl   = _links?.GroupPaymentsUrl(groupId);

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId         = adminUserId,
            SenderDisplayName    = (await db.Users.FindAsync(adminUserId) as ApplicationUser)?.UserName,
            RecipientUserId      = targetUserId,
            RecipientDisplayName = user.UserName,
            Subject              = subject,
            Body                 = bodyText + MailboxLinkLine(ctaText, ctaUrl),
            CreatedAt            = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        // E-mail best-effort — falhas são silenciosas para não bloquear o fluxo principal
        if (!string.IsNullOrWhiteSpace(user.Email))
            await SendEmailBestEffortAsync(user.Email, subject, bodyText, ctaText, ctaUrl, "cobrança");

        return (userName, user.Email, user.PhoneNumber);
    }
}
