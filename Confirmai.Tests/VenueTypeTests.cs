using Confirmai.Enums;

namespace Confirmai.Tests;

public class VenueTypeTests
{
    [Fact]
    public void Quadra_HasCorrectValue()
    {
        // Assert
        Assert.Equal(1, (int)VenueType.Quadra);
    }

    [Fact]
    public void Society_HasCorrectValue()
    {
        // Assert
        Assert.Equal(2, (int)VenueType.Society);
    }

    [Fact]
    public void Campo_HasCorrectValue()
    {
        // Assert
        Assert.Equal(3, (int)VenueType.Campo);
    }

    [Fact]
    public void AllValues_AreUnique()
    {
        // Arrange
        var values = new[] { VenueType.Quadra, VenueType.Society, VenueType.Campo };

        // Assert
        Assert.Equal(3, values.Distinct().Count());
    }

    [Fact]
    public void Quadra_IsNotSociety()
    {
        // Assert
        Assert.NotEqual(VenueType.Quadra, VenueType.Society);
    }

    [Fact]
    public void Quadra_IsNotCampo()
    {
        // Assert
        Assert.NotEqual(VenueType.Quadra, VenueType.Campo);
    }

    [Fact]
    public void Society_IsNotCampo()
    {
        // Assert
        Assert.NotEqual(VenueType.Society, VenueType.Campo);
    }
}
