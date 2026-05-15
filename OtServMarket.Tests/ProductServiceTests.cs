using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Confirmai.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task AddAsync_PersistsProduct_WhenImageIsNull()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        await service.AddAsync(new Product
        {
            Name = "Produto 1",
            Description = "Descrição",
            Price = 0.001m,
            UserId = "seller-1"
        }, imageFile: null);

        Assert.Equal(1, await db.Products.CountAsync());
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_WorkAsExpected()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var product = new Product
        {
            Name = "Antes",
            Description = "Desc",
            Price = 0.01m,
            UserId = "seller-1"
        };

        await service.AddAsync(product, imageFile: null);
        product.Name = "Depois";
        await service.UpdateAsync(product, imageFile: null);

        var updated = await service.GetByIdAsync(product.Id);
        Assert.Equal("Depois", updated!.Name);

        await service.DeleteAsync(product.Id);
        Assert.Null(await service.GetByIdAsync(product.Id));
    }

    [Fact]
    public async Task GetAllExceptUserAsync_FiltersByUser()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        await service.AddAsync(new Product { Name = "A", Description = "D", Price = 0.1m, UserId = "u1" }, null);
        await service.AddAsync(new Product { Name = "B", Description = "D", Price = 0.2m, UserId = "u2" }, null);

        var products = await service.GetAllExceptUserAsync("u1");

        Assert.Single(products);
        Assert.Equal("u2", products[0].UserId);
    }

    private static IWebHostEnvironment CreateEnvironment()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        return env.Object;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ExcludesArchivedProducts()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1" });
        db.Products.AddRange(
            new Product { Name = "Active", Description = "d", Price = 1m, UserId = "u1" },
            new Product { Name = "Archived", Description = "d", Price = 1m, UserId = "u1", Category = "legacy-archived" },
            new Product { Name = "Deleted", Description = "d", Price = 1m, UserId = "u1", Category = "deleted-archived" }
        );
        await db.SaveChangesAsync();

        var result = await service.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsProduct_WhenExists()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var product = new Product { Name = "Test", Description = "d", Price = 1m, UserId = "u1" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await service.GetByIdAsync(product.Id);
        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        Assert.Null(await service.GetByIdAsync(999));
    }

    [Fact]
    public async Task AddAsync_SetsRequiresDeliveryFalse()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var product = new Product { Name = "Item", Description = "d", Price = 1m, UserId = "u1", RequiresDelivery = true };
        await service.AddAsync(product, null);

        Assert.False(product.RequiresDelivery);
    }

    [Fact]
    public async Task AddAsync_NormalizesAccentColor()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var product = new Product { Name = "Item", Description = "d", Price = 1m, UserId = "u1", AccentColor = "  ff00aa  " };
        await service.AddAsync(product, null);

        Assert.Equal("#FF00AA", product.AccentColor);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenProductNotFound()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var ghost = new Product { Id = 999, Name = "Ghost", Description = "d", Price = 1m, UserId = "u1" };
        await service.UpdateAsync(ghost, null);

        Assert.Equal(0, await db.Products.CountAsync());
    }

    // ── DeleteAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        Assert.Equal(ProductService.ProductDeleteResult.NotFound, await service.DeleteAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_Deletes_WhenNoOrders()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var product = new Product { Name = "Item", Description = "d", Price = 1m, UserId = "u1" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await service.DeleteAsync(product.Id);

        Assert.Equal(ProductService.ProductDeleteResult.Deleted, result);
        Assert.Null(await db.Products.FindAsync(product.Id));
    }

    // ── GetProductsCountAsync ────────────────────────────────────────

    [Fact]
    public async Task GetProductsCountAsync_ExcludesArchived()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        db.Products.AddRange(
            new Product { Name = "A", Description = "d", Price = 1m, UserId = "u1" },
            new Product { Name = "B", Description = "d", Price = 1m, UserId = "u1", Category = "deleted-archived" },
            new Product { Name = "C", Description = "d", Price = 1m, UserId = "u1" }
        );
        await db.SaveChangesAsync();

        Assert.Equal(2, await service.GetProductsCountAsync());
    }

    // ── GetByUserIdAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyUserProducts()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        db.Products.AddRange(
            new Product { Name = "Mine", Description = "d", Price = 1m, UserId = "u1" },
            new Product { Name = "Theirs", Description = "d", Price = 1m, UserId = "u2" }
        );
        await db.SaveChangesAsync();

        var result = await service.GetByUserIdAsync("u1");

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Name);
    }

    [Fact]
    public async Task GetByUserIdAsync_ExcludesArchived()
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        db.Products.AddRange(
            new Product { Name = "Active", Description = "d", Price = 1m, UserId = "u1" },
            new Product { Name = "Archived", Description = "d", Price = 1m, UserId = "u1", Category = "legacy-archived" }
        );
        await db.SaveChangesAsync();

        var result = await service.GetByUserIdAsync("u1");

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    // ── AccentColor normalization ────────────────────────────────────

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData("#ff00aa", "#FF00AA")]
    [InlineData("ff00aa", "#FF00AA")]
    [InlineData("  #aaBBcc  ", "#AABBCC")]
    [InlineData("zzzzzz", null)]
    [InlineData("12345", null)]
    public async Task AccentColor_IsNormalizedOnAdd(string? input, string? expected)
    {
        using var db = TestDataFactory.CreateDbContext();
        var service = new ProductService(db, CreateEnvironment());

        var product = new Product { Name = "P", Description = "d", Price = 1m, UserId = "u1", AccentColor = input };
        await service.AddAsync(product, null);

        Assert.Equal(expected, product.AccentColor);
    }
}
