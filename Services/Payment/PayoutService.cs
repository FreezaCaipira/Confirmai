using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Service that handles automatic Pix payouts to organizers after payment confirmation.
/// Calculates fee, sends Pix via EfiBank, and updates PaymentRecord with payout status.
/// </summary>
public sealed class PayoutService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPixPayoutService _payoutService;
    private readonly FeeOptions _feeOptions;
    private readonly LogService _log;

    public PayoutService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions,
        IPixPayoutService payoutService,
        LogService log)
    {
        _dbFactory = dbFactory;
        _feeOptions = feeOptions.Value;
        _payoutService = payoutService;
        _log = log;
    }

    /// <summary>
    /// Processes payout for a confirmed EventConfirmation.
    /// Calculates fee, sends Pix to organizer, and updates PaymentRecord.
    /// </summary>
    public async Task ProcessPayoutAsync(int confirmationId, string txId)
    {
        if (!_feeOptions.IsConfigured)
        {
            await _log.LogAsync(
                $"Payout: taxa não configurada. Skipping payout for confirmationId={confirmationId}.",
                source: "Payout", level: "Debug");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var confirmation = await db.EventConfirmations
            .Include(c => c.Event).ThenInclude(e => e.Group)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (confirmation is null)
        {
            await _log.LogAsync(
                $"Payout: EventConfirmation não encontrado. confirmationId={confirmationId}.",
                source: "Payout", level: "Warning");
            return;
        }

        var gatewayName = confirmation.PaymentGatewayName;
        if (string.IsNullOrWhiteSpace(gatewayName) || !_feeOptions.SupportedGateways.Contains(gatewayName))
        {
            await _log.LogAsync(
                $"Payout: gateway não suportado para taxa. gateway={gatewayName}, confirmationId={confirmationId}.",
                source: "Payout", level: "Debug");
            return;
        }

        var payoutAccount = await db.GroupPayoutAccounts
            .FirstOrDefaultAsync(gpa => gpa.GroupId == confirmation.Event.GroupId && gpa.IsActive);

        if (payoutAccount is null)
        {
            await _log.LogAsync(
                $"Payout: GroupPayoutAccount não encontrado para groupId={confirmation.Event.GroupId}. Skipping payout.",
                source: "Payout", level: "Warning");
            return;
        }

        var amount = confirmation.Event.Price;
        if (amount is null || amount <= 0)
        {
            await _log.LogAsync(
                $"Payout: valor inválido. amount={amount}, confirmationId={confirmationId}.",
                source: "Payout", level: "Warning");
            return;
        }

        // Calcular taxa usando valores fixos
        var totalFee = _feeOptions.AppFeeFixed + _feeOptions.GatewayFeeFixed;
        var baseAmount = amount.Value;
        var feeAmount = totalFee;
        var totalAmount = baseAmount + totalFee;

        await _log.LogAsync(
            $"Payout: calculando taxa. base={baseAmount}, appFee={_feeOptions.AppFeeFixed}, gatewayFee={_feeOptions.GatewayFeeFixed}, totalFee={totalFee}, total={totalAmount}, confirmationId={confirmationId}.",
            source: "Payout", level: "Info");

        // Buscar ou criar PaymentRecord
        var paymentRecord = await db.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == txId);

        // Idempotência: se o repasse já foi enviado ou confirmado, não reprocessar
        if (paymentRecord is not null &&
            (paymentRecord.PayoutStatus == PayoutStatus.Sent ||
             paymentRecord.PayoutStatus == PayoutStatus.Confirmed))
        {
            await _log.LogAsync(
                $"Payout: repasse já enviado (status={paymentRecord.PayoutStatus}). Skipping confirmationId={confirmationId}.",
                source: "Payout", level: "Debug");
            return;
        }

        if (paymentRecord is null)
        {
            paymentRecord = new PaymentRecord
            {
                PaymentId = txId,
                Amount = totalAmount,
                IsPaid = true,
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                BaseAmount = baseAmount,
                FeeAmount = feeAmount,
                PayoutStatus = PayoutStatus.Pending,
                PayoutPixKey = payoutAccount.PixKeyValue
            };
            db.Payments.Add(paymentRecord);
        }
        else
        {
            paymentRecord.BaseAmount = baseAmount;
            paymentRecord.FeeAmount = feeAmount;
            paymentRecord.PayoutStatus = PayoutStatus.Pending;
            paymentRecord.PayoutPixKey = payoutAccount.PixKeyValue;
        }

        await db.SaveChangesAsync();

        // Enviar Pix de repasse
        try
        {
            await _log.LogAsync(
                $"Payout: enviando Pix. amount={baseAmount}, pixKey={payoutAccount.PixKeyValue}, confirmationId={confirmationId}.",
                source: "Payout", level: "Info");

            var idempotencyKey = $"payout-{confirmation.Id}-{txId}";
            var endToEndId = await _payoutService.SendPayoutAsync(
                baseAmount,
                payoutAccount.PixKeyValue,
                $"Repasse Confirmai - Evento {confirmation.Event.Id}",
                idempotencyKey);

            // Atualizar PaymentRecord com sucesso
            paymentRecord.PayoutStatus = PayoutStatus.Sent;
            paymentRecord.PayoutEndToEndId = endToEndId;
            paymentRecord.PayoutSentAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await _log.LogAsync(
                $"Payout: Pix enviado com sucesso. endToEndId={endToEndId}, confirmationId={confirmationId}.",
                source: "Payout", level: "Info");
        }
        catch (Exception ex)
        {
            paymentRecord.PayoutStatus = PayoutStatus.Failed;
            paymentRecord.PayoutErrorMessage = ex.Message;
            paymentRecord.PayoutRetryCount++;
            await db.SaveChangesAsync();

            await _log.LogAsync(
                $"Payout: falha ao enviar Pix. error={ex.Message}, confirmationId={confirmationId}.",
                source: "Payout", level: "Error");
        }
    }

    /// <summary>
    /// Retries failed payouts with exponential backoff.
    /// Called by PayoutRetryService (IHostedService) on a timer.
    /// </summary>
    public async Task RetryFailedPayoutsAsync()
    {
        if (!_feeOptions.IsConfigured)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();

        const int maxRetries = 5;
        var now = DateTime.UtcNow;

        var failedPayouts = await db.Payments
            .Where(p => p.PayoutStatus == PayoutStatus.Failed
                     || p.PayoutStatus == PayoutStatus.Retrying)
            .ToListAsync();

        foreach (var record in failedPayouts)
        {
            if (record.PayoutRetryCount >= maxRetries)
            {
                record.PayoutStatus = PayoutStatus.Failed;
                await _log.LogAsync(
                    $"Payout: MÁXIMO de retentativas atingido ({maxRetries}). " +
                    $"PaymentId={record.PaymentId}, retryCount={record.PayoutRetryCount}. " +
                    $"REPASSE MANUAL NECESSÁRIO — valor base={record.BaseAmount}, pixKey={record.PayoutPixKey}.",
                    source: "Payout", level: "Error");
                await db.SaveChangesAsync();
                continue;
            }

            var backoffMinutes = 5 * Math.Pow(2, record.PayoutRetryCount);
            var lastAttempt = record.PayoutSentAt ?? record.CreatedAt;
            var nextRetryAt = lastAttempt.AddMinutes(backoffMinutes);

            if (now < nextRetryAt)
                continue;

            var confirmation = await db.EventConfirmations
                .Include(c => c.Event).ThenInclude(e => e.Group)
                .FirstOrDefaultAsync(c => c.PixTxId == record.PaymentId);

            if (confirmation is null)
            {
                await _log.LogAsync(
                    $"Payout: EventConfirmation não encontrado para retry. PaymentId={record.PaymentId}.",
                    source: "Payout", level: "Warning");
                continue;
            }

            record.PayoutStatus = PayoutStatus.Retrying;
            await db.SaveChangesAsync();

            await _log.LogAsync(
                $"Payout: retentativa {record.PayoutRetryCount + 1}/{maxRetries}. " +
                $"confirmationId={confirmation.Id}, backoff={backoffMinutes:F0}min.",
                source: "Payout", level: "Info");

            try
            {
                var idempotencyKey = $"payout-{confirmation.Id}-{record.PaymentId}";
                var endToEndId = await _payoutService.SendPayoutAsync(
                    record.BaseAmount ?? 0,
                    record.PayoutPixKey ?? string.Empty,
                    $"Repasse Confirmai - Evento {confirmation.Event.Id}",
                    idempotencyKey);

                record.PayoutStatus = PayoutStatus.Sent;
                record.PayoutEndToEndId = endToEndId;
                record.PayoutSentAt = DateTime.UtcNow;
                record.PayoutErrorMessage = null;
                await db.SaveChangesAsync();

                await _log.LogAsync(
                    $"Payout: retry bem-sucedido. endToEndId={endToEndId}, confirmationId={confirmation.Id}.",
                    source: "Payout", level: "Info");
            }
            catch (Exception ex)
            {
                record.PayoutStatus = PayoutStatus.Failed;
                record.PayoutErrorMessage = ex.Message;
                record.PayoutRetryCount++;
                await db.SaveChangesAsync();

                await _log.LogAsync(
                    $"Payout: retry falhou. error={ex.Message}, retryCount={record.PayoutRetryCount}/{maxRetries}.",
                    source: "Payout", level: "Error");
            }
        }
    }
}
