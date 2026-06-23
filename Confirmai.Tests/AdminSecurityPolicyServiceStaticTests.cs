using Confirmai.Services.Admin;
using System.Globalization;

namespace Confirmai.Tests;

public class AdminSecurityPolicyServiceStaticTests
{
    [Fact]
    public void ParseBool_WithValidTrue_ReturnsTrue()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "true" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseBool", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { settings, "key", false });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ParseBool_WithValidFalse_ReturnsFalse()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "false" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseBool", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { settings, "key", true });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ParseBool_WithInvalidValue_ReturnsFallback()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "invalid" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseBool", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { settings, "key", true });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ParseBool_WithMissingKey_ReturnsFallback()
    {
        // Arrange
        var settings = new Dictionary<string, string>();

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseBool", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool?)method?.Invoke(null, new object[] { settings, "key", false });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ParseInt_WithValidValue_ReturnsClampedValue()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "15" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseInt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (int?)method?.Invoke(null, new object[] { settings, "key", 5, 1, 20 });

        // Assert
        Assert.Equal(15, result);
    }

    [Fact]
    public void ParseInt_WithValueBelowMin_ReturnsMin()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "0" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseInt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (int?)method?.Invoke(null, new object[] { settings, "key", 5, 1, 20 });

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void ParseInt_WithValueAboveMax_ReturnsMax()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "25" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseInt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (int?)method?.Invoke(null, new object[] { settings, "key", 5, 1, 20 });

        // Assert
        Assert.Equal(20, result);
    }

    [Fact]
    public void ParseInt_WithInvalidValue_ReturnsFallback()
    {
        // Arrange
        var settings = new Dictionary<string, string> { { "key", "invalid" } };

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseInt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (int?)method?.Invoke(null, new object[] { settings, "key", 5, 1, 20 });

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void ParseInt_WithMissingKey_ReturnsFallback()
    {
        // Arrange
        var settings = new Dictionary<string, string>();

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("ParseInt", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (int?)method?.Invoke(null, new object[] { settings, "key", 5, 1, 20 });

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void BuildAuditMessage_WithAllChanges_ReturnsCompleteMessage()
    {
        // Arrange
        var previous = new RuntimeSecurityPolicy(true, 5, 30);
        var current = new RuntimeSecurityPolicy(false, 10, 60);

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("BuildAuditMessage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { previous, current }) as string;

        // Assert
        Assert.Contains("RequireConfirmedEmail: True -> False", result);
        Assert.Contains("LockoutMaxFailedAccessAttempts: 5 -> 10", result);
        Assert.Contains("LockoutMinutes: 30 -> 60", result);
    }

    [Fact]
    public void BuildAuditMessage_WithNoChanges_ReturnsMessageWithSameValues()
    {
        // Arrange
        var previous = new RuntimeSecurityPolicy(true, 5, 30);
        var current = new RuntimeSecurityPolicy(true, 5, 30);

        // Act
        var method = typeof(AdminSecurityPolicyService).GetMethod("BuildAuditMessage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { previous, current }) as string;

        // Assert
        Assert.Contains("RequireConfirmedEmail: True -> True", result);
        Assert.Contains("LockoutMaxFailedAccessAttempts: 5 -> 5", result);
        Assert.Contains("LockoutMinutes: 30 -> 30", result);
    }
}
