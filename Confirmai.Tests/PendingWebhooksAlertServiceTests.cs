using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Confirmai.Tests;

public class PendingWebhooksAlertServiceTests
{
    private readonly Mock<ILogger<PendingWebhooksAlertService>> _loggerMock;

    public PendingWebhooksAlertServiceTests()
    {
        _loggerMock = new Mock<ILogger<PendingWebhooksAlertService>>();
    }

    [Fact]
    public async Task StartAsync_LogsStartMessage_InitializesTimer()
    {
        // Arrange
        var scopeFactoryMock = CreateScopeFactoryMock();
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PendingWebhooksAlertService iniciado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_LogsStopMessage_DisposesTimer()
    {
        // Arrange
        var scopeFactoryMock = CreateScopeFactoryMock();
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PendingWebhooksAlertService parado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckPendingWebhooksAsync_WhenAboveThreshold_LogsWarning()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        
        // Add 6 pending confirmations (> threshold of 5)
        for (int i = 0; i < 6; i++)
        {
            var eventEntity = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48 + i).ToString("yyyy-MM-dd HH:mm"), 50m);
            db.Events.Add(eventEntity);
            
            var confirmation = TestDataFactory.CreateEventConfirmation(eventEntity, user);
            confirmation.HasPaid = false;
            confirmation.PaymentStatus = EventConfirmationPaymentStatus.Pending;
            confirmation.ConfirmedAt = DateTime.UtcNow.AddHours(-25 - i);
            db.EventConfirmations.Add(confirmation);
        }
        
        await db.SaveChangesAsync();

        var scopeFactoryMock = CreateScopeFactoryMock(db);
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckPendingWebhooksAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ALERTA") && v.ToString()!.Contains("6")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckPendingWebhooksAsync_WhenBelowThreshold_LogsInfo()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        
        // Add 2 pending confirmations (< threshold of 5)
        for (int i = 0; i < 2; i++)
        {
            var eventEntity = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48 + i).ToString("yyyy-MM-dd HH:mm"), 50m);
            db.Events.Add(eventEntity);
            
            var confirmation = TestDataFactory.CreateEventConfirmation(eventEntity, user);
            confirmation.HasPaid = false;
            confirmation.PaymentStatus = EventConfirmationPaymentStatus.Pending;
            confirmation.ConfirmedAt = DateTime.UtcNow.AddHours(-25 - i);
            db.EventConfirmations.Add(confirmation);
        }
        
        await db.SaveChangesAsync();

        var scopeFactoryMock = CreateScopeFactoryMock(db);
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckPendingWebhooksAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("2") && v.ToString()!.Contains("pendentes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckPendingWebhooksAsync_WhenNone_LogsDebug()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        await db.SaveChangesAsync();

        var scopeFactoryMock = CreateScopeFactoryMock(db);
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckPendingWebhooksAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nenhum webhook Pix pendente")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckPendingWebhooksAsync_ExcludesRecentPendingConfirmations()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        
        // Add 1 old (>24h) and 1 recent (<24h)
        var oldEvent = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(oldEvent);
        var oldConfirmation = TestDataFactory.CreateEventConfirmation(oldEvent, user);
        oldConfirmation.HasPaid = false;
        oldConfirmation.PaymentStatus = EventConfirmationPaymentStatus.Pending;
        oldConfirmation.ConfirmedAt = DateTime.UtcNow.AddHours(-25);
        db.EventConfirmations.Add(oldConfirmation);
        
        var recentEvent = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(50).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(recentEvent);
        var recentConfirmation = TestDataFactory.CreateEventConfirmation(recentEvent, user);
        recentConfirmation.HasPaid = false;
        recentConfirmation.PaymentStatus = EventConfirmationPaymentStatus.Pending;
        recentConfirmation.ConfirmedAt = DateTime.UtcNow.AddHours(-23);
        db.EventConfirmations.Add(recentConfirmation);
        
        await db.SaveChangesAsync();

        var scopeFactoryMock = CreateScopeFactoryMock(db);
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckPendingWebhooksAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert - Should log info for 1 pending (not warning)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1") && v.ToString()!.Contains("pendentes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckPendingWebhooksAsync_ExcludesPastEvents()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var group = TestDataFactory.CreateGroup("Test Group");
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        
        // Add 1 past event and 1 future event
        var pastEvent = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(pastEvent);
        var pastConfirmation = TestDataFactory.CreateEventConfirmation(pastEvent, user);
        pastConfirmation.HasPaid = false;
        pastConfirmation.PaymentStatus = EventConfirmationPaymentStatus.Pending;
        pastConfirmation.ConfirmedAt = DateTime.UtcNow.AddHours(-25);
        db.EventConfirmations.Add(pastConfirmation);
        
        var futureEvent = TestDataFactory.CreateEvent(group, DateTime.UtcNow.AddHours(48).ToString("yyyy-MM-dd HH:mm"), 50m);
        db.Events.Add(futureEvent);
        var futureConfirmation = TestDataFactory.CreateEventConfirmation(futureEvent, user);
        futureConfirmation.HasPaid = false;
        futureConfirmation.PaymentStatus = EventConfirmationPaymentStatus.Pending;
        futureConfirmation.ConfirmedAt = DateTime.UtcNow.AddHours(-25);
        db.EventConfirmations.Add(futureConfirmation);
        
        await db.SaveChangesAsync();

        var scopeFactoryMock = CreateScopeFactoryMock(db);
        var service = new PendingWebhooksAlertService(scopeFactoryMock.Object, _loggerMock.Object);

        // Act
        var method = service.GetType().GetMethod("CheckPendingWebhooksAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (method?.Invoke(service, null) as Task ?? Task.CompletedTask);

        // Assert - Should log info for 1 pending (not warning)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1") && v.ToString()!.Contains("pendentes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static Mock<IServiceScopeFactory> CreateScopeFactoryMock(AppDbContext? db = null)
    {
        var scopeFactoryMock = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var scopeMock = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        
        if (db != null)
        {
            serviceProviderMock.Setup(p => p.GetService(typeof(AppDbContext))).Returns(db);
        }

        return scopeFactoryMock;
    }
}
