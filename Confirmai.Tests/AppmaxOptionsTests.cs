using Confirmai.Configuration;

namespace Confirmai.Tests;

public class AppmaxOptionsTests
{
    [Fact]
    public void Section_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("Appmax", AppmaxOptions.Section);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new AppmaxOptions();

        // Assert
        Assert.Null(options.ApiKey);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
        Assert.Equal("https://api.appmax.com.br", options.BaseUrl);
        Assert.Equal("https://auth.appmax.com.br", options.AuthBaseUrl);
        Assert.False(options.EnableEventCheckout);
    }

    [Fact]
    public void HasStaticToken_WhenApiKeyIsNull_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions { ApiKey = null };

        // Act
        var hasToken = options.HasStaticToken;

        // Assert
        Assert.False(hasToken);
    }

    [Fact]
    public void HasStaticToken_WhenApiKeyIsEmpty_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions { ApiKey = string.Empty };

        // Act
        var hasToken = options.HasStaticToken;

        // Assert
        Assert.False(hasToken);
    }

    [Fact]
    public void HasStaticToken_WhenApiKeyHasValue_ReturnsTrue()
    {
        // Arrange
        var options = new AppmaxOptions { ApiKey = "test-token" };

        // Act
        var hasToken = options.HasStaticToken;

        // Assert
        Assert.True(hasToken);
    }

    [Fact]
    public void HasClientCredentials_WhenBothCredentialsPresent_ReturnsTrue()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };

        // Act
        var hasCredentials = options.HasClientCredentials;

        // Assert
        Assert.True(hasCredentials);
    }

    [Fact]
    public void HasClientCredentials_WhenClientIdMissing_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            ClientId = null,
            ClientSecret = "client-secret"
        };

        // Act
        var hasCredentials = options.HasClientCredentials;

        // Assert
        Assert.False(hasCredentials);
    }

    [Fact]
    public void HasClientCredentials_WhenClientSecretMissing_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            ClientId = "client-id",
            ClientSecret = null
        };

        // Act
        var hasCredentials = options.HasClientCredentials;

        // Assert
        Assert.False(hasCredentials);
    }

    [Fact]
    public void IsEnabled_WhenEventCheckoutDisabled_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            EnableEventCheckout = false,
            ApiKey = "test-token",
            BaseUrl = "https://api.appmax.com.br",
            AuthBaseUrl = "https://auth.appmax.com.br"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenEventCheckoutEnabledWithStaticToken_ReturnsTrue()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            EnableEventCheckout = true,
            ApiKey = "test-token",
            BaseUrl = "https://api.appmax.com.br",
            AuthBaseUrl = "https://auth.appmax.com.br"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenEventCheckoutEnabledWithClientCredentials_ReturnsTrue()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            EnableEventCheckout = true,
            ClientId = "client-id",
            ClientSecret = "client-secret",
            BaseUrl = "https://api.appmax.com.br",
            AuthBaseUrl = "https://auth.appmax.com.br"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenBaseUrlMissing_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            EnableEventCheckout = true,
            ApiKey = "test-token",
            BaseUrl = string.Empty,
            AuthBaseUrl = "https://auth.appmax.com.br"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenAuthBaseUrlMissing_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            EnableEventCheckout = true,
            ApiKey = "test-token",
            BaseUrl = "https://api.appmax.com.br",
            AuthBaseUrl = string.Empty
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenNoCredentials_ReturnsFalse()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            EnableEventCheckout = true,
            BaseUrl = "https://api.appmax.com.br",
            AuthBaseUrl = "https://auth.appmax.com.br"
        };

        // Act
        var isEnabled = options.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange
        var options = new AppmaxOptions
        {
            ApiKey = "my-api-key",
            ClientId = "my-client-id",
            ClientSecret = "my-client-secret",
            BaseUrl = "https://custom.api.com",
            AuthBaseUrl = "https://custom.auth.com",
            EnableEventCheckout = true
        };

        // Act & Assert
        Assert.Equal("my-api-key", options.ApiKey);
        Assert.Equal("my-client-id", options.ClientId);
        Assert.Equal("my-client-secret", options.ClientSecret);
        Assert.Equal("https://custom.api.com", options.BaseUrl);
        Assert.Equal("https://custom.auth.com", options.AuthBaseUrl);
        Assert.True(options.EnableEventCheckout);
    }
}
