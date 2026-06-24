using Confirmai.Configuration;

namespace Confirmai.Tests;

public class AbacatePayOptionsTests
{
    [Fact]
    public void Section_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("AbacatePay", AbacatePayOptions.Section);
    }

    [Fact]
    public void PublicHmacKey_ConstantValue()
    {
        // Act & Assert
        Assert.Equal(
            "t9dXRhHHo3yDEj5pVDYz0frf7q6bMKyMRmxxCPIPp3RCplBfXRxqlC6ZpiWmOqj4" +
            "L63qEaeUOtrCI8P0VMUgo6iIga2ri9ogaHFs0WIIywSMg0q7RmBfybe1E5XJcfC4" +
            "IW3alNqym0tXoAKkzvfEjZxV6bE0oG2zJrNNYmUCKZyV0KZ3JS8Votf9EAWWYdi" +
            "DkMkpbMdPggfh1EqHlVkMiTady6jOR3hyzGEHrIz2Ret0xHKMbiqkr9HS1JhNHDX9",
            AbacatePayOptions.PublicHmacKey);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new AbacatePayOptions();

        // Assert
        Assert.Null(options.ApiKey);
        Assert.Equal("https://api.abacatepay.com/v2", options.BaseUrl);
        Assert.Null(options.WebhookSecret);
        Assert.Equal(3600, options.PixExpiresInSeconds);
    }

    [Fact]
    public void IsEnabled_WhenApiKeyIsNull_ReturnsFalse()
    {
        // Arrange
        var options = new AbacatePayOptions { ApiKey = null };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenApiKeyIsEmpty_ReturnsFalse()
    {
        // Arrange
        var options = new AbacatePayOptions { ApiKey = string.Empty };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenApiKeyIsWhitespace_ReturnsFalse()
    {
        // Arrange
        var options = new AbacatePayOptions { ApiKey = "   " };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenApiKeyHasValue_ReturnsTrue()
    {
        // Arrange
        var options = new AbacatePayOptions { ApiKey = "test-api-key" };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange
        var options = new AbacatePayOptions
        {
            ApiKey = "my-api-key",
            BaseUrl = "https://custom.api.com",
            WebhookSecret = "my-webhook-secret",
            PixExpiresInSeconds = 7200
        };

        // Act & Assert
        Assert.Equal("my-api-key", options.ApiKey);
        Assert.Equal("https://custom.api.com", options.BaseUrl);
        Assert.Equal("my-webhook-secret", options.WebhookSecret);
        Assert.Equal(7200, options.PixExpiresInSeconds);
    }

    [Fact]
    public void PixExpiresInSeconds_CanBeSetToDifferentValues()
    {
        // Arrange
        var options = new AbacatePayOptions();

        // Act
        options.PixExpiresInSeconds = 1800;

        // Assert
        Assert.Equal(1800, options.PixExpiresInSeconds);
    }

    [Fact]
    public void BaseUrl_CanBeCustomized()
    {
        // Arrange
        var options = new AbacatePayOptions();

        // Act
        options.BaseUrl = "https://sandbox.api.com/v2";

        // Assert
        Assert.Equal("https://sandbox.api.com/v2", options.BaseUrl);
    }
}
