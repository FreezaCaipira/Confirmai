using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Confirmai.Tests;

public class AppInitializationServiceTests
{
    [Fact]
    public async Task SeedAsync_EmitsLogs_WhenAdminIsCreatedAndPromoted()
    {
        var loggerProvider = new TestLoggerProvider();

        using var serviceProvider = BuildServiceProvider(
            CreateConfiguration(adminEmail: "logs-admin@test.local"),
            loggerProvider);
        using var scope = serviceProvider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<AppInitializationService>();

        await service.SeedAsync();

        Assert.Contains(loggerProvider.Messages, m => m.Contains("Usuário admin seed criado"));
        Assert.Contains(loggerProvider.Messages, m => m.Contains("promovido a admin"));
    }

    [Fact]
    public async Task SeedAsync_AddsDefaultGateways_Idempotently()
    {
        using var serviceProvider = BuildServiceProvider(CreateConfiguration());
        using var scope = serviceProvider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<AppInitializationService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await service.SeedAsync();
        await service.SeedAsync();

        var gateways = await db.Gateways
            .OrderBy(g => g.Name)
            .ToListAsync();

        Assert.Equal(3, gateways.Count);
        Assert.Equal("BTCPayServer", gateways[0].Name);
        Assert.Equal("Pix", gateways[1].Name);
        Assert.Equal("Testnet", gateways[2].Name);
    }

    [Fact]
    public async Task SeedAsync_CreatesAdminUser_AndAssignsAdminRole()
    {
        const string adminEmail = "admin@test.local";

        using var serviceProvider = BuildServiceProvider(CreateConfiguration(adminEmail: adminEmail));
        using var scope = serviceProvider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<AppInitializationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await service.SeedAsync();

        var admin = await userManager.FindByEmailAsync(adminEmail);
        Assert.NotNull(admin);
        Assert.True(await roleManager.RoleExistsAsync("admin"));
        Assert.True(await userManager.IsInRoleAsync(admin!, "admin"));
    }

    [Fact]
    public async Task SeedAsync_PromotesExistingUserToAdmin_WhenUserExistsWithoutRole()
    {
        const string adminEmail = "existing-admin@test.local";

        using var serviceProvider = BuildServiceProvider(CreateConfiguration(adminEmail: adminEmail));
        using var scope = serviceProvider.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<AppInitializationService>();

        var existingUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Existing User"
        };

        var createUser = await userManager.CreateAsync(existingUser, "P@ssw0rd123!");
        Assert.True(createUser.Succeeded);

        await service.SeedAsync();

        var persisted = await userManager.FindByEmailAsync(adminEmail);
        Assert.NotNull(persisted);
        Assert.True(await userManager.IsInRoleAsync(persisted!, "admin"));
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration, TestLoggerProvider? loggerProvider = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            if (loggerProvider != null)
            {
                builder.AddProvider(loggerProvider);
            }
        });
        services.AddSingleton(configuration);
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<AppInitializationService>();

        return services.BuildServiceProvider();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Confirmai.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration CreateConfiguration(
        string adminEmail = "seed-admin@test.local",
        string adminPassword = "P@ssw0rd123!",
        string adminFullName = "Seed Admin")
    {
        return TestConfigurationFactory.Create(
            ("AdminSeed:Email", adminEmail),
            ("AdminSeed:Password", adminPassword),
            ("AdminSeed:FullName", adminFullName));
    }
}

