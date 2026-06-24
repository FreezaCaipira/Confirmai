using Confirmai.Enums;
using Confirmai.Shared.Helpers;

namespace Confirmai.Tests;

public class EventMinimumsTests
{
    [Fact]
    public void Get_ReturnsFutsalMinimums()
    {
        var result = EventMinimums.Get(Sport.Futsal);
        
        Assert.Equal(8, result.MinOutfield);
        Assert.Equal(2, result.MinGoalkeepers);
        Assert.Equal(10, result.Total);
    }

    [Fact]
    public void Get_ReturnsPokerMinimums()
    {
        var result = EventMinimums.Get(Sport.Poker);
        
        Assert.Equal(0, result.MinOutfield);
        Assert.Equal(0, result.MinGoalkeepers);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void Get_ReturnsZeroMinimums_ForUnknownSport()
    {
        var result = EventMinimums.Get((Sport)999);
        
        Assert.Equal(0, result.MinOutfield);
        Assert.Equal(0, result.MinGoalkeepers);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void HasQuorum_ReturnsTrue_WhenFutsalMinimumsMet()
    {
        var result = EventMinimums.HasQuorum(Sport.Futsal, 8, 2);
        
        Assert.True(result);
    }

    [Fact]
    public void HasQuorum_ReturnsTrue_WhenFutsalMinimumsExceeded()
    {
        var result = EventMinimums.HasQuorum(Sport.Futsal, 10, 3);
        
        Assert.True(result);
    }

    [Fact]
    public void HasQuorum_ReturnsFalse_WhenFutsalOutfieldBelowMinimum()
    {
        var result = EventMinimums.HasQuorum(Sport.Futsal, 7, 2);
        
        Assert.False(result);
    }

    [Fact]
    public void HasQuorum_ReturnsFalse_WhenFutsalGoalkeepersBelowMinimum()
    {
        var result = EventMinimums.HasQuorum(Sport.Futsal, 8, 1);
        
        Assert.False(result);
    }

    [Fact]
    public void HasQuorum_ReturnsFalse_WhenFutsalBothBelowMinimum()
    {
        var result = EventMinimums.HasQuorum(Sport.Futsal, 5, 1);
        
        Assert.False(result);
    }

    [Fact]
    public void HasQuorum_ReturnsTrue_WhenPokerZeroConfirmed()
    {
        var result = EventMinimums.HasQuorum(Sport.Poker, 0, 0);
        
        Assert.True(result);
    }

    [Fact]
    public void HasQuorum_ReturnsTrue_WhenPokerAnyConfirmed()
    {
        var result = EventMinimums.HasQuorum(Sport.Poker, 5, 0);
        
        Assert.True(result);
    }

    [Fact]
    public void HasQuorum_ReturnsTrue_WhenUnknownSport()
    {
        var result = EventMinimums.HasQuorum((Sport)999, 0, 0);
        
        Assert.True(result);
    }

    [Fact]
    public void Lacking_ReturnsZero_WhenFutsalMinimumsMet()
    {
        var result = EventMinimums.Lacking(Sport.Futsal, 8, 2);
        
        Assert.Equal(0, result);
    }

    [Fact]
    public void Lacking_ReturnsZero_WhenFutsalMinimumsExceeded()
    {
        var result = EventMinimums.Lacking(Sport.Futsal, 10, 3);
        
        Assert.Equal(0, result);
    }

    [Fact]
    public void Lacking_ReturnsOne_WhenFutsalOutfieldOneBelow()
    {
        var result = EventMinimums.Lacking(Sport.Futsal, 7, 2);
        
        Assert.Equal(1, result);
    }

    [Fact]
    public void Lacking_ReturnsOne_WhenFutsalGoalkeeperOneBelow()
    {
        var result = EventMinimums.Lacking(Sport.Futsal, 8, 1);
        
        Assert.Equal(1, result);
    }

    [Fact]
    public void Lacking_ReturnsSum_WhenFutsalBothBelow()
    {
        var result = EventMinimums.Lacking(Sport.Futsal, 5, 1);
        
        Assert.Equal(4, result); // (8-5) + (2-1) = 3 + 1 = 4
    }

    [Fact]
    public void Lacking_ReturnsZero_WhenPokerZeroConfirmed()
    {
        var result = EventMinimums.Lacking(Sport.Poker, 0, 0);
        
        Assert.Equal(0, result);
    }

    [Fact]
    public void Lacking_ReturnsZero_WhenPokerAnyConfirmed()
    {
        var result = EventMinimums.Lacking(Sport.Poker, 5, 0);
        
        Assert.Equal(0, result);
    }

    [Fact]
    public void Lacking_ReturnsZero_WhenUnknownSport()
    {
        var result = EventMinimums.Lacking((Sport)999, 0, 0);
        
        Assert.Equal(0, result);
    }

    [Fact]
    public void Lacking_DoesNotReturnNegative_WhenConfirmedExceedsMinimum()
    {
        var result = EventMinimums.Lacking(Sport.Futsal, 20, 10);
        
        Assert.Equal(0, result);
    }
}
