namespace Confirmai.Enums;

public enum EventConfirmationPaymentStatus
{
    Pending  = 0,
    Paid     = 1,
    Failed   = 2,
    Refunded = 3,
    /// <summary>Pix charge window elapsed without payment; reconciliation will no longer poll it.</summary>
    Expired  = 4,
}
