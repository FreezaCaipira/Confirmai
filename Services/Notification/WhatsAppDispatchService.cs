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
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/partida/{ev.Id}";
    }

    /// <summary>Absolute URL of the group payments page, or null when PublicBaseUrl is unset.</summary>
    public string? GroupPaymentsLink(int groupId)
    {
        var baseUrl = _options.Value.PublicBaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/grupo/{groupId}/pagamentos";
    }

    public async Task DispatchAsync(int eventId, WhatsAppMessageKind kind, string text, CancellationToken ct = default)
    {
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
                .FirstOrDefaultAsync(d => d.EventId == eventId && d.MessageKind == kind.ToString(), ct);

            if (dispatch?.Status == WhatsAppDispatchStatus.Sent) return;
            if (dispatch is not null && dispatch.Attempts >= MaxAttempts) return;

            if (dispatch is null)
            {
                dispatch = new WhatsAppDispatch
                {
                    EventId = eventId,
                    MessageKind = kind.ToString(),
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
