using Confirmai.Enums;

namespace Confirmai.Tests;

public class PokerEventTypeTests
{
    [Fact]
    public void Tournament_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)PokerEventType.Tournament);
    }

    [Fact]
    public void CashGame_HasCorrectValue()
    {
        // Assert
        Assert.Equal(2, (int)PokerEventType.CashGame);
    }

    [Fact]
    public void HomeGame_HasCorrectValue()
    {
        // Assert
        Assert.Equal(3, (int)PokerEventType.HomeGame);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[] { PokerEventType.Tournament, PokerEventType.CashGame, PokerEventType.HomeGame };

        // Assert
        Assert.Equal(3, values.Distinct().Count());
    }

    [Fact]
    public void Tournament_IsNotCashGame()
    {
        // Assert
        Assert.NotEqual(PokerEventType.Tournament, PokerEventType.CashGame);
    }

    [Fact]
    public void Tournament_IsNotHomeGame()
    {
        // Assert
        Assert.NotEqual(PokerEventType.Tournament, PokerEventType.HomeGame);
    }

    [Fact]
    public void CashGame_IsNotHomeGame()
    {
        // Assert
        Assert.NotEqual(PokerEventType.CashGame, PokerEventType.HomeGame);
    }
}
