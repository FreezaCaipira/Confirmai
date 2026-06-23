using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminLogsQueryOverridesTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var eventType = "test.event";
        var source = "TestSource";
        var level = "Info";
        var entityType = "User";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var overrides = new AdminLogsQueryOverrides(
            eventType,
            source,
            level,
            entityType,
            startDate,
            endDate);

        // Assert
        Assert.Equal(eventType, overrides.EventType);
        Assert.Equal(source, overrides.Source);
        Assert.Equal(level, overrides.Level);
        Assert.Equal(entityType, overrides.EntityType);
        Assert.Equal(startDate, overrides.StartDate);
        Assert.Equal(endDate, overrides.EndDate);
    }

    [Fact]
    public void Constructor_WithNullValues()
    {
        // Act
        var overrides = new AdminLogsQueryOverrides(
            null,
            null,
            null,
            null,
            null,
            null);

        // Assert
        Assert.Null(overrides.EventType);
        Assert.Null(overrides.Source);
        Assert.Null(overrides.Level);
        Assert.Null(overrides.EntityType);
        Assert.Null(overrides.StartDate);
        Assert.Null(overrides.EndDate);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenEventTypeIsSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            "test.event",
            null,
            null,
            null,
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenSourceIsSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            null,
            "TestSource",
            null,
            null,
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenLevelIsSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            null,
            null,
            "Info",
            null,
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenEntityTypeIsSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            null,
            null,
            null,
            "User",
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenStartDateIsSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenEndDateIsSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsFalseWhenAllValuesAreNull()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            null,
            null,
            null,
            null,
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.False(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsFalseWhenEventTypeIsWhitespace()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            "   ",
            null,
            null,
            null,
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.False(hasAny);
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenMultipleValuesAreSet()
    {
        // Arrange
        var overrides = new AdminLogsQueryOverrides(
            "test.event",
            "TestSource",
            "Info",
            null,
            null,
            null);

        // Act
        var hasAny = overrides.HasAny;

        // Assert
        Assert.True(hasAny);
    }
}
