namespace Confirmai.Services.Interfaces;

using Confirmai.Models;

public sealed record EventPaymentChargeResult(string ChargeId, string BrCode);

public interface IEventPaymentGateway
{
    string Name { get; }
    string DisplayName { get; }
    bool IsAvailable { get; }

    Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId, GroupPayoutAccount? payoutAccount = null);
    Task<bool> IsChargePaidAsync(string chargeId);
}
