using Confirmai.Models;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogFilteringTests
{
    [Fact]
    public void Apply_WithEmptyCriteria_ReturnsAllLogs()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", UserId = "user1" },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", UserId = "user2" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria();

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithGlobalTerm_FiltersByUserIdSourceOrMessage()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Error occurred", Source = "Payment", UserId = "user1" },
            new AppLog { Id = 2, Message = "Success", Source = "Payment", UserId = "user2" },
            new AppLog { Id = 3, Message = "Test", Source = "Test", UserId = "user1" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { GlobalTerm = "user1" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithUserId_FiltersByUserId()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", UserId = "user1" },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", UserId = "user2" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { UserId = "user1" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Equal("user1", result.First().UserId);
    }

    [Fact]
    public void Apply_WithSource_FiltersBySource()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Payment" },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { Source = "Payment" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Equal("Payment", result.First().Source);
    }

    [Fact]
    public void Apply_WithMessage_FiltersByMessage()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Error occurred", Source = "Test" },
            new AppLog { Id = 2, Message = "Success", Source = "Test" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { Message = "Error" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Contains("Error", result.First().Message);
    }

    [Fact]
    public void Apply_WithLevel_FiltersByLevel()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", Level = "Error" },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", Level = "Info" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { Level = "Error" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Equal("Error", result.First().Level);
    }

    [Fact]
    public void Apply_WithStartDate_FiltersByStartDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", Timestamp = now.AddDays(-2) },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", Timestamp = now.AddDays(-1) },
            new AppLog { Id = 3, Message = "Test 3", Source = "Test", Timestamp = now }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { StartDate = now.AddDays(-1) };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithEndDate_FiltersByEndDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", Timestamp = now.AddDays(-2) },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", Timestamp = now.AddDays(-1) },
            new AppLog { Id = 3, Message = "Test 3", Source = "Test", Timestamp = now }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { EndDate = now.AddDays(-1) };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithEventType_FiltersByEventType()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", EventType = "Payment.Created" },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", EventType = "User.Created" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { EventType = "Payment" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Contains("Payment", result.First().EventType);
    }

    [Fact]
    public void Apply_WithEntityType_FiltersByEntityType()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Test 1", Source = "Test", EntityType = "Product" },
            new AppLog { Id = 2, Message = "Test 2", Source = "Test", EntityType = "User" }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria { EntityType = "Product" };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Equal("Product", result.First().EntityType);
    }

    [Fact]
    public void Apply_WithMultipleCriteria_AppliesAllFilters()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<AppLog>
        {
            new AppLog { Id = 1, Message = "Error occurred", Source = "Payment", Level = "Error", Timestamp = now.AddDays(-2) },
            new AppLog { Id = 2, Message = "Error occurred", Source = "Payment", Level = "Error", Timestamp = now.AddDays(-1) },
            new AppLog { Id = 3, Message = "Success", Source = "Payment", Level = "Info", Timestamp = now }
        }.AsQueryable();

        var criteria = new AdminLogFilterCriteria
        {
            Message = "Error",
            Source = "Payment",
            Level = "Error",
            StartDate = now.AddDays(-1)
        };

        // Act
        var result = AdminLogFiltering.Apply(logs, criteria);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result.First().Id);
    }
}
