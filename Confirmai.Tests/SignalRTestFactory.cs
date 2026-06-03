using Confirmai.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Confirmai.Tests;

internal static class SignalRTestFactory
{
    public static IHubContext<PaymentHub> CreateHubContext()
    {
        var clientProxy = CreateClientProxy();
        var clients = new Mock<IHubClients>();
        clients.Setup(collection => collection.User(It.IsAny<string>())).Returns(clientProxy.Object);
        return BuildHubContext(clients).Object;
    }

    public static (IHubContext<PaymentHub> HubContext, Mock<IClientProxy> ClientProxy) CreateHubContextForUser(string userId)
    {
        var clientProxy = CreateClientProxy();

        var clients = new Mock<IHubClients>();
        clients.Setup(collection => collection.User(userId)).Returns(clientProxy.Object);
        return (BuildHubContext(clients).Object, clientProxy);
    }

    private static Mock<IClientProxy> CreateClientProxy()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return clientProxy;
    }

    private static Mock<IHubContext<PaymentHub>> BuildHubContext(Mock<IHubClients> clients)
    {
        var hubContext = new Mock<IHubContext<PaymentHub>>();
        hubContext.SetupGet(context => context.Clients).Returns(clients.Object);
        hubContext.SetupGet(context => context.Groups).Returns(Mock.Of<IGroupManager>());
        return hubContext;
    }
}

