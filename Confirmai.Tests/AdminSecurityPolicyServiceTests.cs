using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Confirmai.Tests;

public class AdminSecurityPolicyServiceTests
{
    [Fact]
    public async Task GetRuntimePolicyAsync_ReturnsEnvironmentDefaults_WhenSettingsDoNotExist()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"admin-security-policy-{Guid.NewGuid()}");
        await using var db = factory.CreateDbContext();
        var service = CreateService(db, factory, isDevelopment: false);

        var policy = await service.GetRuntimePolicyAsync();

        Assert.True(policy.RequireConfirmedEmail);
        Assert.Equal(5, policy.LockoutMaxFailedAccessAttempts);
        Assert.Equal(15, policy.LockoutMinutes);
    }

    [Fact]
    public async Task GetRuntimePolicyAsync_UsesPersistedValues_WhenSettingsExist()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"admin-security-policy-{Guid.NewGuid()}");
        await using var db = factory.CreateDbContext();
        db.AppSettings.AddRange(
            new AppSetting { Key = AdminSecurityPolicyService.RequireConfirmedEmailKey, Value = "false" },
            new AppSetting { Key = AdminSecurityPolicyService.LockoutMaxAttemptsKey, Value = "3" },
            new AppSetting { Key = AdminSecurityPolicyService.LockoutMinutesKey, Value = "25" });
        await db.SaveChangesAsync();

        var service = CreateService(db, factory, isDevelopment: false);
        var policy = await service.GetRuntimePolicyAsync();

        Assert.False(policy.RequireConfirmedEmail);
        Assert.Equal(3, policy.LockoutMaxFailedAccessAttempts);
        Assert.Equal(25, policy.LockoutMinutes);
    }

    [Fact]
    public async Task SetRuntimePolicyForAdminAsync_Throws_WhenUserIsNotAdmin()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"admin-security-policy-{Guid.NewGuid()}");
        await using var db = factory.CreateDbContext();
        var service = CreateService(db, factory, isDevelopment: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetRuntimePolicyForAdminAsync(CreatePrincipal("u-1", "user"), new RuntimeSecurityPolicy(false, 3, 15)));
    }

    [Fact]
    public async Task SetRuntimePolicyForAdminAsync_PersistsValues_WhenUserIsAdmin()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"admin-security-policy-{Guid.NewGuid()}");
        await using var db = factory.CreateDbContext();
        // Seed RequireConfirmedEmail = true so the audit log shows True -> False
        db.AppSettings.Add(new AppSetting { Key = AdminSecurityPolicyService.RequireConfirmedEmailKey, Value = "true" });
        await db.SaveChangesAsync();
        var service = CreateService(db, factory, isDevelopment: false);

        var saved = await service.SetRuntimePolicyForAdminAsync(CreatePrincipal("a-1", "admin"), new RuntimeSecurityPolicy(false, 2, 30));
        var policy = await service.GetRuntimePolicyAsync();

        Assert.True(saved);
        Assert.False(policy.RequireConfirmedEmail);
        Assert.Equal(2, policy.LockoutMaxFailedAccessAttempts);
        Assert.Equal(30, policy.LockoutMinutes);

        var auditLog = await db.Logs
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.Equal(AdminAuditSources.SecurityPolicy, auditLog!.Source);
        Assert.Equal(AdminAuditLevels.Success, auditLog.Level);
        Assert.Equal("a-1", auditLog.UserId);
        Assert.Contains("Security policy updated.", auditLog.Message);
        Assert.Contains("RequireConfirmedEmail: True -> False", auditLog.Message);
        Assert.Contains("LockoutMaxFailedAccessAttempts: 5 -> 2", auditLog.Message);
        Assert.Contains("LockoutMinutes: 15 -> 30", auditLog.Message);
    }

    private static IWebHostEnvironment CreateEnvironment(bool isDevelopment)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");
        return env.Object;
    }

    private static AdminSecurityPolicyService CreateService(AppDbContext db, IDbContextFactory<AppDbContext> factory, bool isDevelopment)
    {
        return new AdminSecurityPolicyService(
            factory,
            CreateEnvironment(isDevelopment),
            new LogService(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.Core.LogService>.Instance));
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

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test-auth"));
    }
}


