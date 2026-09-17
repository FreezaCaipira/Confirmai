using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Confirmai.Services.Core;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;

namespace Confirmai.Services.Admin;

/// <summary>
/// Encapsulates admin confirmation management (payment toggling, removal).
/// </summary>
public class AdminConfirmationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LogService? _logService;
    private readonly PlatformFeeLedgerService? _feeLedger;
    private readonly ILogger<AdminConfirmationService>? _logger;

    public AdminConfirmationService(
        IDbContextFactory<AppDbContext> dbFactory,
        LogService? logService = null,
        PlatformFeeLedgerService? feeLedger = null,
        ILogger<AdminConfirmationService>? logger = null)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _logService = logService;
        _feeLedger = feeLedger;
        _logger = logger;
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
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null)
            return new AdminTogglePaidResult { Found = false };

        // Only a group admin of the event's group may mark a manual payment.
        // The UI also guards this, but the service cannot trust the caller.
        if (conf.Event is null ||
            !await GroupAccess.IsGroupAdminAsync(db, conf.Event.GroupId, currentUserId))
        {
            return new AdminTogglePaidResult
            {
                Found = true,
                Updated = false,
                DenyReason = AdminMutationDenyReason.Forbidden
            };
        }

        // Guard: prevent unmarking payment from gateway
        if (conf.HasPaid && conf.PaymentGatewayName != null)
            return new AdminTogglePaidResult
            {
                Found = true,
                Updated = false,
                DenyReason = AdminMutationDenyReason.GatewayPayment
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

        // Stamp platform fee on the confirmation (V1 manual flow, futsal only)
        if (conf.HasPaid && _feeLedger is not null)
        {
            try
            {
                await _feeLedger.StampFeeOnPaidAsync(confirmationId);
            }
            catch (Exception ex)
            {
                // Non-blocking for the admin, but never silent: an unstamped fee is revenue lost.
                _logger?.LogError(ex,
                    "Falha ao carimbar a taxa da plataforma na confirmacao {ConfirmationId}", confirmationId);
            }
        }

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
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null)
            return new AdminRemoveConfirmationResult { Found = false };

        if (conf.Event is null ||
            !await GroupAccess.IsGroupAdminAsync(db, conf.Event.GroupId, currentUserId))
        {
            return new AdminRemoveConfirmationResult
            {
                Found = true,
                Updated = false,
                DenyReason = AdminMutationDenyReason.Forbidden
            };
        }

        // Guard: prevent removal if payment came from gateway
        if (conf.PaymentGatewayName != null)
            return new AdminRemoveConfirmationResult
            {
                Found = true,
                Updated = false,
                DenyReason = AdminMutationDenyReason.GatewayPayment
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

/// <summary>Why a mutation was denied — pages translate this, never the service.</summary>
public enum AdminMutationDenyReason
{
    None,
    /// <summary>Caller is not a group admin of the event's group.</summary>
    Forbidden,
    /// <summary>Confirmation carries a gateway payment and cannot be reverted/removed.</summary>
    GatewayPayment
}

public class AdminTogglePaidResult
{
    public bool Found { get; set; }
    public bool Updated { get; set; }
    public AdminMutationDenyReason DenyReason { get; set; }
    public EventConfirmationPaymentStatus? NewStatus { get; set; }
}

public class AdminRemoveConfirmationResult
{
    public bool Found { get; set; }
    public bool Updated { get; set; }
    public AdminMutationDenyReason DenyReason { get; set; }
}
