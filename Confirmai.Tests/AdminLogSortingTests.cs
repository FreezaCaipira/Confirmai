using Confirmai.Models;
using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogSortingTests
{
    [Fact]
    public void Apply_WithTimestampAscending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, Timestamp = DateTime.UtcNow.AddHours(-2) },
            new() { Id = 2, Timestamp = DateTime.UtcNow.AddHours(-1) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Timestamp, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithTimestampDescending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, Timestamp = DateTime.UtcNow.AddHours(-2) },
            new() { Id = 2, Timestamp = DateTime.UtcNow.AddHours(-1) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Timestamp, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithLevelAscending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, Level = "Info", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, Level = "Warning", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Level, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithLevelDescending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, Level = "Info", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, Level = "Warning", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Level, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithSourceAscending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, Source = "SourceA", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, Source = "SourceB", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Source, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithSourceDescending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, Source = "SourceA", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, Source = "SourceB", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Source, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithUserAscending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, UserId = "user1", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, UserId = "user2", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.User, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithUserDescending_ReturnsOrderedQueryable()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, UserId = "user1", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, UserId = "user2", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.User, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Apply_WithEmptyList_ReturnsEmptyQueryable()
    {
        // Arrange
        var logs = new List<AppLog>().AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.Timestamp, true);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Apply_WithNullUserId_UsesEmptyString()
    {
        // Arrange
        var logs = new List<AppLog>
        {
            new() { Id = 1, UserId = null, Timestamp = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, UserId = "user1", Timestamp = DateTime.UtcNow.AddHours(-2) }
        }.AsQueryable();

        // Act
        var result = AdminLogSorting.Apply(logs, AdminLogSortColumn.User, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
}
