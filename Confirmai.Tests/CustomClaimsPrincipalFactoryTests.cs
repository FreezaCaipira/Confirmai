using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Confirmai.Tests;

public class CustomClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task CreateAsync_WithNullUser_Throws()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
        
        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object,
            null!, null!, null!, null!);
        
        var optionsMock = new Mock<IOptions<IdentityOptions>>();
        optionsMock.Setup(x => x.Value).Returns(new IdentityOptions());
        
        var (db, dbFactory) = TestDataFactory.CreateDbContextWithFactory();
        
        var factory = new CustomClaimsPrincipalFactory(
            userManagerMock.Object,
            roleManagerMock.Object,
            optionsMock.Object,
            dbFactory);
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => factory.CreateAsync(null!));
    }
}
