using Confirmai.Models;

namespace Confirmai.Tests;

public class AppLogTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var log = new AppLog { Id = 1 };
        Assert.Equal(1, log.Id);
    }

    [Fact]
    public void Timestamp_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var log = new AppLog { Timestamp = now };
        Assert.Equal(now, log.Timestamp);
    }

    [Fact]
    public void Timestamp_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var log = new AppLog();
        var after = DateTime.UtcNow;
        Assert.InRange(log.Timestamp, before, after);
    }

    [Fact]
    public void UserId_CanBeSetAndGet()
    {
        var log = new AppLog { UserId = "user-123" };
        Assert.Equal("user-123", log.UserId);
    }

    [Fact]
    public void UserId_CanBeNull()
    {
        var log = new AppLog { UserId = null };
        Assert.Null(log.UserId);
    }

    [Fact]
    public void Level_CanBeSetAndGet()
    {
        var log = new AppLog { Level = "Error" };
        Assert.Equal("Error", log.Level);
    }

    [Fact]
    public void Level_DefaultsToInfo()
    {
        var log = new AppLog();
        Assert.Equal("Info", log.Level);
    }

    [Fact]
    public void Source_CanBeSetAndGet()
    {
        var log = new AppLog { Source = "Payment" };
        Assert.Equal("Payment", log.Source);
    }

    [Fact]
    public void Source_DefaultsToEmptyString()
    {
        var log = new AppLog();
        Assert.Equal("", log.Source);
    }

    [Fact]
    public void Message_CanBeSetAndGet()
    {
        var log = new AppLog { Message = "Test message" };
        Assert.Equal("Test message", log.Message);
    }

    [Fact]
    public void Message_DefaultsToEmptyString()
    {
        var log = new AppLog();
        Assert.Equal("", log.Message);
    }

    [Fact]
    public void Exception_CanBeSetAndGet()
    {
        var log = new AppLog { Exception = "Exception details" };
        Assert.Equal("Exception details", log.Exception);
    }

    [Fact]
    public void Exception_CanBeNull()
    {
        var log = new AppLog { Exception = null };
        Assert.Null(log.Exception);
    }

    [Fact]
    public void EventType_CanBeSetAndGet()
    {
        var log = new AppLog { EventType = "user.registered" };
        Assert.Equal("user.registered", log.EventType);
    }

    [Fact]
    public void EventType_CanBeNull()
    {
        var log = new AppLog { EventType = null };
        Assert.Null(log.EventType);
    }

    [Fact]
    public void EntityType_CanBeSetAndGet()
    {
        var log = new AppLog { EntityType = "User" };
        Assert.Equal("User", log.EntityType);
    }

    [Fact]
    public void EntityType_CanBeNull()
    {
        var log = new AppLog { EntityType = null };
        Assert.Null(log.EntityType);
    }

    [Fact]
    public void EntityId_CanBeSetAndGet()
    {
        var log = new AppLog { EntityId = "123" };
        Assert.Equal("123", log.EntityId);
    }

    [Fact]
    public void EntityId_CanBeNull()
    {
        var log = new AppLog { EntityId = null };
        Assert.Null(log.EntityId);
    }

    [Fact]
    public void IpAddress_CanBeSetAndGet()
    {
        var log = new AppLog { IpAddress = "192.168.1.1" };
        Assert.Equal("192.168.1.1", log.IpAddress);
    }

    [Fact]
    public void IpAddress_CanBeNull()
    {
        var log = new AppLog { IpAddress = null };
        Assert.Null(log.IpAddress);
    }

    [Fact]
    public void CorrelationId_CanBeSetAndGet()
    {
        var log = new AppLog { CorrelationId = "corr-123" };
        Assert.Equal("corr-123", log.CorrelationId);
    }

    [Fact]
    public void CorrelationId_CanBeNull()
    {
        var log = new AppLog { CorrelationId = null };
        Assert.Null(log.CorrelationId);
    }

    [Fact]
    public void MetadataJson_CanBeSetAndGet()
    {
        var log = new AppLog { MetadataJson = "{\"key\":\"value\"}" };
        Assert.Equal("{\"key\":\"value\"}", log.MetadataJson);
    }

    [Fact]
    public void MetadataJson_CanBeNull()
    {
        var log = new AppLog { MetadataJson = null };
        Assert.Null(log.MetadataJson);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var now = DateTime.UtcNow;
        var log = new AppLog
        {
            Id = 1,
            Timestamp = now,
            UserId = "user-123",
            Level = "Error",
            Source = "Payment",
            Message = "Test",
            Exception = "Exception",
            EventType = "user.registered",
            EntityType = "User",
            EntityId = "123",
            IpAddress = "192.168.1.1",
            CorrelationId = "corr-123",
            MetadataJson = "{}"
        };
        
        Assert.Equal(1, log.Id);
        Assert.Equal(now, log.Timestamp);
        Assert.Equal("user-123", log.UserId);
        Assert.Equal("Error", log.Level);
        Assert.Equal("Payment", log.Source);
        Assert.Equal("Test", log.Message);
        Assert.Equal("Exception", log.Exception);
        Assert.Equal("user.registered", log.EventType);
        Assert.Equal("User", log.EntityType);
        Assert.Equal("123", log.EntityId);
        Assert.Equal("192.168.1.1", log.IpAddress);
        Assert.Equal("corr-123", log.CorrelationId);
        Assert.Equal("{}", log.MetadataJson);
    }
}
