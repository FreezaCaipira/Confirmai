using Confirmai.Models;

namespace Confirmai.Tests;

public class ServerApiKeyTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var key = new ServerApiKey { Id = 1 };
        Assert.Equal(1, key.Id);
    }

    [Fact]
    public void ServerId_CanBeSetAndGet()
    {
        var key = new ServerApiKey { ServerId = 5 };
        Assert.Equal(5, key.ServerId);
    }

    [Fact]
    public void KeyHash_CanBeSetAndGet()
    {
        var key = new ServerApiKey { KeyHash = "hash123" };
        Assert.Equal("hash123", key.KeyHash);
    }

    [Fact]
    public void KeyHash_DefaultsToEmptyString()
    {
        var key = new ServerApiKey();
        Assert.Equal(string.Empty, key.KeyHash);
    }

    [Fact]
    public void KeyPrefix_CanBeSetAndGet()
    {
        var key = new ServerApiKey { KeyPrefix = "prefix" };
        Assert.Equal("prefix", key.KeyPrefix);
    }

    [Fact]
    public void KeyPrefix_DefaultsToEmptyString()
    {
        var key = new ServerApiKey();
        Assert.Equal(string.Empty, key.KeyPrefix);
    }

    [Fact]
    public void Label_CanBeSetAndGet()
    {
        var key = new ServerApiKey { Label = "Test Key" };
        Assert.Equal("Test Key", key.Label);
    }

    [Fact]
    public void Label_CanBeNull()
    {
        var key = new ServerApiKey { Label = null };
        Assert.Null(key.Label);
    }

    [Fact]
    public void IsActive_CanBeSetAndGet()
    {
        var key = new ServerApiKey { IsActive = true };
        Assert.True(key.IsActive);
    }

    [Fact]
    public void IsActive_DefaultsToFalse()
    {
        var key = new ServerApiKey();
        Assert.False(key.IsActive);
    }

    [Fact]
    public void CreatedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var key = new ServerApiKey { CreatedAt = now };
        Assert.Equal(now, key.CreatedAt);
    }

    [Fact]
    public void LastUsedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var key = new ServerApiKey { LastUsedAt = now };
        Assert.Equal(now, key.LastUsedAt);
    }

    [Fact]
    public void LastUsedAt_CanBeNull()
    {
        var key = new ServerApiKey { LastUsedAt = null };
        Assert.Null(key.LastUsedAt);
    }

    [Fact]
    public void RevokedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var key = new ServerApiKey { RevokedAt = now };
        Assert.Equal(now, key.RevokedAt);
    }

    [Fact]
    public void RevokedAt_CanBeNull()
    {
        var key = new ServerApiKey { RevokedAt = null };
        Assert.Null(key.RevokedAt);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var now = DateTime.UtcNow;
        var key = new ServerApiKey
        {
            Id = 1,
            ServerId = 10,
            KeyHash = "hash",
            KeyPrefix = "pref",
            Label = "Label",
            IsActive = true,
            CreatedAt = now,
            LastUsedAt = now,
            RevokedAt = now
        };
        
        Assert.Equal(1, key.Id);
        Assert.Equal(10, key.ServerId);
        Assert.Equal("hash", key.KeyHash);
        Assert.Equal("pref", key.KeyPrefix);
        Assert.Equal("Label", key.Label);
        Assert.True(key.IsActive);
        Assert.Equal(now, key.CreatedAt);
        Assert.Equal(now, key.LastUsedAt);
        Assert.Equal(now, key.RevokedAt);
    }
}
