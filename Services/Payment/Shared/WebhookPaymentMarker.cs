using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Payment.Shared;

/// <summary>
/// Shared logic for marking EventConfirmation records as paid from webhook handlers.
/// Consolidates the duplicated MarkPaymentPaidAsync pattern from AbacatePay and EfiBank webhooks.
/// </summary>
public sealed class WebhookPaymentMarker
{
    private readonly AppDbContext _db;
    private readonly LogService _log;
    private readonly PaymentEventBus _eventBus;

    public WebhookPaymentMarker(AppDbContext db, LogService log, PaymentEventBus eventBus)
    {
        _db = db;
        _log = log;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Marks an EventConfirmation as paid by its PixTxId.
    /// Handles idempotency (already paid), refund protection, concurrency, and event bus notification.
    /// </summary>
    /// <param name="txId">The Pix transaction ID (stored in EventConfirmation.PixTxId).</param>
    /// <param name="gatewayName">Gateway name to store (e.g. "Pix", "EfiBank").</param>
    /// <param name="logPrefix">Prefix for log messages (e.g. "AbacatePay", "EfiBank").</param>
    /// <returns>True if the confirmation was found and processed, false otherwise.</returns>
    public async Task<bool> MarkConfirmationPaidAsync(string txId, string gatewayName, string logPrefix)
    {
        var confirmation = await _db.EventConfirmations
            .FirstOrDefaultAsync(c => c.PixTxId == txId);

        if (confirmation is null)
        {
            await _log.LogAsync(
                $"{logPrefix} webhook: EventConfirmation não encontrado para txId={txId}.",
                source: "Webhook", level: "Warning");
            return false;
        }

        if (confirmation.PaymentStatus == EventConfirmationPaymentStatus.Paid)
            return true; // idempotent

        if (confirmation.PaymentStatus == EventConfirmationPaymentStatus.Refunded)
            return true; // do not resurrect refunded confirmations

        confirmation.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        confirmation.HasPaid = true;
        if (string.IsNullOrWhiteSpace(confirmation.PaymentGatewayName))
            confirmation.PaymentGatewayName = gatewayName;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return true; // another process beat us to it
        }

        _eventBus.NotifyPaymentConfirmed(confirmation.UserId, txId);

        await _log.LogAsync(
            $"{logPrefix}: pagamento txId={txId} confirmado via webhook. ConfirmationId={confirmation.Id}",
            source: "Webhook", level: "Info");

        return true;
    }

    /// <summary>
    /// Marks a marketplace Payment as paid by its PaymentId.
    /// Handles idempotency and concurrency.
    /// </summary>
    /// <param name="chargeId">The charge/payment ID.</param>
    /// <param name="logPrefix">Prefix for log messages.</param>
    public async Task MarkMarketplacePaymentPaidAsync(string chargeId, string logPrefix)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == chargeId);

        if (payment is null)
        {
            await _log.LogAsync(
                $"{logPrefix} webhook: pagamento não encontrado para chargeId={chargeId}.",
                source: "Webhook", level: "Warning");
            return;
        }

        if (payment.IsPaid)
            return; // idempotent

        payment.IsPaid = true;
        payment.PaidAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return;
        }

        _eventBus.NotifyPaymentConfirmed(payment.UserId ?? "", chargeId);

        await _log.LogAsync(
            $"{logPrefix}: pagamento chargeId={chargeId} confirmado via webhook.",
            source: "Webhook", level: "Info");
    }
}
