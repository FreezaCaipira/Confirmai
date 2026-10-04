using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Notification;

/// <summary>
/// C31 — every WhatsApp group message goes through here. Enforces, in order:
///   1. global feature flag (Enabled) — off = complete no-op;
///   2. event exists and is allowed to trigger this kind (cancelled events
///      only get the cancellation message; past events get none);
///   3. the group has a linked JID (empty = off for that group);
///   4. the JID is in the allowlist — out of it, nothing is sent;
///   5. idempotency via the unique (EventId, MessageKind) dispatch row.
/// Never throws: a WhatsApp failure must never break the caller's flow.
/// </summary>
public sealed class WhatsAppDispatchService
{
    private const int MaxAttempts = 5;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IWhatsAppSender _sender;
    private readonly IOptions<WhatsAppOptions> _options;
    private readonly OperationalMetrics? _ops;
    private readonly ILogger<WhatsAppDispatchService> _logger;

    public WhatsAppDispatchService(
        IDbContextFactory<AppDbContext> dbFactory,
        IWhatsAppSender sender,
        IOptions<WhatsAppOptions> options,
        ILogger<WhatsAppDispatchService> logger,
        OperationalMetrics? ops = null)
    {
        _dbFactory = dbFactory;
        _sender = sender;
        _options = options;
        _logger = logger;
        _ops = ops;
    }

    /// <summary>Absolute URL of the event page, or null when PublicBaseUrl is unset.</summary>
    public string? EventLink(Event ev)
    {
        var baseUrl = _options.Value.PublicBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;
        var path = ev.Sport == Sport.Poker ? "poker" : "futsal";
        return $"{baseUrl.TrimEnd('/')}/{path}/{ev.Id}";
    }

    /// <summary>Absolute URL of the group payments page, or null when PublicBaseUrl is unset.</summary>
    public string? GroupPaymentsLink(int groupId)
    {
        var baseUrl = _options.Value.PublicBaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/grupo/{groupId}/pagamentos";
    }

    /// <summary>
    /// C31 itens 2-3 — 15-minute sweep run by WhatsAppReminderSchedulerService.
    /// Sends DayReminder (event's local date == today, local hour &gt;= 7, more
    /// than 90 min away so it never follows the hour reminder) and HourReminder (starts in (60, 90] minutes so a 15-min sweep
    /// always catches the window once). Idempotent via the dispatch rows.
    /// Returns the number of dispatch attempts made.
    /// </summary>
    public async Task<int> RunReminderSweepAsync(DateTime utcNow, CancellationToken ct = default)
    {
        if (!_options.Value.Enabled) return 0;

        var localNow = utcNow.ToLocalTime();
        var localToday = localNow.Date;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var candidates = await db.Events
            .Include(e => e.Group)
            .Include(e => e.Venue)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(e => e.IsActive
                        && e.Group!.WhatsAppGroupJid != null
                        && e.StartsAt > utcNow
                        && e.StartsAt <= utcNow.Date.AddDays(2))
            .ToListAsync(ct);

        var attempts = 0;
        foreach (var ev in candidates)
        {
            var untilStart = ev.StartsAt - utcNow;
            if (untilStart > TimeSpan.FromMinutes(60) && untilStart <= TimeSpan.FromMinutes(90))
            {
                await DispatchAsync(ev.Id, WhatsAppMessageKind.HourReminder,
                    WhatsAppTexts.HourReminder(ev, EventLink(ev)), ct, StartSlot(ev.StartsAt));
                attempts++;
            }
            else if (untilStart > TimeSpan.FromMinutes(90)
                     && localNow.Hour >= 7 && ev.StartsAt.ToLocalTime().Date == localToday)
            {
                await DispatchAsync(ev.Id, WhatsAppMessageKind.DayReminder,
                    WhatsAppTexts.DayReminder(ev, EventLink(ev)), ct, StartSlot(ev.StartsAt));
                attempts++;
            }
        }
        return attempts;
    }

    /// <summary>
    /// C31 F3b — group-scoped pending-payment notice (no names, no amounts).
    /// Group-scoped rows carry EventId = null, so the unique index does not
    /// deduplicate them; instead a Sent row for the same JID inside the last
    /// 24h suppresses repeats (an admin notifying N delinquents in one sitting
    /// produces exactly one group message).
    /// </summary>
    public async Task DispatchPaymentPendingAsync(int groupId, string groupName, CancellationToken ct = default)
    {
        var opts = _options.Value;
        if (!opts.Enabled) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var group = await db.Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);
            if (group is null) return;

            var jid = group.WhatsAppGroupJid;
            if (string.IsNullOrWhiteSpace(jid)) return;

            if (!opts.ParseAllowedGroupJids().Contains(jid))
            {
                _ops?.WhatsAppSend("blocked_allowlist");
                _logger.LogWarning(
                    "WhatsApp: JID fora da allowlist, aviso de pendencia do grupo {GroupId} nao enviado.",
                    groupId);
                return;
            }

