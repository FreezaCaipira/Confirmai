using Confirmai.Enums;

namespace Confirmai.Tests;

public class SportTests
{
    [Fact]
    public void Futsal_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)Sport.Futsal);
    }

    [Fact]
    public void Poker_HasCorrectValue()
    {
        // Assert
        Assert.Equal(2, (int)Sport.Poker);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[] { Sport.Futsal, Sport.Poker };

        // Assert
        Assert.Equal(2, values.Distinct().Count());
    }

    [Fact]
    public void Futsal_IsNotPoker()
    {
        // Assert
        Assert.NotEqual(Sport.Futsal, Sport.Poker);
    }

    [Fact]
    public void Poker_IsNotFutsal()
    {
        // Assert
        Assert.NotEqual(Sport.Poker, Sport.Futsal);
    }
}
