using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;
using Confirmai.Models;

namespace Confirmai.Services.EventPayments;

public sealed class EfiBankEventPaymentGateway : IEventPaymentGateway
{
    private readonly EfiBankPixService _efiBank;

    public EfiBankEventPaymentGateway(EfiBankPixService efiBank)
    {
        _efiBank = efiBank;
    }

    public string Name => "EfiBank";
    public string DisplayName => "Pix · EfiBank";
    public bool IsAvailable => _efiBank.IsEnabled;

    public async Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId, GroupPayoutAccount? payoutAccount = null, decimal serviceFeePercentage = 0)
    {
        var (txId, brCode) = await _efiBank.CreateChargeAsync(amount, confirmationId, payoutAccount, serviceFeePercentage);
        return new EventPaymentChargeResult(txId, brCode);
    }

    public Task<bool> IsChargePaidAsync(string chargeId)
        => _efiBank.IsChargePaidAsync(chargeId);
}
