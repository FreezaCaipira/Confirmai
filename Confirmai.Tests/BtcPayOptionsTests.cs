using Confirmai.Config;

namespace Confirmai.Tests;

public class BtcPayOptionsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new BtcPayOptions();

        // Assert
        Assert.Equal(string.Empty, options.WebhookUrlLocal);
        Assert.Equal(string.Empty, options.WebhookUrlProd);
    }

    [Fact]
    public void CanSetWebhookUrlLocal()
    {
        // Arrange
        var options = new BtcPayOptions();

        // Act
        options.WebhookUrlLocal = "https://localhost:5000/api/btcpay/webhook";

        // Assert
        Assert.Equal("https://localhost:5000/api/btcpay/webhook", options.WebhookUrlLocal);
    }

    [Fact]
    public void CanSetWebhookUrlProd()
    {
        // Arrange
        var options = new BtcPayOptions();

        // Act
        options.WebhookUrlProd = "https://app.confirmai.com.br/api/btcpay/webhook";

        // Assert
        Assert.Equal("https://app.confirmai.com.br/api/btcpay/webhook", options.WebhookUrlProd);
    }

    [Fact]
    public void CanSetBothUrls()
    {
        // Arrange
        var options = new BtcPayOptions
        {
            WebhookUrlLocal = "https://localhost:5000/api/btcpay/webhook",
            WebhookUrlProd = "https://app.confirmai.com.br/api/btcpay/webhook"
        };

        // Act & Assert
        Assert.Equal("https://localhost:5000/api/btcpay/webhook", options.WebhookUrlLocal);
        Assert.Equal("https://app.confirmai.com.br/api/btcpay/webhook", options.WebhookUrlProd);
    }

    [Fact]
    public void WebhookUrlLocal_CanBeSetToEmpty()
    {
        // Arrange
        var options = new BtcPayOptions { WebhookUrlLocal = "https://example.com" };

        // Act
        options.WebhookUrlLocal = string.Empty;

        // Assert
        Assert.Equal(string.Empty, options.WebhookUrlLocal);
    }

    [Fact]
    public void WebhookUrlProd_CanBeSetToEmpty()
    {
        // Arrange
        var options = new BtcPayOptions { WebhookUrlProd = "https://example.com" };

        // Act
        options.WebhookUrlProd = string.Empty;

        // Assert
        Assert.Equal(string.Empty, options.WebhookUrlProd);
    }
}
