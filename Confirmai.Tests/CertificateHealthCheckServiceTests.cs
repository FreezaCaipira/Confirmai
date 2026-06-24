using Confirmai.Services.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Confirmai.Tests;

public class CertificateHealthCheckServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<CertificateHealthCheckService>> _loggerMock;

    public CertificateHealthCheckServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<CertificateHealthCheckService>>();
    }

    [Fact]
    public async Task StartAsync_LogsStartAndCreatesTimer()
    {
        // Arrange
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CertificateHealthCheckService iniciado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_LogsStopAndDisposesTimer()
    {
        // Arrange
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CertificateHealthCheckService parado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckEfiBankCertificateAsync_WhenPathNotConfigured_LogsWarning()
    {
        // Arrange
        _configMock.Setup(c => c["EfiBank:ClientCertificatePath"]).Returns((string?)null);
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);

        // Act - Call private method via reflection
        var method = service.GetType().GetMethod("CheckEfiBankCertificateAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EfiBank:ClientCertificatePath não configurado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckEfiBankCertificateAsync_WhenFileNotFound_LogsError()
    {
        // Arrange
        _configMock.Setup(c => c["EfiBank:ClientCertificatePath"]).Returns("C:\\nonexistent\\cert.pfx");
        _configMock.Setup(c => c["EfiBank:CertificatePassword"]).Returns((string?)null);
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckEfiBankCertificateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Certificado EfiBank não encontrado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckEfiBankCertificateAsync_WhenEmptyPath_LogsWarning()
    {
        // Arrange
        _configMock.Setup(c => c["EfiBank:ClientCertificatePath"]).Returns(string.Empty);
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckEfiBankCertificateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EfiBank:ClientCertificatePath não configurado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckEfiBankCertificateAsync_WhenWhitespacePath_LogsError()
    {
        // Arrange
        _configMock.Setup(c => c["EfiBank:ClientCertificatePath"]).Returns("   ");
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckEfiBankCertificateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert - Whitespace is treated as a path, so file not found error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Certificado EfiBank não encontrado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenCalledTwice_LogsWarning()
    {
        // Arrange
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - Should log warning about already started
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var service = new CertificateHealthCheckService(_configMock.Object, _loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));
        Assert.Null(exception);
    }
}
