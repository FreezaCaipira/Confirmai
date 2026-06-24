using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class EventCollisionInfoTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var eventId = 1;
        var groupId = 100;
        var startsAt = DateTime.UtcNow;

        // Act
        var info = new EventCollisionService.EventCollisionInfo
        {
            EventId = eventId,
            GroupId = groupId,
            StartsAt = startsAt
        };

        // Assert
        Assert.Equal(eventId, info.EventId);
        Assert.Equal(groupId, info.GroupId);
        Assert.Equal(startsAt, info.StartsAt);
    }

    [Fact]
    public void Constructor_WithDefaultValues()
    {
        // Act
        var info = new EventCollisionService.EventCollisionInfo();

        // Assert
        Assert.Equal(0, info.EventId);
        Assert.Equal(0, info.GroupId);
        Assert.Equal(default(DateTime), info.StartsAt);
    }

    [Fact]
    public void Constructor_WithLargeValues()
    {
        // Arrange
        var eventId = 999999;
        var groupId = 888888;
        var startsAt = DateTime.UtcNow.AddYears(10);

        // Act
        var info = new EventCollisionService.EventCollisionInfo
        {
            EventId = eventId,
            GroupId = groupId,
            StartsAt = startsAt
        };

        // Assert
        Assert.Equal(999999, info.EventId);
        Assert.Equal(888888, info.GroupId);
        Assert.Equal(startsAt, info.StartsAt);
    }
}
