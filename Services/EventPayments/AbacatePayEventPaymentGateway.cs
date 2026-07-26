using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;
using Confirmai.Models;

namespace Confirmai.Services.EventPayments;

public sealed class AbacatePayEventPaymentGateway : IEventPaymentGateway
{
    private readonly AbacatePayPixService _abacatePay;

    public AbacatePayEventPaymentGateway(AbacatePayPixService abacatePay)
    {
        _abacatePay = abacatePay;
    }

    public string Name => "Pix";
    public string DisplayName => "Pix · AbacatePay";
    public bool IsAvailable => _abacatePay.IsEnabled;

    public async Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId, GroupPayoutAccount? payoutAccount = null)
    {
        var orderId = $"event-confirmation-{confirmationId}";
        var (brCode, chargeId) = await _abacatePay.GenerateAddressAsync(amount, orderId);
        return new EventPaymentChargeResult(chargeId, brCode);
    }

    public async Task<bool> IsChargePaidAsync(string chargeId)
        => await _abacatePay.GetReceivedAmountAsync(chargeId) > 0m;
}
