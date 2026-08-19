using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Services.Admin;

/// <summary>
/// Handles delinquency data loading: unpaid confirmations and payment history.
/// Applies filters: past events, non-GK, pending status, no gateway payments.
/// </summary>
public class DelinquencyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IOptions<FeeOptions> _feeOptions;

    public DelinquencyService(IDbContextFactory<AppDbContext> dbFactory, IOptions<FeeOptions> feeOptions)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _feeOptions = feeOptions ?? throw new ArgumentNullException(nameof(feeOptions));
    }

    /// <summary>
    /// Loads unpaid confirmations (delinquent entries) for a group.
    /// Filters: past events, price > 0, !HasPaid, Pending status, not GK, group members.
    /// </summary>
    public async Task<List<UserDelinquency>> LoadUnpaidConfirmationsAsync(
        int groupId,
        HashSet<string> memberIds,
        Sport sport,
        bool enablePaymentGateways = false)
    {
        await using var db = _dbFactory.CreateDbContext();

        var nowUtc = DateTime.UtcNow;
        var manualFee = _feeOptions.Value.ManualPlatformFeeFixed;
        var isFutsal = sport == Sport.Futsal;

        var unpaidConfirmations = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.Event.StartsAt < nowUtc &&  // Past events only
                c.Event.Price != null &&
                c.Event.Price > 0 &&
                !c.HasPaid &&
                c.PaymentStatus == EventConfirmationPaymentStatus.Pending &&
                c.Position != FutsalPosition.Goalkeeper &&  // No GK
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderBy(c => c.Event.StartsAt)
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.EventId,
                c.Event,
                c.User,
                c.PaymentGatewayName,
                c.Position,
                HasProof = c.PixProofUploadedAt != null,
            })
            .ToListAsync();

        var delinquencyList = unpaidConfirmations
            .GroupBy(c => c.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                var userName = user?.FullName ?? user?.UserName ?? "Jogador";
                var entries = g.Select(c =>
                {
                    var href = sport == Sport.Futsal
                        ? $"/futsal/{c.EventId}"
                        : $"/poker/{c.EventId}";
                    return new DelinquencyEntry(
                        c.Id,
                        c.EventId,
                        c.Event.StartsAt,
                        ManualPlatformFee.TotalToPay(enablePaymentGateways, isFutsal, c.Event.Price!.Value, manualFee),
                        href,
                        c.HasProof);
                }).ToList();
                return new UserDelinquency(g.Key, userName, entries);
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();

        return delinquencyList;
    }

    /// <summary>
    /// Loads payment history (last 50 manual markups) for a group.
    /// </summary>
    public async Task<List<PaymentHistoryEntry>> LoadPaymentHistoryAsync(
        int groupId,
        HashSet<string> memberIds,
        Sport sport,
        bool enablePaymentGateways = false)
    {
        await using var db = _dbFactory.CreateDbContext();

        var manualFee = _feeOptions.Value.ManualPlatformFeeFixed;
        var isFutsal = sport == Sport.Futsal;

        var manualPaid = await db.EventConfirmations
            .Where(c =>
                c.Event.GroupId == groupId &&
                c.HasPaid &&
                c.MarkedPaidByUserId != null &&
                memberIds.Contains(c.UserId))
            .Include(c => c.Event)
            .Include(c => c.User)
            .OrderByDescending(c => c.MarkedPaidAt)
            .Take(50)
            .ToListAsync();

        var adminIds = manualPaid.Select(c => c.MarkedPaidByUserId!).Distinct().ToList();
        var adminUsers = await db.Users
            .Where(u => adminIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FullName ?? u.UserName ?? u.Email })
            .ToListAsync();
        var adminMap = adminUsers.ToDictionary(u => u.Id, u => u.Name ?? "Admin");

        var paymentHistory = manualPaid.Select(c =>
        {
            var href = sport == Sport.Futsal ? $"/futsal/{c.EventId}" : $"/poker/{c.EventId}";
            var userName = c.User?.FullName ?? c.User?.UserName ?? "Jogador";
            var adminName = adminMap.TryGetValue(c.MarkedPaidByUserId!, out var n) ? n : "Admin";
            return new PaymentHistoryEntry(
                userName,
                c.Event.StartsAt,
                ManualPlatformFee.TotalToPay(enablePaymentGateways, isFutsal, c.Event.Price ?? 0, manualFee),
                href,
                adminName,
                c.MarkedPaidAt!.Value,
                c.Id,
                c.PixProofImageData != null && c.PixProofImageData.Length > 0);
        }).ToList();

        return paymentHistory;
    }
}

public record DelinquencyEntry(
    int ConfirmationId,
    int EventId,
    DateTime EventDate,
    decimal EventPrice,
    string EventHref,
    bool HasProof);

public record UserDelinquency(
    string UserId,
    string UserName,
    List<DelinquencyEntry> Entries)
{
    public decimal TotalAmount => Entries.Sum(e => e.EventPrice);
}

public record PaymentHistoryEntry(
    string UserName,
    DateTime EventDate,
    decimal EventPrice,
    string EventHref,
    string AdminName,
    DateTime MarkedAt,
    int? ConfirmationId = null,
    bool HasProof = false);

public record PendingProofEntry(
    int ConfirmationId,
    string UserId,
    string UserName,
    int EventId,
    string EventName,
    string GroupName,
    DateTime EventDate,
    decimal EventPrice,
    string EventHref,
    DateTime ProofUploadedAt);
