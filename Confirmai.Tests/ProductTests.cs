using Confirmai.Models;

namespace Confirmai.Tests;

public class ProductTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var product = new Product { Id = 1 };
        Assert.Equal(1, product.Id);
    }

    [Fact]
    public void Name_CanBeSetAndGet()
    {
        var product = new Product { Name = "Test Product" };
        Assert.Equal("Test Product", product.Name);
    }

    [Fact]
    public void Name_DefaultsToEmptyString()
    {
        var product = new Product();
        Assert.Equal(string.Empty, product.Name);
    }

    [Fact]
    public void Description_CanBeSetAndGet()
    {
        var product = new Product { Description = "Test Description" };
        Assert.Equal("Test Description", product.Description);
    }

    [Fact]
    public void Description_CanBeNull()
    {
        var product = new Product { Description = null };
        Assert.Null(product.Description);
    }

    [Fact]
    public void Price_CanBeSetAndGet()
    {
        var product = new Product { Price = 100.50m };
        Assert.Equal(100.50m, product.Price);
    }

    [Fact]
    public void Price_DefaultsToZero()
    {
        var product = new Product();
        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void Weight_CanBeSetAndGet()
    {
        var product = new Product { Weight = 50.25m };
        Assert.Equal(50.25m, product.Weight);
    }

    [Fact]
    public void Weight_DefaultsToZero()
    {
        var product = new Product();
        Assert.Equal(0m, product.Weight);
    }

    [Fact]
    public void Attack_CanBeSetAndGet()
    {
        var product = new Product { Attack = 100 };
        Assert.Equal(100, product.Attack);
    }

    [Fact]
    public void Attack_DefaultsToZero()
    {
        var product = new Product();
        Assert.Equal(0, product.Attack);
    }

    [Fact]
    public void Defense_CanBeSetAndGet()
    {
        var product = new Product { Defense = 50 };
        Assert.Equal(50, product.Defense);
    }

    [Fact]
    public void Defense_DefaultsToZero()
    {
        var product = new Product();
        Assert.Equal(0, product.Defense);
    }

    [Fact]
    public void Armor_CanBeSetAndGet()
    {
        var product = new Product { Armor = 75 };
        Assert.Equal(75, product.Armor);
    }

    [Fact]
    public void Armor_DefaultsToZero()
    {
        var product = new Product();
        Assert.Equal(0, product.Armor);
    }

    [Fact]
    public void DropSources_CanBeSetAndGet()
    {
        var product = new Product { DropSources = "Monster A, Monster B" };
        Assert.Equal("Monster A, Monster B", product.DropSources);
    }

    [Fact]
    public void DropSources_CanBeNull()
    {
        var product = new Product { DropSources = null };
        Assert.Null(product.DropSources);
    }

    [Fact]
    public void ImagePath_CanBeSetAndGet()
    {
        var product = new Product { ImagePath = "/images/product.png" };
        Assert.Equal("/images/product.png", product.ImagePath);
    }

    [Fact]
    public void ImagePath_CanBeNull()
    {
        var product = new Product { ImagePath = null };
        Assert.Null(product.ImagePath);
    }

    [Fact]
    public void CreatedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var product = new Product { CreatedAt = now };
        Assert.Equal(now, product.CreatedAt);
    }

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var product = new Product();
        var after = DateTime.UtcNow;
        Assert.InRange(product.CreatedAt, before, after);
    }

    [Fact]
    public void UserId_CanBeSetAndGet()
    {
        var product = new Product { UserId = "user-123" };
        Assert.Equal("user-123", product.UserId);
    }

    [Fact]
    public void UserId_DefaultsToEmptyString()
    {
        var product = new Product();
        Assert.Equal(string.Empty, product.UserId);
    }

    [Fact]
    public void ShortDescription_CanBeSetAndGet()
    {
        var product = new Product { ShortDescription = "Short desc" };
        Assert.Equal("Short desc", product.ShortDescription);
    }

    [Fact]
    public void ShortDescription_CanBeNull()
    {
        var product = new Product { ShortDescription = null };
        Assert.Null(product.ShortDescription);
    }

    [Fact]
    public void Category_CanBeSetAndGet()
    {
        var product = new Product { Category = "Weapons" };
        Assert.Equal("Weapons", product.Category);
    }

    [Fact]
    public void Category_CanBeNull()
    {
        var product = new Product { Category = null };
        Assert.Null(product.Category);
    }

    [Fact]
    public void PricingGateway_CanBeSetAndGet()
    {
        var product = new Product { PricingGateway = "Pix" };
        Assert.Equal("Pix", product.PricingGateway);
    }

    [Fact]
    public void PricingGateway_CanBeNull()
    {
        var product = new Product { PricingGateway = null };
        Assert.Null(product.PricingGateway);
    }

    [Fact]
    public void AccentColor_CanBeSetAndGet()
    {
        var product = new Product { AccentColor = "#FF0000" };
        Assert.Equal("#FF0000", product.AccentColor);
    }

    [Fact]
    public void AccentColor_CanBeNull()
    {
        var product = new Product { AccentColor = null };
        Assert.Null(product.AccentColor);
    }

    [Fact]
    public void RequiresDelivery_CanBeSetAndGet()
    {
        var product = new Product { RequiresDelivery = true };
        Assert.True(product.RequiresDelivery);
    }

    [Fact]
    public void RequiresDelivery_DefaultsToFalse()
    {
        var product = new Product();
        Assert.False(product.RequiresDelivery);
    }
}
