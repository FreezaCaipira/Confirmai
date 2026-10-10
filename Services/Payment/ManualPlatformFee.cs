namespace Confirmai.Services.Payment;

/// <summary>
/// Platform fee rules for the V1 manual flow (no gateways).
/// The fee is charged on top of the entry price and shown to the player,
/// so the amount displayed, the Pix QR payload and the ledger all agree.
/// C39-A: a regra de "aplica?" passou a olhar esporte/carimbo pela mesma
/// logica de <see cref="PlatformFeePolicy.AppliesTo"/> — quem chama ja
/// resolveu se a confirmacao e do fluxo manual; aqui o fee decide o total.
/// </summary>
public static class ManualPlatformFee
{
    /// <summary>True when the platform fee applies to this charge.</summary>
    public static bool Applies(bool gatewaysEnabled, decimal basePrice, decimal fee)
        => !gatewaysEnabled && basePrice > 0m && fee > 0m;

    /// <summary>Amount the player must transfer: entry price plus the fee when it applies.</summary>
    public static decimal TotalToPay(bool gatewaysEnabled, decimal basePrice, decimal fee)
        => Applies(gatewaysEnabled, basePrice, fee) ? basePrice + fee : basePrice;
}
