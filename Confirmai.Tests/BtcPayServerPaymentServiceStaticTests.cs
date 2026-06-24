using Confirmai.Services.Payment;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Confirmai.Tests;

public class BtcPayServerPaymentServiceStaticTests
{
    [Fact]
    public void Name_ReturnsBTCPayServer()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var configMock = new Mock<IConfiguration>();
        
        configMock.Setup(c => c["BtcPay:ApiKey"]).Returns("test-key");
        configMock.Setup(c => c["BtcPay:StoreId"]).Returns("test-store");
        configMock.Setup(c => c["BtcPay:Url"]).Returns("https://test.btcpay.com");
        
        var service = new BtcPayServerPaymentService(httpFactoryMock.Object, configMock.Object);

        // Act
        var name = service.Name;

        // Assert
        Assert.Equal("BTCPayServer", name);
    }

    [Fact]
    public async Task GenerateAddressWithKeyAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var configMock = new Mock<IConfiguration>();
        
        configMock.Setup(c => c["BtcPay:ApiKey"]).Returns("test-key");
        configMock.Setup(c => c["BtcPay:StoreId"]).Returns("test-store");
        configMock.Setup(c => c["BtcPay:Url"]).Returns("https://test.btcpay.com");
        
        var service = new BtcPayServerPaymentService(httpFactoryMock.Object, configMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() => 
            service.GenerateAddressWithKeyAsync(0.1m));
    }
}
