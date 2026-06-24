using Confirmai.Services.Payment;

namespace Confirmai.Tests;

public class EfiBankPixServiceStaticTests
{
    [Fact]
    public void GetEffectiveAmount_WithSandbox_ReturnsOne()
    {
        // Act
        var result = EfiBankPixService.GetEffectiveAmount(100.50m, sandbox: true);

        // Assert
        Assert.Equal(1.00m, result);
    }

    [Fact]
    public void GetEffectiveAmount_WithSandboxAndZeroAmount_ReturnsOne()
    {
        // Act
        var result = EfiBankPixService.GetEffectiveAmount(0m, sandbox: true);

        // Assert
        Assert.Equal(1.00m, result);
    }

    [Fact]
    public void GetEffectiveAmount_WithoutSandbox_ReturnsRequestedAmount()
    {
        // Act
        var result = EfiBankPixService.GetEffectiveAmount(100.50m, sandbox: false);

        // Assert
        Assert.Equal(100.50m, result);
    }

    [Fact]
    public void GetEffectiveAmount_WithoutSandboxAndZeroAmount_ReturnsZero()
    {
        // Act
        var result = EfiBankPixService.GetEffectiveAmount(0m, sandbox: false);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void GenerateTxId_Returns32CharacterHexString()
    {
        // Act
        var method = typeof(EfiBankPixService).GetMethod("GenerateTxId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length);
        Assert.True(result.All(c => "0123456789ABCDEF".Contains(c)));
    }

    [Fact]
    public void GenerateTxId_GeneratesUniqueValues()
    {
        // Act
        var method = typeof(EfiBankPixService).GetMethod("GenerateTxId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result1 = (string?)method?.Invoke(null, null);
        var result2 = (string?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GenerateTxId_ReturnsUppercase()
    {
        // Act
        var method = typeof(EfiBankPixService).GetMethod("GenerateTxId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result, result.ToUpper());
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(10.00)]
    [InlineData(100.00)]
    [InlineData(9999.99)]
    public void GetEffectiveAmount_WithSandbox_AlwaysReturnsOne(decimal amount)
    {
        // Act
        var result = EfiBankPixService.GetEffectiveAmount(amount, sandbox: true);

        // Assert
        Assert.Equal(1.00m, result);
    }

    [Theory]
    [InlineData(0.01, 0.01)]
    [InlineData(1.00, 1.00)]
    [InlineData(10.50, 10.50)]
    [InlineData(100.99, 100.99)]
    [InlineData(9999.99, 9999.99)]
    public void GetEffectiveAmount_WithoutSandbox_ReturnsExactAmount(decimal amount, decimal expected)
    {
        // Act
        var result = EfiBankPixService.GetEffectiveAmount(amount, sandbox: false);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateTxId_GeneratesMultipleUniqueValues()
    {
        // Act
        var method = typeof(EfiBankPixService).GetMethod("GenerateTxId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var results = new HashSet<string>();
        
        for (int i = 0; i < 100; i++)
        {
            var result = (string?)method?.Invoke(null, null);
            Assert.NotNull(result);
            results.Add(result);
        }

        // Assert
        Assert.Equal(100, results.Count);
    }

    [Fact]
    public void GenerateTxId_ContainsOnlyValidHexCharacters()
    {
        // Act
        var method = typeof(EfiBankPixService).GetMethod("GenerateTxId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string?)method?.Invoke(null, null);

        // Assert
        Assert.NotNull(result);
        var validChars = "0123456789ABCDEF";
        Assert.True(result.All(c => validChars.Contains(c)));
    }
}
