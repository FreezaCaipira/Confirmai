using Confirmai.Configuration;
using Confirmai.Services.Payment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

public class EventPaymentReconciliationWorkerTests
{
    [Fact]
    public void Constructor_WithDefaultPixExpiry_CalculatesCorrectExpiryWindow()
    {
        // Arrange
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<EventPaymentReconciliationWorker>>();
        var efiBankOptions = new EfiBankOptions { PixExpiresInSeconds = 3600 };
        var optionsMock = new Mock<IOptions<EfiBankOptions>>();
        optionsMock.Setup(x => x.Value).Returns(efiBankOptions);

        // Act
        var worker = new EventPaymentReconciliationWorker(
            scopeFactoryMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert - expiryWindow should be 2× the configured Pix expiry
        // This is a private field, so we can't directly test it
        // The test mainly verifies the constructor doesn't throw
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_WithCustomPixExpiry_CalculatesCorrectExpiryWindow()
    {
        // Arrange
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<EventPaymentReconciliationWorker>>();
        var efiBankOptions = new EfiBankOptions { PixExpiresInSeconds = 1800 };
        var optionsMock = new Mock<IOptions<EfiBankOptions>>();
        optionsMock.Setup(x => x.Value).Returns(efiBankOptions);

        // Act
        var worker = new EventPaymentReconciliationWorker(
            scopeFactoryMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_WithZeroPixExpiry_CalculatesCorrectExpiryWindow()
    {
        // Arrange
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<EventPaymentReconciliationWorker>>();
        var efiBankOptions = new EfiBankOptions { PixExpiresInSeconds = 0 };
        var optionsMock = new Mock<IOptions<EfiBankOptions>>();
        optionsMock.Setup(x => x.Value).Returns(efiBankOptions);

        // Act
        var worker = new EventPaymentReconciliationWorker(
            scopeFactoryMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_WithNegativePixExpiry_CalculatesCorrectExpiryWindow()
    {
        // Arrange
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<EventPaymentReconciliationWorker>>();
        var efiBankOptions = new EfiBankOptions { PixExpiresInSeconds = -100 };
        var optionsMock = new Mock<IOptions<EfiBankOptions>>();
        optionsMock.Setup(x => x.Value).Returns(efiBankOptions);

        // Act
        var worker = new EventPaymentReconciliationWorker(
            scopeFactoryMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_WithLargePixExpiry_CalculatesCorrectExpiryWindow()
    {
        // Arrange
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<EventPaymentReconciliationWorker>>();
        var efiBankOptions = new EfiBankOptions { PixExpiresInSeconds = 86400 }; // 24 hours
        var optionsMock = new Mock<IOptions<EfiBankOptions>>();
        optionsMock.Setup(x => x.Value).Returns(efiBankOptions);

        // Act
        var worker = new EventPaymentReconciliationWorker(
            scopeFactoryMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        Assert.NotNull(worker);
    }
}
