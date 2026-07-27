using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class AdminUsersQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public AdminUsersQueryServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        var factory = new TestDbContextFactory(options);
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
        return factory;
    }

    private static async Task SeedUsersAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "alice", Email = "alice@test.com" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "bob", Email = "bob@test.com" });
        db.Users.Add(new ApplicationUser { Id = "u3", UserName = "charlie", Email = "charlie@test.com" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LoadPageAsync_ReturnsAllUsers_WhenNoFilters()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("", "", "", "", 1, 10);

        Assert.Equal(3, data.TotalUsers);
        Assert.Equal(3, data.Users.Count);
    }

    [Fact]
    public async Task LoadPageAsync_FiltersByUserName()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("alice", "", "", "", 1, 10);

        Assert.Equal(1, data.TotalUsers);
        Assert.Single(data.Users);
        Assert.Equal("alice", data.Users[0].UserName);
    }

    [Fact]
    public async Task LoadPageAsync_FiltersByEmail()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("", "bob@test.com", "", "", 1, 10);

        Assert.Equal(1, data.TotalUsers);
        Assert.Single(data.Users);
    }

    [Fact]
    public async Task LoadPageAsync_ReturnsEmpty_WhenNoMatch()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("nonexistent", "", "", "", 1, 10);

        Assert.Equal(0, data.TotalUsers);
        Assert.Empty(data.Users);
    }

    [Fact]
    public async Task LoadPageAsync_ReturnsCorrectTotalPages()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("", "", "", "", 1, 2);

        Assert.Equal(2, data.TotalPages);
        Assert.Equal(2, data.Users.Count);
    }

    [Fact]
    public async Task LoadPageAsync_ReturnsAtLeast1Page_WhenNoUsers()
    {
        var factory = CreateFactory();
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("", "", "", "", 1, 10);

        Assert.Equal(1, data.TotalPages);
        Assert.Equal(0, data.TotalUsers);
    }

    [Fact]
    public async Task LoadPageAsync_PaginatesCorrectly()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var page1 = await svc.LoadPageAsync("", "", "", "", 1, 2);
        var page2 = await svc.LoadPageAsync("", "", "", "", 2, 2);

        Assert.Equal(2, page1.Users.Count);
        Assert.Single(page2.Users);
    }

    [Fact]
    public async Task LoadPageAsync_ReturnsUserRolesDict()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("", "", "", "", 1, 10);

        Assert.Equal(3, data.UserRoles.Count);
        Assert.All(data.UserRoles.Values, v => Assert.Equal("-", v));
    }

    [Fact]
    public async Task LoadPageAsync_FiltersCaseInsensitively()
    {
        var factory = CreateFactory();
        await SeedUsersAsync(factory);
        var svc = new AdminUsersQueryService(factory);

        var data = await svc.LoadPageAsync("ALICE", "", "", "", 1, 10);

        Assert.Equal(1, data.TotalUsers);
    }
}
