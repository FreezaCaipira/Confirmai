namespace Confirmai.Services;

public sealed class PaymentEventBus
{
    public event Action<string, string>? OnPaymentConfirmed;

    public void NotifyPaymentConfirmed(string userId, string paymentId)
    {
        OnPaymentConfirmed?.Invoke(userId, paymentId);
    }
}