            var kind = WhatsAppMessageKind.PaymentPending.ToString();
            var since = DateTime.UtcNow.AddHours(-24);
            var recentlySent = await db.WhatsAppDispatches.AnyAsync(d =>
                d.EventId == null
                && d.GroupJid == jid
                && d.MessageKind == kind
                && d.Status == WhatsAppDispatchStatus.Sent
                && d.SentAtUtc > since, ct);
            if (recentlySent) return;

            var dispatch = new WhatsAppDispatch
            {
                EventId = null,
                MessageKind = kind,
                GroupJid = jid,
                Status = WhatsAppDispatchStatus.Failed,
                Attempts = 1,
                FirstAttemptAtUtc = DateTime.UtcNow
            };
            db.WhatsAppDispatches.Add(dispatch);

            var result = await _sender.SendGroupTextAsync(jid,
                WhatsAppTexts.PaymentPending(groupName, GroupPaymentsLink(groupId)), ct);

            if (result.Status != WhatsAppSendStatus.Failed)
            {
                dispatch.Status = WhatsAppDispatchStatus.Sent;
                dispatch.SentAtUtc = DateTime.UtcNow;
                _ops?.WhatsAppSend(result.Status == WhatsAppSendStatus.DryRun ? "dry_run" : "sent");
            }
            else
            {
                _ops?.WhatsAppSend("failed");
                _logger.LogWarning("WhatsApp: aviso de pendencia do grupo {GroupId} falhou ({Error}).",
                    groupId, result.Error);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp: erro inesperado no aviso de pendencia do grupo {GroupId}.",
                groupId);
        }
    }

    /// <summary>
    /// Idempotency slot for messages tied to a start time: a reschedule opens a
    /// new slot, so the new time is announced and reminded again.
    /// </summary>
    public static string StartSlot(DateTime startsAtUtc) => startsAtUtc.ToString("yyyyMMddHHmm");

    /// <param name="slot">Optional discriminator appended to the kind in the
    /// unique (EventId, MessageKind) key, e.g. <see cref="StartSlot"/>.</param>
    public async Task DispatchAsync(int eventId, WhatsAppMessageKind kind, string text,
        CancellationToken ct = default, string? slot = null)
    {
        var messageKey = slot is null ? kind.ToString() : $"{kind}:{slot}";

        var opts = _options.Value;
        if (!opts.Enabled) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var ev = await db.Events
                .Include(e => e.Group)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (ev is null || ev.Group is null) return;

            // Cancelled events only get the cancellation message; past events
            // get nothing except (still worth announcing) a late cancellation.
            var isCancelled = !ev.IsActive;
            var isPast = ev.StartsAt <= DateTime.UtcNow;
            if (kind != WhatsAppMessageKind.EventCancelled && (isCancelled || isPast)) return;

            var jid = ev.Group.WhatsAppGroupJid;
            if (string.IsNullOrWhiteSpace(jid)) return;

            if (!opts.ParseAllowedGroupJids().Contains(jid))
            {
                _ops?.WhatsAppSend("blocked_allowlist");
                _logger.LogWarning(
                    "WhatsApp: JID fora da allowlist, mensagem {Kind} do evento {EventId} nao enviada.",
                    kind, eventId);
                return;
            }

            var dispatch = await db.WhatsAppDispatches
                .FirstOrDefaultAsync(d => d.EventId == eventId && d.MessageKind == messageKey, ct);

            if (dispatch?.Status == WhatsAppDispatchStatus.Sent) return;
            if (dispatch is not null && dispatch.Attempts >= MaxAttempts) return;

            if (dispatch is null)
            {
                dispatch = new WhatsAppDispatch
                {
                    EventId = eventId,
                    MessageKind = messageKey,
                    GroupJid = jid,
                    Status = WhatsAppDispatchStatus.Failed,
                    FirstAttemptAtUtc = DateTime.UtcNow
                };
                db.WhatsAppDispatches.Add(dispatch);
                try
                {
                    // Claim the (EventId, MessageKind) slot BEFORE sending — the
                    // unique index is what protects two instances/restarts.
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    return; // another instance claimed the slot
                }
            }

            dispatch.Attempts += 1;
            var result = await _sender.SendGroupTextAsync(jid, text, ct);

            if (result.Status != WhatsAppSendStatus.Failed)
            {
                dispatch.Status = WhatsAppDispatchStatus.Sent;
                dispatch.SentAtUtc = DateTime.UtcNow;
                _ops?.WhatsAppSend(result.Status == WhatsAppSendStatus.DryRun ? "dry_run" : "sent");
            }
            else
            {
                _ops?.WhatsAppSend("failed");
                _logger.LogWarning("WhatsApp: envio {Kind} do evento {EventId} falhou ({Error}).",
                    kind, eventId, result.Error);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp: erro inesperado no disparo {Kind} do evento {EventId}.",
                kind, eventId);
        }
    }
}
