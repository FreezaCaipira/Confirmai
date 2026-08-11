using Confirmai.Services.Payment;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// The amount shown to the player, the Pix QR payload and the ledger must all agree.
/// Regression guard for the Ciclo 26 review: the summary displayed price + fee while the
/// QR still encoded the bare match price, so the organizer received less than the total shown.
/// </summary>
public class ManualPlatformFeeTests
{
    [Fact]
    public void Applies_ManualFutsalWithPrice_IsTrue()
    {
        Assert.True(ManualPlatformFee.Applies(
            gatewaysEnabled: false, isFutsal: true, basePrice: 15m, manualFee: 0.75m));
    }

    [Fact]
    public void Applies_GatewaysEnabled_IsFalse()
    {
        Assert.False(ManualPlatformFee.Applies(
            gatewaysEnabled: true, isFutsal: true, basePrice: 15m, manualFee: 0.75m));
    }

    [Fact]
    public void Applies_NonFutsal_IsFalse()
    {
        Assert.False(ManualPlatformFee.Applies(
            gatewaysEnabled: false, isFutsal: false, basePrice: 15m, manualFee: 0.75m));
    }

    [Fact]
    public void Applies_FreeMatch_IsFalse()
    {
        Assert.False(ManualPlatformFee.Applies(
            gatewaysEnabled: false, isFutsal: true, basePrice: 0m, manualFee: 0.75m));
    }

    [Fact]
    public void Applies_FeeNotConfigured_IsFalse()
    {
        Assert.False(ManualPlatformFee.Applies(
            gatewaysEnabled: false, isFutsal: true, basePrice: 15m, manualFee: 0m));
    }

    [Fact]
    public void TotalToPay_WhenFeeApplies_AddsFeeOnTop()
    {
        var total = ManualPlatformFee.TotalToPay(
            gatewaysEnabled: false, isFutsal: true, basePrice: 15m, manualFee: 0.75m);

        Assert.Equal(15.75m, total);
    }

    [Fact]
    public void TotalToPay_WhenFeeDoesNotApply_IsBasePrice()
    {
        var total = ManualPlatformFee.TotalToPay(
            gatewaysEnabled: false, isFutsal: false, basePrice: 15m, manualFee: 0.75m);

        Assert.Equal(15m, total);
    }

    [Fact]
    public void PixPayload_EncodesTheSameTotalShownToThePlayer()
    {
        var total = ManualPlatformFee.TotalToPay(
            gatewaysEnabled: false, isFutsal: true, basePrice: 15m, manualFee: 0.75m);

        var payload = EventPaymentService.BuildPixStaticPayload("pix@org", "Racha", "Sao Paulo", total);

        // Field 54 carries the transaction amount in the Pix BR Code.
        Assert.Contains("540515.75", payload);
    }
}
