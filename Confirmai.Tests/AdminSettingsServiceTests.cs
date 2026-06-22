using Confirmai.Services.Admin;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Confirmai.Tests;

public class AdminSettingsServiceTests
{
    [Fact]
    public async Task GetOperationFeePercentAsync_ReturnsDefault_WhenSettingDoesNotExist()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        var service = new AdminSettingsService(dbFactory);

        var fee = await service.GetOperationFeePercentAsync();

        Assert.Equal(AdminSettingsService.DefaultOperationFeePercent, fee);
    }

    [Fact]
    public async Task GetOperationFeePercentAsync_ReturnsDefault_WhenStoredValueIsInvalid()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        await using var db = dbFactory.CreateDbContext();
        db.AppSettings.Add(new AppSetting
        {
            Key = AdminSettingsService.OperationFeePercentKey,
            Value = "not-a-number"
        });
        await db.SaveChangesAsync();

        var service = new AdminSettingsService(dbFactory);
        var fee = await service.GetOperationFeePercentAsync();

        Assert.Equal(AdminSettingsService.DefaultOperationFeePercent, fee);
    }

    [Fact]
    public async Task SetOperationFeePercentAsync_PersistsRoundedValue_WhenInputIsValid()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        var service = new AdminSettingsService(dbFactory);

        var saved = await service.SetOperationFeePercentAsync(3.257m);
        var fee = await service.GetOperationFeePercentAsync();

        Assert.True(saved);
        Assert.Equal(3.26m, fee);
    }

    [Fact]
    public async Task SetOperationFeePercentAsync_ReturnsFalse_WhenInputIsOutOfRange()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        var service = new AdminSettingsService(dbFactory);

        var savedNegative = await service.SetOperationFeePercentAsync(-1m);
        var savedAboveHundred = await service.SetOperationFeePercentAsync(100.01m);

        Assert.False(savedNegative);
        Assert.False(savedAboveHundred);
    }

    [Fact]
    public async Task GetOperationFeePercentForAdminAsync_Throws_WhenUserIsNotAdmin()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        var service = new AdminSettingsService(dbFactory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetOperationFeePercentForAdminAsync(CreatePrincipal(userId: "u-1", roles: "user")));
    }

    [Fact]
    public async Task SetOperationFeePercentForAdminAsync_Throws_WhenUserIsNotAdmin()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        var service = new AdminSettingsService(dbFactory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetOperationFeePercentForAdminAsync(CreatePrincipal(userId: "u-2", roles: "user"), 1.5m));
    }

    [Fact]
    public async Task SetOperationFeePercentForAdminAsync_Succeeds_WhenUserIsAdmin()
    {
        var dbFactory = TestDbContextFactory.CreateInMemoryFactory($"admin-settings-{Guid.NewGuid()}");
        var service = new AdminSettingsService(dbFactory);

        var saved = await service.SetOperationFeePercentForAdminAsync(CreatePrincipal(userId: "a-1", roles: "admin"), 4.5m);
        var fee = await service.GetOperationFeePercentForAdminAsync(CreatePrincipal(userId: "a-1", roles: "admin"));

        Assert.True(saved);
        Assert.Equal(4.5m, fee);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "test-auth");
        return new ClaimsPrincipal(identity);
    }
}


