using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Confirmai.Services.Core;

namespace Confirmai.Services.Admin;

/// <summary>
/// Encapsulates admin confirmation management (payment toggling, removal).
/// </summary>
public class AdminConfirmationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LogService? _logService;

    public AdminConfirmationService(
        IDbContextFactory<AppDbContext> dbFactory,
        LogService? logService = null)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _logService = logService;
    }

    /// <summary>
    /// Toggles a confirmation between Paid/Pending states.
    /// Guards: cannot unmark if PaymentGatewayName is not null.
    /// </summary>
    public async Task<AdminTogglePaidResult> TogglePaidAsync(int confirmationId, string currentUserId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var conf = await db.EventConfirmations
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null)
            return new AdminTogglePaidResult { Found = false };

        // Guard: prevent unmarking payment from gateway
        if (conf.HasPaid && conf.PaymentGatewayName != null)
            return new AdminTogglePaidResult
            {
                Found = true,
                Updated = false,
                Message = "Não é possível desconfirmar um pagamento realizado através de gateway de pagamento."
            };

        var isPaid = conf.PaymentStatus == EventConfirmationPaymentStatus.Paid;
        var newStatus = isPaid
            ? EventConfirmationPaymentStatus.Pending
            : EventConfirmationPaymentStatus.Paid;

        conf.PaymentStatus = newStatus;
        conf.HasPaid = newStatus == EventConfirmationPaymentStatus.Paid;

        // Record admin action
        if (conf.HasPaid)
        {
            conf.MarkedPaidByUserId = currentUserId;
            conf.MarkedPaidAt = DateTime.UtcNow;
        }
        else
        {
            conf.MarkedPaidByUserId = null;
            conf.MarkedPaidAt = null;
        }

        await db.SaveChangesAsync();

        // Audit
        if (_logService != null)
        {
            var auditEvent = conf.HasPaid
                ? AuditEvents.EventConfirmationPaidManual
                : AuditEvents.EventConfirmationUnpaidManual;
            var message = conf.HasPaid
                ? $"Admin marcou confirmação #{confirmationId} como paga"
                : $"Admin desconfirmou pagamento da confirmação #{confirmationId}";

            await _logService.AuditAsync(
                auditEvent,
                AuditEntities.EventConfirmation,
                confirmationId.ToString(),
                message,
                currentUserId,
                source: "AdminConfirmation",
                metadata: new
                {
                    ConfirmationId = confirmationId,
                    NewStatus = newStatus.ToString(),
                    MarkedBy = currentUserId,
                    MarkedAt = conf.MarkedPaidAt
                });
        }

        return new AdminTogglePaidResult
        {
            Found = true,
            Updated = true,
            NewStatus = newStatus
        };
    }

    /// <summary>
    /// Removes a confirmation from an event.
    /// Guards: cannot remove if PaymentGatewayName is not null.
    /// </summary>
    public async Task<AdminRemoveConfirmationResult> RemoveConfirmationAsync(
        int confirmationId,
        string currentUserId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var conf = await db.EventConfirmations
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null)
            return new AdminRemoveConfirmationResult { Found = false };

        // Guard: prevent removal if payment came from gateway
        if (conf.PaymentGatewayName != null)
            return new AdminRemoveConfirmationResult
            {
                Found = true,
                Updated = false,
                Message = "Não é possível remover uma confirmação com pagamento de gateway."
            };

        db.EventConfirmations.Remove(conf);
        await db.SaveChangesAsync();

        // Audit
        if (_logService != null)
        {
            await _logService.AuditAsync(
                AuditEvents.EventConfirmationRemoved,
                AuditEntities.EventConfirmation,
                confirmationId.ToString(),
                $"Admin removeu confirmação #{confirmationId}",
                currentUserId,
                source: "AdminConfirmation",
                metadata: new
                {
                    ConfirmationId = confirmationId,
                    RemovedBy = currentUserId,
                    RemovedAt = DateTime.UtcNow
                });
        }

        return new AdminRemoveConfirmationResult
        {
            Found = true,
            Updated = true
        };
    }
}

public class AdminTogglePaidResult
{
    public bool Found { get; set; }
    public bool Updated { get; set; }
    public string? Message { get; set; }
    public EventConfirmationPaymentStatus? NewStatus { get; set; }
}

public class AdminRemoveConfirmationResult
{
    public bool Found { get; set; }
    public bool Updated { get; set; }
    public string? Message { get; set; }
}
