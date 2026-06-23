using Confirmai.Models.Admin;

namespace Confirmai.Tests;

public class EditUserModelTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var model = new EditUserModel();

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void InstagramHandle_CanBeSet()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.InstagramHandle = "@testuser";

        // Assert
        Assert.Equal("@testuser", model.InstagramHandle);
    }

    [Fact]
    public void DiscordHandle_CanBeSet()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.DiscordHandle = "testuser#1234";

        // Assert
        Assert.Equal("testuser#1234", model.DiscordHandle);
    }

    [Fact]
    public void PaypalAddress_CanBeSet()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.PaypalAddress = "test@example.com";

        // Assert
        Assert.Equal("test@example.com", model.PaypalAddress);
    }

    [Fact]
    public void BinanceAddress_CanBeSet()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.BinanceAddress = "0x1234567890abcdef";

        // Assert
        Assert.Equal("0x1234567890abcdef", model.BinanceAddress);
    }

    [Fact]
    public void PixKey_CanBeSet()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.PixKey = "12345678900";

        // Assert
        Assert.Equal("12345678900", model.PixKey);
    }

    [Fact]
    public void XHandle_CanBeSet()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.XHandle = "@testuser";

        // Assert
        Assert.Equal("@testuser", model.XHandle);
    }

    [Fact]
    public void AllProperties_CanBeNull()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.InstagramHandle = null;
        model.DiscordHandle = null;
        model.PaypalAddress = null;
        model.BinanceAddress = null;
        model.PixKey = null;
        model.XHandle = null;

        // Assert
        Assert.Null(model.InstagramHandle);
        Assert.Null(model.DiscordHandle);
        Assert.Null(model.PaypalAddress);
        Assert.Null(model.BinanceAddress);
        Assert.Null(model.PixKey);
        Assert.Null(model.XHandle);
    }

    [Fact]
    public void AllProperties_CanBeEmptyString()
    {
        // Arrange
        var model = new EditUserModel();

        // Act
        model.InstagramHandle = string.Empty;
        model.DiscordHandle = string.Empty;
        model.PaypalAddress = string.Empty;
        model.BinanceAddress = string.Empty;
        model.PixKey = string.Empty;
        model.XHandle = string.Empty;

        // Assert
        Assert.Equal(string.Empty, model.InstagramHandle);
        Assert.Equal(string.Empty, model.DiscordHandle);
        Assert.Equal(string.Empty, model.PaypalAddress);
        Assert.Equal(string.Empty, model.BinanceAddress);
        Assert.Equal(string.Empty, model.PixKey);
        Assert.Equal(string.Empty, model.XHandle);
    }
}
