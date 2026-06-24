using Confirmai.Configuration;
using Confirmai.Services.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Confirmai.Tests;

public class AbacatePayPixServiceTests
{
    [Fact]
    public void Name_ReturnsPix()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = "test-key" });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var name = service.Name;

        // Assert
        Assert.Equal("Pix", name);
    }

    [Fact]
    public void IsEnabled_WhenEnabled_ReturnsTrue()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = "test-key" });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var isEnabled = service.IsEnabled;

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = null });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var isEnabled = service.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public async Task GenerateAddressAsync_WhenDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = null });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.GenerateAddressAsync(100m));
    }

    [Fact]
    public async Task GetReceivedAmountAsync_WhenDisabled_ReturnsZero()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = null });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var amount = await service.GetReceivedAmountAsync("test-charge-id");

        // Assert
        Assert.Equal(0m, amount);
    }

    [Fact]
    public async Task GenerateAddressWithKeyAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = "test-key" });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            service.GenerateAddressWithKeyAsync(100m));
    }

    [Fact]
    public async Task GetReceivedAmountAsync_WhenDisabled_ReturnsZeroForAnyChargeId()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = null });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var amount = await service.GetReceivedAmountAsync("any-charge-id");

        // Assert
        Assert.Equal(0m, amount);
    }

    [Fact]
    public async Task GenerateAddressAsync_WhenDisabled_ThrowsWithCorrectMessage()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = null });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.GenerateAddressAsync(100m));
        Assert.Contains("AbacatePay não está configurado", ex.Message);
    }

    [Fact]
    public async Task GenerateAddressWithKeyAsync_ThrowsWithCorrectMessage()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = "test-key" });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => 
            service.GenerateAddressWithKeyAsync(100m));
        Assert.Contains("Pix não suporta geração de endereço com chave privada", ex.Message);
    }

    [Fact]
    public void IsEnabled_WhenApiKeyIsWhitespace_ReturnsFalse()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = "   " });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var isEnabled = service.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnabled_WhenApiKeyIsEmptyString_ReturnsFalse()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var optionsMock = new Mock<IOptions<AbacatePayOptions>>();
        var loggerMock = new Mock<ILogger<AbacatePayPixService>>();
        
        optionsMock.Setup(x => x.Value).Returns(new AbacatePayOptions { ApiKey = "" });
        
        var service = new AbacatePayPixService(httpFactoryMock.Object, optionsMock.Object, loggerMock.Object);

        // Act
        var isEnabled = service.IsEnabled;

        // Assert
        Assert.False(isEnabled);
    }
}
