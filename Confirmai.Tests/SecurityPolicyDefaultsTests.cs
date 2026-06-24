using Confirmai.Configuration;

namespace Confirmai.Tests;

public class SecurityPolicyDefaultsTests
{
    [Fact]
    public void Create_WhenIsDevelopment_ReturnsDevelopmentDefaults()
    {
        // Arrange & Act
        var snapshot = SecurityPolicyDefaults.Create(isDevelopment: true);

        // Assert
        Assert.False(snapshot.RequireConfirmedEmail);
        Assert.Equal(6, snapshot.PasswordRequiredLength);
        Assert.True(snapshot.PasswordRequireDigit);
        Assert.True(snapshot.PasswordRequireLowercase);
        Assert.False(snapshot.PasswordRequireUppercase);
        Assert.False(snapshot.PasswordRequireNonAlphanumeric);
        Assert.Equal(1, snapshot.PasswordRequiredUniqueChars);
        Assert.Equal(5, snapshot.LockoutMaxFailedAccessAttempts);
        Assert.Equal(15, snapshot.LockoutMinutes);
        Assert.Equal(60, snapshot.SessionTimeoutMinutes);
    }

    [Fact]
    public void Create_WhenIsProduction_ReturnsProductionDefaults()
    {
        // Arrange & Act
        var snapshot = SecurityPolicyDefaults.Create(isDevelopment: false);

        // Assert
        Assert.True(snapshot.RequireConfirmedEmail);
        Assert.Equal(10, snapshot.PasswordRequiredLength);
        Assert.True(snapshot.PasswordRequireDigit);
        Assert.True(snapshot.PasswordRequireLowercase);
        Assert.True(snapshot.PasswordRequireUppercase);
        Assert.True(snapshot.PasswordRequireNonAlphanumeric);
        Assert.Equal(3, snapshot.PasswordRequiredUniqueChars);
        Assert.Equal(5, snapshot.LockoutMaxFailedAccessAttempts);
        Assert.Equal(15, snapshot.LockoutMinutes);
        Assert.Equal(30, snapshot.SessionTimeoutMinutes);
    }

    [Fact]
    public void Create_DevelopmentVsProduction_Differences()
    {
        // Arrange
        var dev = SecurityPolicyDefaults.Create(isDevelopment: true);
        var prod = SecurityPolicyDefaults.Create(isDevelopment: false);

        // Assert - key differences
        Assert.NotEqual(dev.RequireConfirmedEmail, prod.RequireConfirmedEmail);
        Assert.NotEqual(dev.PasswordRequiredLength, prod.PasswordRequiredLength);
        Assert.NotEqual(dev.PasswordRequireUppercase, prod.PasswordRequireUppercase);
        Assert.NotEqual(dev.PasswordRequireNonAlphanumeric, prod.PasswordRequireNonAlphanumeric);
        Assert.NotEqual(dev.PasswordRequiredUniqueChars, prod.PasswordRequiredUniqueChars);
        Assert.NotEqual(dev.SessionTimeoutMinutes, prod.SessionTimeoutMinutes);

        // Assert - same values
        Assert.Equal(dev.PasswordRequireDigit, prod.PasswordRequireDigit);
        Assert.Equal(dev.PasswordRequireLowercase, prod.PasswordRequireLowercase);
        Assert.Equal(dev.LockoutMaxFailedAccessAttempts, prod.LockoutMaxFailedAccessAttempts);
        Assert.Equal(dev.LockoutMinutes, prod.LockoutMinutes);
    }
}
