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
    private readonly FeeCalculator _feeCalculator;
    private readonly EfiBankPixPayoutService _payoutService;
    private readonly FeeOptions _feeOptions;
    private readonly LogService _log;

    public PayoutService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions,
        EfiBankPixPayoutService payoutService,
        LogService log)
    {
        _dbFactory = dbFactory;
        _feeOptions = feeOptions.Value;
        _payoutService = payoutService;
        _log = log;
        _feeCalculator = new FeeCalculator(_feeOptions.PercentBps);
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

        // Calcular taxa
        var calculation = _feeCalculator.Calculate(amount.Value);
        var baseAmount = calculation.BaseAmount;
        var feeAmount = calculation.FeeAmount;

        await _log.LogAsync(
            $"Payout: calculando taxa. base={baseAmount}, fee={feeAmount}, total={calculation.TotalAmount}, confirmationId={confirmationId}.",
            source: "Payout", level: "Info");

        // Buscar ou criar PaymentRecord
        var paymentRecord = await db.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == txId);

        if (paymentRecord is null)
        {
            paymentRecord = new PaymentRecord
            {
                PaymentId = txId,
                Amount = calculation.TotalAmount,
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

            var endToEndId = await _payoutService.SendPayoutAsync(
                baseAmount,
                payoutAccount.PixKeyValue,
                $"Repasse Confirmai - Evento {confirmation.Event.Id}");

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
}
