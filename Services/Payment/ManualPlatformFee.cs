namespace Confirmai.Services.Payment;

/// <summary>
/// Platform fee rules for the V1 manual flow (no gateways).
/// The fee is charged on top of the match price and shown to the player,
/// so the amount displayed, the Pix QR payload and the ledger all agree.
/// </summary>
public static class ManualPlatformFee
{
    /// <summary>True when the fixed platform fee applies to this match.</summary>
    public static bool Applies(bool gatewaysEnabled, bool isFutsal, decimal basePrice, decimal manualFee)
        => !gatewaysEnabled && isFutsal && basePrice > 0m && manualFee > 0m;

    /// <summary>Amount the player must transfer: match price plus the fee when it applies.</summary>
    public static decimal TotalToPay(bool gatewaysEnabled, bool isFutsal, decimal basePrice, decimal manualFee)
        => Applies(gatewaysEnabled, isFutsal, basePrice, manualFee) ? basePrice + manualFee : basePrice;
}
