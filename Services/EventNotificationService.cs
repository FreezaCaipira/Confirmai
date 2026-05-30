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
        var bodyHtml  = $"<p>{bodyText.Replace("\n", "<br/>")}</p>";

        foreach (var member in members)
        {
            db.UserMailboxMessages.Add(new UserMailboxMessage
            {
                SenderUserId    = ev.CreatedByUserId ?? string.Empty,
                RecipientUserId = member.UserId,
                Subject         = subject,
                Body            = bodyText,
                CreatedAt       = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        foreach (var member in members)
        {
            if (string.IsNullOrWhiteSpace(member.User?.Email)) continue;
            try   { await _emailSender.SendEmailAsync(member.User.Email, subject, bodyHtml); }
            catch { /* ignore */ }
        }
    }

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

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId    = ev.CreatedByUserId ?? string.Empty,
            RecipientUserId = promotedUserId,
            Subject         = subject,
            Body            = bodyText,
            CreatedAt       = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var bodyHtml = $"<p>{bodyText.Replace("\n", "<br/>")}</p>";
            try { await _emailSender.SendEmailAsync(user.Email, subject, bodyHtml); }
            catch { /* ignore */ }
        }
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

        db.UserMailboxMessages.Add(new UserMailboxMessage
        {
            SenderUserId    = adminUserId,
            RecipientUserId = targetUserId,
            Subject         = $"[Confirmai] Pagamentos pendentes — {groupName}",
            Body            = bodyText,
            CreatedAt       = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var subject  = $"[Confirmai] Pagamentos pendentes — {groupName}";
            var bodyHtml = $"<p>{bodyText.Replace("\n", "<br/>")}</p>";
            try { await _emailSender.SendEmailAsync(user.Email, subject, bodyHtml); }
            catch { /* ignore */ }
        }

        return (userName, user.Email, user.PhoneNumber);
    }
}
