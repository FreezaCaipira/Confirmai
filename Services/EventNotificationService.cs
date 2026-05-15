using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

/// <summary>
/// Notifica os participantes confirmados de um evento via mailbox interno e email
/// quando o evento é cancelado ou tem sua data/hora alterada.
/// </summary>
public class EventNotificationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IEmailSender                   _emailSender;

    public EventNotificationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IEmailSender emailSender)
    {
        _dbFactory   = dbFactory;
        _emailSender = emailSender;
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

        await SendToParticipantsAsync(db, ev, cancelledByUserId, subject, bodyText);
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

        await SendToParticipantsAsync(db, ev, updatedByUserId, subject, bodyText);
    }

    // ── internos ─────────────────────────────────────────────────────────────

    private async Task SendToParticipantsAsync(
        AppDbContext db, Event ev, string senderId, string subject, string bodyText)
    {
        var recipients = ev.Confirmations
            .Where(c => c.UserId != senderId && c.User is not null)
            .Select(c => c.User!)
            .ToList();

        if (recipients.Count == 0) return;

        foreach (var user in recipients)
        {
            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId    = senderId,
                RecipientUserId = user.Id,
                Subject         = subject,
                Body            = bodyText,
                CreatedAt       = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        // Email best-effort — falhas são silenciosas para não bloquear o fluxo principal
        var bodyHtml = $"<p>{bodyText.Replace("\n", "<br/>")}</p>";
        foreach (var user in recipients)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;
            try   { await _emailSender.SendEmailAsync(user.Email, subject, bodyHtml); }
            catch { /* ignore */ }
        }

        // TODO (WhatsApp) ────────────────────────────────────────────────────
        // Pré-requisitos:
        //   1. Adicionar campo PhoneNumber (E.164, ex: "+5535999990000") em ApplicationUser
        //      e um bool WhatsAppOptIn (consentimento explícito do usuário).
        //   2. Escolher provider: Evolution API (self-hosted), Z-API ou Twilio.
        //      Criar IWhatsAppSender + implementação concreta; registrar em Program.cs.
        //   3. Injetar IWhatsAppSender neste serviço e enviar mensagem logo abaixo:
        //
        //   foreach (var user in recipients)
        //   {
        //       if (!user.WhatsAppOptIn || string.IsNullOrWhiteSpace(user.PhoneNumber)) continue;
        //       try { await _whatsAppSender.SendTextAsync(user.PhoneNumber, bodyText); }
        //       catch { /* ignore */ }
        //   }
        // ────────────────────────────────────────────────────────────────────
    }
}
