namespace Confirmai.Services;

public sealed record EventPaymentChargeResult(string ChargeId, string BrCode);

public interface IEventPaymentGateway
{
    string Name { get; }
    string DisplayName { get; }
    bool IsAvailable { get; }

    Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId);
    Task<bool> IsChargePaidAsync(string chargeId);
}
