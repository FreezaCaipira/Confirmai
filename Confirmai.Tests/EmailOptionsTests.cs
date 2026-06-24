using Confirmai.Configuration;

namespace Confirmai.Tests;

public class EmailOptionsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new EmailOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(string.Empty, options.Host);
        Assert.Equal(587, options.Port);
        Assert.True(options.UseSsl);
        Assert.Equal(string.Empty, options.Username);
        Assert.Equal(string.Empty, options.Password);
        Assert.Equal(string.Empty, options.FromEmail);
        Assert.Equal("Confirmai", options.FromName);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange
        var options = new EmailOptions
        {
            Enabled = true,
            Host = "smtp.example.com",
            Port = 25,
            UseSsl = false,
            Username = "user@example.com",
            Password = "password123",
            FromEmail = "noreply@example.com",
            FromName = "My App"
        };

        // Act & Assert
        Assert.True(options.Enabled);
        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(25, options.Port);
        Assert.False(options.UseSsl);
        Assert.Equal("user@example.com", options.Username);
        Assert.Equal("password123", options.Password);
        Assert.Equal("noreply@example.com", options.FromEmail);
        Assert.Equal("My App", options.FromName);
    }

    [Fact]
    public void Port_CanBeSetToDifferentValues()
    {
        // Arrange
        var options = new EmailOptions();

        // Act
        options.Port = 465;

        // Assert
        Assert.Equal(465, options.Port);
    }

    [Fact]
    public void FromName_CanBeCustomized()
    {
        // Arrange
        var options = new EmailOptions();

        // Act
        options.FromName = "Custom Sender Name";

        // Assert
        Assert.Equal("Custom Sender Name", options.FromName);
    }
}
