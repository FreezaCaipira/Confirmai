using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.Core;
using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Payment;

public sealed record EventPaymentReconcileResult(
    bool Found,
    bool Updated,
    bool IsPaid,
    string Message,
    int? ConfirmationId,
    string? GatewayName);

public sealed record EventPaymentReconciliationSweepResult(
    int Considered,
    int Updated,
    int StillPending,
    int NotFound)
{
    public bool HadAnyWork => Considered > 0;
}

public sealed class EventPaymentReconciliationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EventPaymentGatewayFactory _gatewayFactory;
    private readonly LogService _logService;

    public EventPaymentReconciliationService(
        IDbContextFactory<AppDbContext> dbFactory,
        EventPaymentGatewayFactory gatewayFactory,
        LogService logService)
    {
        _dbFactory = dbFactory;
        _gatewayFactory = gatewayFactory;
        _logService = logService;
    }

    public async Task<EventPaymentReconcileResult> ReconcileByChargeIdAsync(string chargeId, string? actorUserId = null)
    {
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            return new EventPaymentReconcileResult(
                Found: false,
                Updated: false,
                IsPaid: false,
                Message: "Informe um chargeId/txId válido.",
                ConfirmationId: null,
                GatewayName: null);
        }

        chargeId = chargeId.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var confirmation = await db.EventConfirmations
            .FirstOrDefaultAsync(c => c.PixTxId == chargeId);

        if (confirmation is null)
        {
            return new EventPaymentReconcileResult(
                Found: false,
                Updated: false,
                IsPaid: false,
                Message: "Nenhuma confirmação encontrada para o chargeId informado.",
                ConfirmationId: null,
                GatewayName: null);
        }

        if (confirmation.PaymentStatus == EventConfirmationPaymentStatus.Paid)
        {
            return new EventPaymentReconcileResult(
                Found: true,
                Updated: false,
                IsPaid: true,
                Message: "A confirmação já está marcada como paga.",
                ConfirmationId: confirmation.Id,
                GatewayName: confirmation.PaymentGatewayName);
        }

            if (confirmation.PaymentStatus != EventConfirmationPaymentStatus.Pending)
            {
                return new EventPaymentReconcileResult(
                Found: true,
                Updated: false,
                IsPaid: false,
                Message: $"A confirmação está em estado {confirmation.PaymentStatus} e não é elegível para reconciliação automática.",
                ConfirmationId: confirmation.Id,
                GatewayName: confirmation.PaymentGatewayName);
            }

        var resolved = await ResolveGatewayAsync(confirmation.PaymentGatewayName, chargeId);
        if (resolved is null)
        {
            return new EventPaymentReconcileResult(
                Found: true,
                Updated: false,
                IsPaid: false,
                Message: "Não foi possível determinar o gateway para reconciliação.",
                ConfirmationId: confirmation.Id,
                GatewayName: confirmation.PaymentGatewayName);
        }

        var (gateway, paid) = resolved.Value;
        if (!paid)
        {
            return new EventPaymentReconcileResult(
                Found: true,
                Updated: false,
                IsPaid: false,
                Message: "Cobrança ainda pendente no gateway.",
                ConfirmationId: confirmation.Id,
                GatewayName: gateway.Name);
        }

        confirmation.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        confirmation.HasPaid = true;
        if (string.IsNullOrWhiteSpace(confirmation.PaymentGatewayName))
            confirmation.PaymentGatewayName = gateway.Name;

        await db.SaveChangesAsync();

        var origin = string.IsNullOrWhiteSpace(actorUserId)
            ? "worker.reconciliation"
            : "admin.reconciliation";

        await _logService.AuditAsync(
            eventType: AuditEvents.PaymentConfirmed,
            entityType: AuditEntities.Payment,
            entityId: confirmation.Id.ToString(),
            message: $"Reconciliação confirmou pagamento do chargeId={chargeId} via {gateway.Name}.",
            actorUserId: actorUserId,
            source: AdminAuditSources.Payments,
            metadata: new
            {
                confirmationId = confirmation.Id,
                gateway = gateway.Name,
                chargeId,
                origin
            });

        return new EventPaymentReconcileResult(
            Found: true,
            Updated: true,
            IsPaid: true,
            Message: "Reconciliação concluída: pagamento confirmado e persistido.",
            ConfirmationId: confirmation.Id,
            GatewayName: confirmation.PaymentGatewayName);
    }

    public async Task<EventPaymentReconciliationSweepResult> ReconcilePendingConfirmationsAsync(
        int take = 50,
        string? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Ignore charges older than 48 h — Pix charges expire (default 1 h) so they will never be paid.
        var cutoff = DateTime.UtcNow.AddHours(-48);

        var pendingCharges = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending
                     && c.PixTxId != null
                     && c.ConfirmedAt >= cutoff)
            .OrderBy(c => c.ConfirmedAt)
            .Select(c => new { c.PixTxId, c.Id })
            .Take(take)
            .ToListAsync(cancellationToken);

        if (pendingCharges.Count == 0)
        {
            var emptyResult = new EventPaymentReconciliationSweepResult(
                Considered: 0,
                Updated: 0,
                StillPending: 0,
                NotFound: 0);

            await WriteSweepAuditAsync(emptyResult, actorUserId);
            return emptyResult;
        }

        var updated = 0;
        var stillPending = 0;
        var notFound = 0;

        foreach (var pending in pendingCharges)
        {
            var itemResult = await ReconcileByChargeIdAsync(pending.PixTxId!, actorUserId);
            if (itemResult.Updated)
                updated++;
            else if (!itemResult.Found)
                notFound++;
            else if (!itemResult.IsPaid)
                stillPending++;
        }

        var result = new EventPaymentReconciliationSweepResult(
            Considered: pendingCharges.Count,
            Updated: updated,
            StillPending: stillPending,
            NotFound: notFound);

        await WriteSweepAuditAsync(result, actorUserId);
        return result;
    }

    private Task WriteSweepAuditAsync(EventPaymentReconciliationSweepResult result, string? actorUserId)
    {
        var origin = string.IsNullOrWhiteSpace(actorUserId)
            ? "worker.reconciliation"
            : "admin.sweep";

        var message = result.Considered == 0
            ? "Varredura de reconciliação executada sem pendências."
            : $"Varredura de reconciliação executada. Considerados={result.Considered}, Atualizados={result.Updated}, Pendentes={result.StillPending}, NãoEncontrados={result.NotFound}.";

        return _logService.AuditAsync(
            eventType: AuditEvents.PaymentReconciliationSweep,
            entityType: AuditEntities.Payment,
            entityId: null,
            message: message,
            actorUserId: actorUserId,
            source: AdminAuditSources.Payments,
            metadata: new
            {
                origin,
                considered = result.Considered,
                updated = result.Updated,
                stillPending = result.StillPending,
                notFound = result.NotFound
            });
    }

    private async Task<(IEventPaymentGateway gateway, bool paid)?> ResolveGatewayAsync(string? gatewayName, string chargeId)
    {
        if (!string.IsNullOrWhiteSpace(gatewayName))
        {
            var byName = _gatewayFactory.GetByNameIgnoringToggle(gatewayName);
            if (byName is not null)
            {
                var isPaid = await byName.IsChargePaidAsync(chargeId);
                return (byName, isPaid);
            }
        }

        foreach (var gateway in _gatewayFactory.GetAllAvailableIgnoringToggle())
        {
            var isPaid = await gateway.IsChargePaidAsync(chargeId);
            if (isPaid)
                return (gateway, true);
        }

        return null;
    }

    /// <summary>
    /// Marks as <see cref="EventConfirmationPaymentStatus.Expired"/> all Pending Pix charges
    /// whose <see cref="EventConfirmation.ConfirmedAt"/> is older than <paramref name="expiryWindow"/>.
    /// Returns the number of rows updated.
    /// </summary>
    public async Task<int> ExpireStalePixChargesAsync(
        TimeSpan expiryWindow,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var cutoff = DateTime.UtcNow - expiryWindow;

        var stale = await db.EventConfirmations
            .Where(c => c.PaymentStatus == EventConfirmationPaymentStatus.Pending
                     && c.PixTxId != null
                     && c.ConfirmedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0) return 0;

        foreach (var c in stale)
            c.PaymentStatus = EventConfirmationPaymentStatus.Expired;

        await db.SaveChangesAsync(cancellationToken);

        await _logService.LogAsync(
            $"EventPaymentReconciliation: {stale.Count} cobrança(s) Pix expirada(s) marcadas como Expired (janela={expiryWindow.TotalHours:F1}h).",
            source: "Reconciliation", level: "Info");

        return stale.Count;
    }
}
