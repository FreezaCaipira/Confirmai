using Confirmai.Services;

namespace Confirmai.Tests;

public class PaymentEventBusTests
{
    [Fact]
    public void NotifyPaymentConfirmed_InvokesSubscribers()
    {
        var bus = new PaymentEventBus();
        string? receivedUserId = null;
        string? receivedPaymentId = null;

        bus.OnPaymentConfirmed += (userId, paymentId) =>
        {
            receivedUserId = userId;
            receivedPaymentId = paymentId;
        };

        bus.NotifyPaymentConfirmed("user-1", "pay-abc");

        Assert.Equal("user-1", receivedUserId);
        Assert.Equal("pay-abc", receivedPaymentId);
    }

    [Fact]
    public void NotifyPaymentConfirmed_DoesNotThrow_WhenNoSubscribers()
    {
        var bus = new PaymentEventBus();

        var ex = Record.Exception(() => bus.NotifyPaymentConfirmed("user-1", "pay-abc"));

        Assert.Null(ex);
    }

    [Fact]
    public void NotifyPaymentConfirmed_InvokesMultipleSubscribers()
    {
        var bus = new PaymentEventBus();
        var callCount = 0;

        bus.OnPaymentConfirmed += (_, _) => callCount++;
        bus.OnPaymentConfirmed += (_, _) => callCount++;

        bus.NotifyPaymentConfirmed("u", "p");

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void NotifyOrderEnteredReview_InvokesSubscribers()
    {
        var bus = new PaymentEventBus();
        int? receivedOrderId = null;

        bus.OnOrderEnteredReview += orderId => receivedOrderId = orderId;

        bus.NotifyOrderEnteredReview(42);

        Assert.Equal(42, receivedOrderId);
    }

    [Fact]
    public void NotifyOrderEnteredReview_DoesNotThrow_WhenNoSubscribers()
    {
        var bus = new PaymentEventBus();

        var ex = Record.Exception(() => bus.NotifyOrderEnteredReview(99));

        Assert.Null(ex);
    }

    [Fact]
    public void NotifyOrderEnteredReview_InvokesMultipleSubscribers()
    {
        var bus = new PaymentEventBus();
        var callCount = 0;

        bus.OnOrderEnteredReview += _ => callCount++;
        bus.OnOrderEnteredReview += _ => callCount++;

        bus.NotifyOrderEnteredReview(1);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Unsubscribing_OnOrderEnteredReview_StopsInvocation()
    {
        var bus = new PaymentEventBus();
        var callCount = 0;
        Action<int> handler = _ => callCount++;

        bus.OnOrderEnteredReview += handler;
        bus.NotifyOrderEnteredReview(1);

        bus.OnOrderEnteredReview -= handler;
        bus.NotifyOrderEnteredReview(2);

        Assert.Equal(1, callCount);
    }
}
