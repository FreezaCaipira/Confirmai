using Confirmai.Services.Admin;
using System.Security.Claims;

namespace Confirmai.Tests;

public class AdminSettingsServiceStaticTests
{
    [Fact]
    public void EnsureAdmin_WithAuthenticatedAdminUser_DoesNotThrow()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Role, "admin")
        }, "Test"));

        // Act
        var method = typeof(AdminSettingsService).GetMethod("EnsureAdmin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Assert
        var exception = Record.Exception(() => method?.Invoke(null, new object[] { user }));
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureAdmin_WithUnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var method = typeof(AdminSettingsService).GetMethod("EnsureAdmin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
            method?.Invoke(null, new object[] { user }));
        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
    }

    [Fact]
    public void EnsureAdmin_WithNullUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        ClaimsPrincipal? user = null;

        // Act
        var method = typeof(AdminSettingsService).GetMethod("EnsureAdmin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
            method?.Invoke(null, new object[] { user }));
        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
    }

    [Fact]
    public void EnsureAdmin_WithNonAdminUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Role, "user")
        }, "Test"));

        // Act
        var method = typeof(AdminSettingsService).GetMethod("EnsureAdmin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
            method?.Invoke(null, new object[] { user }));
        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
    }

    [Fact]
    public void EnsureAdmin_WithAuthenticatedUserWithoutRole_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user")
        }, "Test"));

        // Act
        var method = typeof(AdminSettingsService).GetMethod("EnsureAdmin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Assert
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
            method?.Invoke(null, new object[] { user }));
        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
    }
}
