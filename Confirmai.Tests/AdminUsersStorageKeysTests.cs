using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminUsersStorageKeysTests
{
    [Fact]
    public void UserNameFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.users.userName", AdminUsersStorageKeys.UserNameFilter);
    }

    [Fact]
    public void EmailFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.users.email", AdminUsersStorageKeys.EmailFilter);
    }

    [Fact]
    public void RoleFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.users.role", AdminUsersStorageKeys.RoleFilter);
    }

    [Fact]
    public void StatusFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.users.status", AdminUsersStorageKeys.StatusFilter);
    }

    [Fact]
    public void Page_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.users.page", AdminUsersStorageKeys.Page);
    }
}
