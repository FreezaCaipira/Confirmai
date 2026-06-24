using Confirmai.Services.Factories;

namespace Confirmai.Tests;

public class EventPaymentGatewayOptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var name = "TestGateway";
        var displayName = "Test Gateway Display";

        // Act
        var option = new EventPaymentGatewayOption(name, displayName);

        // Assert
        Assert.Equal(name, option.Name);
        Assert.Equal(displayName, option.DisplayName);
    }

    [Fact]
    public void Constructor_WithEmptyStrings()
    {
        // Act
        var option = new EventPaymentGatewayOption(string.Empty, string.Empty);

        // Assert
        Assert.Equal(string.Empty, option.Name);
        Assert.Equal(string.Empty, option.DisplayName);
    }

    [Fact]
    public void Constructor_WithNullValues()
    {
        // Act
        var option = new EventPaymentGatewayOption(null!, null!);

        // Assert
        Assert.Null(option.Name);
        Assert.Null(option.DisplayName);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters()
    {
        // Arrange
        var name = "Test-Gateway_123";
        var displayName = "Test Gateway (Display) @#$";

        // Act
        var option = new EventPaymentGatewayOption(name, displayName);

        // Assert
        Assert.Equal(name, option.Name);
        Assert.Equal(displayName, option.DisplayName);
    }

    [Fact]
    public void Constructor_WithLongStrings()
    {
        // Arrange
        var name = new string('A', 100);
        var displayName = new string('B', 200);

        // Act
        var option = new EventPaymentGatewayOption(name, displayName);

        // Assert
        Assert.Equal(100, option.Name.Length);
        Assert.Equal(200, option.DisplayName.Length);
    }
}
