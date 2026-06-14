using Confirmai.Services.Payment;

namespace Confirmai.Tests;

public class PaymentEventBusTests
{
    [Fact]
    public void NotifyPaymentConfirmed_InvokesHandler_WithCorrectArgs()
    {
        var bus = new PaymentEventBus();
        string? receivedUserId = null;
        string? receivedPaymentId = null;

        bus.OnPaymentConfirmed += (userId, paymentId) =>
        {
            receivedUserId = userId;
            receivedPaymentId = paymentId;
        };

        bus.NotifyPaymentConfirmed("user-42", "pay-100");

        Assert.Equal("user-42", receivedUserId);
        Assert.Equal("pay-100", receivedPaymentId);
    }

    [Fact]
    public void NotifyPaymentConfirmed_DoesNotThrow_WhenNoHandlers()
    {
        var bus = new PaymentEventBus();

        var ex = Record.Exception(() => bus.NotifyPaymentConfirmed("u", "p"));

        Assert.Null(ex);
    }

    [Fact]
    public void NotifyPaymentConfirmed_InvokesMultipleHandlers()
    {
        var bus = new PaymentEventBus();
        var calls = new List<(string UserId, string PaymentId)>();

        bus.OnPaymentConfirmed += (u, p) => calls.Add((u, p));
        bus.OnPaymentConfirmed += (u, p) => calls.Add((u, p));

        bus.NotifyPaymentConfirmed("u1", "p1");

        Assert.Equal(2, calls.Count);
        Assert.All(calls, c =>
        {
            Assert.Equal("u1", c.UserId);
            Assert.Equal("p1", c.PaymentId);
        });
    }

    [Fact]
    public void OnPaymentConfirmed_CanUnsubscribe()
    {
        var bus = new PaymentEventBus();
        var called = false;

        void Handler(string u, string p) => called = true;

        bus.OnPaymentConfirmed += Handler;
        bus.OnPaymentConfirmed -= Handler;

        bus.NotifyPaymentConfirmed("u", "p");

        Assert.False(called);
    }
}
