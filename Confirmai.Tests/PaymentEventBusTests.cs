using Confirmai.Services.Payment;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

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
}
