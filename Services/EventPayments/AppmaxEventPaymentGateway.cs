using Confirmai.Services.Payment;
namespace Confirmai.Services;

public sealed class AppmaxEventPaymentGateway : IEventPaymentGateway
{
    private readonly AppmaxPixService _appmax;

    public AppmaxEventPaymentGateway(AppmaxPixService appmax)
    {
        _appmax = appmax;
    }

    public string Name => "Appmax";
    public string DisplayName => "Pix · Appmax";
    public bool IsAvailable => _appmax.IsEnabled;

    public async Task<EventPaymentChargeResult> CreateChargeAsync(decimal amount, int confirmationId)
    {
        var (chargeId, brCode) = await _appmax.CreateChargeAsync(amount, confirmationId);
        return new EventPaymentChargeResult(chargeId, brCode);
    }

    public Task<bool> IsChargePaidAsync(string chargeId)
    {
        return _appmax.IsChargePaidAsync(chargeId);
    }
}
