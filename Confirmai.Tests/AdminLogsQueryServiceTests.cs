using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Confirmai.Tests;

public class AdminLogsQueryServiceTests
{
    private static Mock<IDbContextFactory<AppDbContext>> CreateFactoryMock(AppDbContext db)
    {
        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(db);
        factoryMock.Setup(f => f.CreateDbContext()).Returns(db);
        return factoryMock;
    }

    [Fact]
    public async Task GetPageDataAsync_WhenNoLogs_ReturnsEmptyPage()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 1, 10);

        // Assert
        Assert.Equal(0, result.TotalLogs);
        Assert.Equal(1, result.EffectivePage);
        Assert.Empty(result.Logs);
        Assert.Equal(0, result.AuditCounts.All);
    }

    [Fact]
    public async Task GetPageDataAsync_WithLogs_ReturnsPagedResults()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);
        
        for (int i = 0; i < 25; i++)
        {
            db.Logs.Add(new AppLog
            {
                Level = "Information",
                Message = $"Log {i}",
                Source = "Test",
                Timestamp = DateTime.UtcNow.AddHours(-i),
                UserId = user.Id
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 1, 10);

        // Assert
        Assert.Equal(25, result.TotalLogs);
        Assert.Equal(1, result.EffectivePage);
        Assert.Equal(10, result.Logs.Count);
        Assert.Equal(25, result.AuditCounts.All);
    }

    [Fact]
    public async Task GetPageDataAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);
        
        for (int i = 0; i < 25; i++)
        {
            db.Logs.Add(new AppLog
            {
                Level = "Information",
                Message = $"Log {i}",
                Source = "Test",
                Timestamp = DateTime.UtcNow.AddHours(-i),
                UserId = user.Id
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act - Request page 2
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 2, 10);

        // Assert
        Assert.Equal(25, result.TotalLogs);
        Assert.Equal(2, result.EffectivePage);
        Assert.Equal(10, result.Logs.Count);
    }

    [Fact]
    public async Task GetPageDataAsync_CalculatesAuditCountsBySource()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);
        
        db.Logs.Add(new AppLog
        {
            Level = "Information",
            Message = "Security policy change",
            Source = AdminAuditSources.SecurityPolicy,
            Timestamp = DateTime.UtcNow,
            UserId = user.Id
        });
        
        db.Logs.Add(new AppLog
        {
            Level = "Warning",
            Message = "Webhook warning",
            Source = AdminAuditSources.Webhook,
            Timestamp = DateTime.UtcNow,
            UserId = user.Id
        });
        
        db.Logs.Add(new AppLog
        {
            Level = "Information",
            Message = "Server integration",
            Source = AdminAuditSources.ServerIntegration,
            Timestamp = DateTime.UtcNow,
            UserId = user.Id
        });
        
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 1, 10);

        // Assert
        Assert.Equal(3, result.AuditCounts.All);
        Assert.Equal(1, result.AuditCounts.SecurityPolicy);
        Assert.Equal(1, result.AuditCounts.WebhookWarnings);
        Assert.Equal(1, result.AuditCounts.ServerIntegration);
    }

    [Fact]
    public async Task GetEntityTimelineAsync_ReturnsLogsForEntity()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);
        
        db.Logs.Add(new AppLog
        {
            Level = "Information",
            Message = "Entity event 1",
            Source = "Test",
            Timestamp = DateTime.UtcNow.AddHours(-2),
            UserId = user.Id,
            EntityType = "Event",
            EntityId = "123"
        });
        
        db.Logs.Add(new AppLog
        {
            Level = "Information",
            Message = "Entity event 2",
            Source = "Test",
            Timestamp = DateTime.UtcNow.AddHours(-1),
            UserId = user.Id,
            EntityType = "Event",
            EntityId = "123"
        });
        
        db.Logs.Add(new AppLog
        {
            Level = "Information",
            Message = "Other entity",
            Source = "Test",
            Timestamp = DateTime.UtcNow,
            UserId = user.Id,
            EntityType = "Event",
            EntityId = "456"
        });
        
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);

        // Act
        var result = await service.GetEntityTimelineAsync("Event", "123");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, log => Assert.Equal("123", log.EntityId));
    }

    [Fact]
    public async Task GetEntityTimelineAsync_ReturnsLogsInChronologicalOrder()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);
        
        var times = new[] { DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-2) };
        foreach (var time in times)
        {
            db.Logs.Add(new AppLog
            {
                Level = "Information",
                Message = $"Event at {time}",
                Source = "Test",
                Timestamp = time,
                UserId = user.Id,
                EntityType = "Event",
                EntityId = "123"
            });
        }
        
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);

        // Act
        var result = await service.GetEntityTimelineAsync("Event", "123");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].Timestamp < result[1].Timestamp);
        Assert.True(result[1].Timestamp < result[2].Timestamp);
    }

    [Fact]
    public async Task GetEntityTimelineAsync_WhenNoLogs_ReturnsEmptyList()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);

        // Act
        var result = await service.GetEntityTimelineAsync("Event", "999");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPageDataAsync_ClampsPageToValidRange_WhenRequestedPageExceedsTotal()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);

        for (int i = 0; i < 5; i++)
        {
            db.Logs.Add(new AppLog
            {
                Level = "Information",
                Message = $"Log {i}",
                Source = "Test",
                Timestamp = DateTime.UtcNow.AddHours(-i),
                UserId = user.Id
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act - Request page 10 when only 5 logs exist
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 10, 10);

        // Assert
        Assert.Equal(5, result.TotalLogs);
        Assert.Equal(1, result.EffectivePage); // Clamped to last page
    }

    [Fact]
    public async Task GetPageDataAsync_ClampsPageToOne_WhenRequestedPageIsZero()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);

        for (int i = 0; i < 5; i++)
        {
            db.Logs.Add(new AppLog
            {
                Level = "Information",
                Message = $"Log {i}",
                Source = "Test",
                Timestamp = DateTime.UtcNow.AddHours(-i),
                UserId = user.Id
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act - Request page 0
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 0, 10);

        // Assert
        Assert.Equal(5, result.TotalLogs);
        Assert.Equal(1, result.EffectivePage); // Clamped to first page
    }

    [Fact]
    public async Task GetPageDataAsync_UsesMinimumPageSizeOne_WhenRequestedPageSizeIsZero()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);

        for (int i = 0; i < 5; i++)
        {
            db.Logs.Add(new AppLog
            {
                Level = "Information",
                Message = $"Log {i}",
                Source = "Test",
                Timestamp = DateTime.UtcNow.AddHours(-i),
                UserId = user.Id
            });
        }
        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act - Request page size 0
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 1, 0);

        // Assert
        Assert.Equal(5, result.TotalLogs);
        Assert.Equal(1, result.Logs.Count); // Minimum page size of 1
    }

    [Fact]
    public async Task GetPageDataAsync_CalculatesPaymentPanelStaleCount()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);

        db.Logs.Add(new AppLog
        {
            Level = "Warning",
            Message = "Payment panel stale",
            Source = "Payments",
            Timestamp = DateTime.UtcNow,
            UserId = user.Id,
            EventType = AuditEvents.PaymentReconciliationPanelStale
        });

        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);
        var criteria = new AdminLogFilterCriteria();
        var auditCriteria = new AdminLogFilterCriteria();

        // Act
        var result = await service.GetPageDataAsync(
            criteria, auditCriteria, AdminLogSortColumn.Timestamp, true, 1, 10);

        // Assert
        Assert.Equal(1, result.AuditCounts.PaymentPanelStale);
    }

    [Fact]
    public async Task GetEntityTimelineAsync_IncludesUserInResults()
    {
        // Arrange
        var db = TestDataFactory.CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test", FullName = "Test User" };
        db.Users.Add(user);

        db.Logs.Add(new AppLog
        {
            Level = "Information",
            Message = "Entity event",
            Source = "Test",
            Timestamp = DateTime.UtcNow,
            UserId = user.Id,
            EntityType = "Event",
            EntityId = "123"
        });

        await db.SaveChangesAsync();

        var factoryMock = CreateFactoryMock(db);
        var service = new AdminLogsQueryService(factoryMock.Object);

        // Act
        var result = await service.GetEntityTimelineAsync("Event", "123");

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].User);
        Assert.Equal("user-1", result[0].User.Id);
    }
}
