using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Confirmai.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetUserByNameAsync_ReturnsNull_WhenUserNameIsBlank()
    {
        var service = new UserService(CreateUserManager().Object);

        var user = await service.GetUserByNameAsync(" ");

        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByNameAndRoles_WorkWithUserManager()
    {
        var mock = CreateUserManager();
        var user = new ApplicationUser { UserName = "alice" };

        mock.Setup(x => x.FindByNameAsync("alice")).ReturnsAsync(user);
        mock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "admin" });

        var service = new UserService(mock.Object);

        var found = await service.GetUserByNameAsync("alice");
        var roles = await service.GetUserRolesAsync(user);

        Assert.NotNull(found);
        Assert.Single(roles);
        Assert.Equal("admin", roles[0]);
    }

    [Fact]
    public async Task GetUsersCountAsync_ReturnsCountFromQueryable()
    {
        var mock = CreateUserManager();
        mock.SetupGet(x => x.Users).Returns(new List<ApplicationUser>
        {
            new() { UserName = "u1" },
            new() { UserName = "u2" },
            new() { UserName = "u3" }
        }.AsQueryable());

        var service = new UserService(mock.Object);
        var count = await service.GetUsersCountAsync();

        Assert.Equal(3, count);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}

