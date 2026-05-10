namespace Confirmai.Services;

public sealed class PaymentEventBus
{
    public event Action<string, string>? OnPaymentConfirmed;
    public event Action<int>? OnOrderEnteredReview;

    public void NotifyPaymentConfirmed(string userId, string paymentId)
    {
        OnPaymentConfirmed?.Invoke(userId, paymentId);
    }

    public void NotifyOrderEnteredReview(int orderId)
    {
        OnOrderEnteredReview?.Invoke(orderId);
    }
}
