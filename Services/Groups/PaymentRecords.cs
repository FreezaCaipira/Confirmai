namespace Confirmai.Services.Groups;

/// <summary>
/// Delinquency and payment history records used by GroupPaymentsService.
/// Moved from DelinquencyService.cs (removed in Ciclo 29 Fase A).
/// </summary>
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
