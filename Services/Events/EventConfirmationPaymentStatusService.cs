using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Events;

public sealed record EventConfirmationPaymentTransitionResult(
    bool Found,
    bool Updated,
    int? ConfirmationId,
    EventConfirmationPaymentStatus? PreviousStatus,
    EventConfirmationPaymentStatus? CurrentStatus,
    string Message);

public sealed class EventConfirmationPaymentStatusService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LogService _logService;

    public EventConfirmationPaymentStatusService(
        IDbContextFactory<AppDbContext> dbFactory,
        LogService logService)
    {
        _dbFactory = dbFactory;
        _logService = logService;
    }

    public async Task<EventConfirmationPaymentTransitionResult> TransitionStatusAsync(
        int confirmationId,
        EventConfirmationPaymentStatus targetStatus,
        string? actorUserId = null,
        string? reason = null)
    {
        if (confirmationId <= 0)
        {
            return new EventConfirmationPaymentTransitionResult(
                Found: false,
                Updated: false,
                ConfirmationId: null,
                PreviousStatus: null,
                CurrentStatus: null,
                Message: "Informe um confirmationId válido.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var confirmation = await db.EventConfirmations.FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (confirmation is null)
        {
            return new EventConfirmationPaymentTransitionResult(
                Found: false,
                Updated: false,
                ConfirmationId: null,
                PreviousStatus: null,
                CurrentStatus: null,
                Message: "Confirmação não encontrada.");
        }

        var previousStatus = confirmation.PaymentStatus;
        if (previousStatus == targetStatus)
        {
            return new EventConfirmationPaymentTransitionResult(
                Found: true,
                Updated: false,
                ConfirmationId: confirmation.Id,
                PreviousStatus: previousStatus,
                CurrentStatus: previousStatus,
                Message: "A confirmação já está neste status.");
        }

        if (!IsAllowedTransition(previousStatus, targetStatus))
        {
            return new EventConfirmationPaymentTransitionResult(
                Found: true,
                Updated: false,
                ConfirmationId: confirmation.Id,
                PreviousStatus: previousStatus,
                CurrentStatus: previousStatus,
                Message: $"Transição inválida: {previousStatus} -> {targetStatus}.");
        }

        confirmation.PaymentStatus = targetStatus;
        confirmation.HasPaid = targetStatus == EventConfirmationPaymentStatus.Paid;
        await db.SaveChangesAsync();

        var eventType = targetStatus switch
        {
            EventConfirmationPaymentStatus.Paid => AuditEvents.PaymentConfirmed,
            EventConfirmationPaymentStatus.Failed => AuditEvents.PaymentFailed,
            EventConfirmationPaymentStatus.Refunded => AuditEvents.PaymentRefunded,
            _ => AuditEvents.PaymentStatusChanged,
        };

        await _logService.AuditAsync(
            eventType: eventType,
            entityType: AuditEntities.Payment,
            entityId: confirmation.Id.ToString(),
            message: $"Status de pagamento da confirmação {confirmation.Id} alterado de {previousStatus} para {targetStatus}.",
            actorUserId: actorUserId,
            source: AdminAuditSources.Payments,
            metadata: new
            {
                origin = "admin.payment.status.transition",
                confirmationId = confirmation.Id,
                eventId = confirmation.EventId,
                userId = confirmation.UserId,
                previousStatus = previousStatus.ToString(),
                newStatus = targetStatus.ToString(),
                pixTxId = confirmation.PixTxId,
                gateway = confirmation.PaymentGatewayName,
                reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
            });

        return new EventConfirmationPaymentTransitionResult(
            Found: true,
            Updated: true,
            ConfirmationId: confirmation.Id,
            PreviousStatus: previousStatus,
            CurrentStatus: targetStatus,
            Message: $"Status atualizado para {targetStatus}.");
    }

    private static bool IsAllowedTransition(EventConfirmationPaymentStatus current, EventConfirmationPaymentStatus target)
    {
        return current switch
        {
            EventConfirmationPaymentStatus.Pending => target is EventConfirmationPaymentStatus.Paid or EventConfirmationPaymentStatus.Failed,
            EventConfirmationPaymentStatus.Failed => target is EventConfirmationPaymentStatus.Pending or EventConfirmationPaymentStatus.Paid,
            EventConfirmationPaymentStatus.Paid => target == EventConfirmationPaymentStatus.Refunded,
            EventConfirmationPaymentStatus.Refunded => false,
            _ => false,
        };
    }
}
