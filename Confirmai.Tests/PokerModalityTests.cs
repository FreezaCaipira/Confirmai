using Confirmai.Enums;

namespace Confirmai.Tests;

public class PokerModalityTests
{
    [Fact]
    public void Vanilla_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)PokerModality.Vanilla);
    }

    [Fact]
    public void PKO_HasCorrectValue()
    {
        // Assert
        Assert.Equal(2, (int)PokerModality.PKO);
    }

    [Fact]
    public void Freezeout_HasCorrectValue()
    {
        // Assert
        Assert.Equal(3, (int)PokerModality.Freezeout);
    }

    [Fact]
    public void PLO4_HasCorrectValue()
    {
        // Assert
        Assert.Equal(4, (int)PokerModality.PLO4);
    }

    [Fact]
    public void PLO5_HasCorrectValue()
    {
        // Assert
        Assert.Equal(5, (int)PokerModality.PLO5);
    }

    [Fact]
    public void PLO6_HasCorrectValue()
    {
        // Assert
        Assert.Equal(6, (int)PokerModality.PLO6);
    }

    [Fact]
    public void DealerChoice_HasCorrectValue()
    {
        // Assert
        Assert.Equal(7, (int)PokerModality.DealerChoice);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[]
        {
            PokerModality.Vanilla,
            PokerModality.PKO,
            PokerModality.Freezeout,
            PokerModality.PLO4,
            PokerModality.PLO5,
            PokerModality.PLO6,
            PokerModality.DealerChoice
        };

        // Assert
        Assert.Equal(7, values.Distinct().Count());
    }
}
