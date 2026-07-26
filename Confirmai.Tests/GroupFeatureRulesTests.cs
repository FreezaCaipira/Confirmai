using Confirmai.Models;
using Confirmai.Services.Groups;

namespace Confirmai.Tests;

public class GroupFeatureRulesTests
{
    [Fact]
    public void ApplyPaymentGatewaysToggle_WhenDisabling_TurnsOffRankingAndVoting()
    {
        // Arrange
        var group = new Group
        {
            EnablePaymentGateways = true,
            EnablePostMatchRanking = true,
            EnableBestPlayerVoting = true
        };

        // Act
        GroupFeatureRules.ApplyPaymentGatewaysToggle(group, enabled: false);

        // Assert — disabling gateways cascades to the paid extras
        Assert.False(group.EnablePaymentGateways);
        Assert.False(group.EnablePostMatchRanking);
        Assert.False(group.EnableBestPlayerVoting);
    }

    [Fact]
    public void ApplyPaymentGatewaysToggle_WhenEnabling_DoesNotAutoEnableExtras()
    {
        // Arrange — extras were off; enabling gateways must not turn them on
        var group = new Group
        {
            EnablePaymentGateways = false,
            EnablePostMatchRanking = false,
            EnableBestPlayerVoting = false
        };

        // Act
        GroupFeatureRules.ApplyPaymentGatewaysToggle(group, enabled: true);

        // Assert
        Assert.True(group.EnablePaymentGateways);
        Assert.False(group.EnablePostMatchRanking);
        Assert.False(group.EnableBestPlayerVoting);
    }

    [Fact]
    public void ApplyPaymentGatewaysToggle_WhenEnabling_PreservesExistingExtras()
    {
        // Arrange — extras already on stay on when gateways are enabled
        var group = new Group
        {
            EnablePaymentGateways = false,
            EnablePostMatchRanking = true,
            EnableBestPlayerVoting = true
        };

        // Act
        GroupFeatureRules.ApplyPaymentGatewaysToggle(group, enabled: true);

        // Assert
        Assert.True(group.EnablePaymentGateways);
        Assert.True(group.EnablePostMatchRanking);
        Assert.True(group.EnableBestPlayerVoting);
    }

    [Fact]
    public void ApplyPaymentGatewaysToggle_WhenGroupIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => GroupFeatureRules.ApplyPaymentGatewaysToggle(null!, enabled: false));
    }
}
