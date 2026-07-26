using Confirmai.Models;
using Confirmai.Services.Utility;
using System.Globalization;

namespace Confirmai.Tests;

public class ProductServiceStaticTests
{
    [Fact]
    public void ArchiveProduct_SetsCategoryAndPrefixesName()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Category = "Active",
            RequiresDelivery = true
        };

        // Act - Call private static method via reflection
        var method = typeof(ProductService).GetMethod("ArchiveProduct", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method?.Invoke(null, new object[] { product });

        // Assert
        Assert.Equal("deleted-archived", product.Category);
        Assert.False(product.RequiresDelivery);
        Assert.StartsWith("[ARQUIVADO] ", product.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArchiveProduct_WhenAlreadyArchived_DoesNotDuplicatePrefix()
    {
        // Arrange
        var product = new Product
        {
            Name = "[ARQUIVADO] Test Product",
            Category = "Active",
            RequiresDelivery = true
        };

        // Act
        var method = typeof(ProductService).GetMethod("ArchiveProduct", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method?.Invoke(null, new object[] { product });

        // Assert
        Assert.Equal("[ARQUIVADO] Test Product", product.Name); // No duplicate prefix
    }

    [Theory]
    [InlineData("#FF0000", "#FF0000")]
    [InlineData("FF0000", "#FF0000")]
    [InlineData("ff0000", "#FF0000")]
    [InlineData("#00FF00", "#00FF00")]
    [InlineData("#0000FF", "#0000FF")]
    [InlineData("#ABCDEF", "#ABCDEF")]
    [InlineData("#123456", "#123456")]
    public void NormalizeAccentColor_ValidHex_ReturnsNormalized(string input, string expected)
    {
        // Act
        var method = typeof(ProductService).GetMethod("NormalizeAccentColor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object?[] { input }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#FFF")]
    [InlineData("#FFFFF")]
    [InlineData("#FFFFFFF")]
    [InlineData("ZZZZZZ")]
    [InlineData("12345G")]
    public void NormalizeAccentColor_InvalidHex_ReturnsNull(string? input)
    {
        // Act
        var method = typeof(ProductService).GetMethod("NormalizeAccentColor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object?[] { input }) as string;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NormalizeAccentColor_TrimsWhitespace()
    {
        // Act
        var method = typeof(ProductService).GetMethod("NormalizeAccentColor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method?.Invoke(null, new object[] { "  #FF0000  " }) as string;

        // Assert
        Assert.Equal("#FF0000", result);
    }
}
