using System.Linq;
using Confirmai.Models;
using Confirmai.Pages;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Cobre a ressalva do Ciclo 23 (Senior): default manual (V1) e o alvo do
/// link corrigido no PixReceiverSelector (rota de perfil valida).
/// </summary>
public class GroupPaymentDefaultTests
{
    [Fact]
    public void NewGroup_StartsInManualPaymentMode_ByDefault()
    {
        var group = new Group();

        Assert.False(group.EnablePaymentGateways);
    }

    [Fact]
    public void NewGroup_ManualDefault_DoesNotEnableGatewayOnlyFeatures()
    {
        var group = new Group();

        Assert.False(group.EnablePaymentGateways);
        Assert.False(group.EnablePostMatchRanking);
        Assert.False(group.EnableBestPlayerVoting);
    }
}

public class ProfileRouteTests
{
    [Fact]
    public void ProfilePage_ExposesProfileIdRoute_SoPixReceiverLinkIsValid()
    {
        var routes = typeof(Profile)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(r => r.Template)
            .ToArray();

        Assert.Contains("/profile/{Id}", routes);
    }
}
