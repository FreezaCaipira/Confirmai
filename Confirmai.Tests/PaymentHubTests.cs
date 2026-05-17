using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Confirmai.Hubs;

namespace Confirmai.Tests;

public class PaymentHubTests
{
    [Fact]
    public void PaymentHub_HasAuthorizeAttribute()
    {
        var attrs = typeof(PaymentHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);

        Assert.Single(attrs);
    }

    [Fact]
    public void PaymentHub_ExtendsHub()
    {
        Assert.True(typeof(Hub).IsAssignableFrom(typeof(PaymentHub)));
    }

    [Fact]
    public void SignalRTestFactory_CreateHubContext_ReturnsNonNull()
    {
        var hubContext = SignalRTestFactory.CreateHubContext();

        Assert.NotNull(hubContext);
        Assert.NotNull(hubContext.Clients);
    }

    [Fact]
    public void SignalRTestFactory_CreateHubContextForUser_ReturnsClientProxy()
    {
        var (hubContext, clientProxy) = SignalRTestFactory.CreateHubContextForUser("user-42");

        Assert.NotNull(hubContext);
        Assert.NotNull(clientProxy);

        var proxy = hubContext.Clients.User("user-42");
        Assert.NotNull(proxy);
    }
}
