using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Payment;

/// <summary>
/// Manages the platform fee ledger for the V1 manual flow.
/// Stamps the fee amount on confirmations when they are marked as paid,
/// and tracks the group balance (accrued, settled, due).
/// </summary>
public class PlatformFeeLedgerService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IOptions<FeeOptions> _feeOptions;

    public PlatformFeeLedgerService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptions<FeeOptions> feeOptions)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
    }

    /// <summary>
    /// Stamps the platform fee on a confirmation when it is marked as paid.
    /// Idempotent: only stamps if PlatformFeeAmount is still null.
    /// Only applies to futsal events in manual mode (no gateways) with a positive price.
    /// </summary>
    public async Task<bool> StampFeeOnPaidAsync(int confirmationId)
    {
        var fee = _feeOptions.Value.ManualPlatformFeeFixed;
        if (fee <= 0) return false;

        await using var db = _dbFactory.CreateDbContext();

        var conf = await db.EventConfirmations
            .AsTracking()
            .Include(c => c.Event)
                .ThenInclude(e => e!.Group)
            .FirstOrDefaultAsync(c => c.Id == confirmationId);

        if (conf is null) return false;
        if (conf.PlatformFeeAmount is not null) return false; // idempotent
        if (conf.PaymentStatus != EventConfirmationPaymentStatus.Paid) return false;

        var group = conf.Event?.Group;
        if (group is null) return false;
        if (group.Sport != Sport.Futsal) return false;
        if (group.EnablePaymentGateways) return false;
        if (conf.Event!.Price.GetValueOrDefault() <= 0) return false;

        conf.PlatformFeeAmount = fee;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Returns the group's fee balance: accrued, settled, and due.
    /// Due = Accrued - Settled (only counting settlements with status Pago).
    /// </summary>
    public async Task<(decimal Accrued, decimal Settled, decimal Due)> GetGroupBalanceAsync(int groupId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var accrued = await db.EventConfirmations
            .Where(c => c.Event!.GroupId == groupId && c.PlatformFeeAmount.HasValue)
            .SumAsync(c => c.PlatformFeeAmount!.Value);

        var settled = await db.PlatformFeeSettlements
            .Where(s => s.GroupId == groupId && s.Status == PlatformFeeSettlementStatus.Pago)
            .SumAsync(s => s.Amount);

        return (accrued, settled, accrued - settled);
    }
}
