using Confirmai.Services.Payment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Confirmai.Tests;

public class PayoutRetryServiceTests
{
    [Fact]
    public async Task StartAsync_CreatesTimerAndLogsStart()
    {
        var logger = new Mock<ILogger<PayoutRetryService>>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var service = new PayoutRetryService(scopeFactory.Object, logger.Object);

        await service.StartAsync(CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PayoutRetryService iniciado")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_LogsStopAndDisposesTimer()
    {
        var logger = new Mock<ILogger<PayoutRetryService>>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var service = new PayoutRetryService(scopeFactory.Object, logger.Object);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PayoutRetryService parado")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        var logger = new Mock<ILogger<PayoutRetryService>>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var service = new PayoutRetryService(scopeFactory.Object, logger.Object);

        await service.StopAsync(CancellationToken.None);
    }
}
