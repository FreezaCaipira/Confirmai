using Confirmai.Services.Events;

namespace Confirmai.Tests;

public class EventNotificationSchedulerServiceStaticTests
{
    [Fact]
    public void GetNextNotificationTime_ReturnsDateTimeAt8AM()
    {
        // Act
        var method = typeof(EventNotificationSchedulerService).GetMethod("GetNextNotificationTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (DateTime?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Value.Hour);
        Assert.Equal(0, result.Value.Minute);
        Assert.Equal(0, result.Value.Second);
    }

    [Fact]
    public void GetNextNotificationTime_ReturnsFutureDateTime()
    {
        // Act
        var method = typeof(EventNotificationSchedulerService).GetMethod("GetNextNotificationTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (DateTime?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value > DateTime.UtcNow);
    }

    [Fact]
    public void GetNextNotificationTime_ReturnsUtcDateTime()
    {
        // Act
        var method = typeof(EventNotificationSchedulerService).GetMethod("GetNextNotificationTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (DateTime?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
    }
}
