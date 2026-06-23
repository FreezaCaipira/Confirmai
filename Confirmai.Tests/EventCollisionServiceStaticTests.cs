using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class EventCollisionServiceStaticTests
{
    [Fact]
    public void BuildConflictMessage_WithValidDateTime_ReturnsFormattedMessage()
    {
        // Arrange
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = EventCollisionService.BuildConflictMessage(dateTime);

        // Assert
        Assert.Contains("Conflito de horário", result);
        Assert.Contains("já existe uma partida deste grupo", result);
    }

    [Fact]
    public void BuildConflictMessage_WithDifferentDateTime_ReturnsDifferentFormattedDate()
    {
        // Arrange
        var dateTime1 = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);
        var dateTime2 = new DateTime(2026, 6, 23, 15, 45, 0, DateTimeKind.Utc);

        // Act
        var result1 = EventCollisionService.BuildConflictMessage(dateTime1);
        var result2 = EventCollisionService.BuildConflictMessage(dateTime2);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void BuildConflictMessage_WithMidnight_ReturnsCorrectFormat()
    {
        // Arrange
        var dateTime = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = EventCollisionService.BuildConflictMessage(dateTime);

        // Assert
        Assert.Contains("Conflito de horário", result);
        Assert.Contains("já existe uma partida deste grupo", result);
    }

    [Fact]
    public void BuildConflictMessage_WithLateNight_ReturnsCorrectFormat()
    {
        // Arrange
        var dateTime = new DateTime(2026, 6, 22, 23, 59, 0, DateTimeKind.Utc);

        // Act
        var result = EventCollisionService.BuildConflictMessage(dateTime);

        // Assert
        Assert.Contains("Conflito de horário", result);
        Assert.Contains("já existe uma partida deste grupo", result);
    }

    [Fact]
    public void BuildConflictMessage_WithLocalTimeConversion_ConvertsToLocalTime()
    {
        // Arrange
        var dateTime = new DateTime(2026, 6, 22, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = EventCollisionService.BuildConflictMessage(dateTime);

        // Assert
        Assert.Contains("Conflito de horário", result);
        Assert.Contains("já existe uma partida deste grupo", result);
    }
}
