using Confirmai.Models;

namespace Confirmai.Tests;

public class PaymentRecordTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var record = new PaymentRecord { Id = 1 };
        Assert.Equal(1, record.Id);
    }

    [Fact]
    public void ServerId_CanBeSetAndGet()
    {
        var record = new PaymentRecord { ServerId = 5 };
        Assert.Equal(5, record.ServerId);
    }

    [Fact]
    public void ServerId_CanBeNull()
    {
        var record = new PaymentRecord { ServerId = null };
        Assert.Null(record.ServerId);
    }

    [Fact]
    public void ProductId_CanBeSetAndGet()
    {
        var record = new PaymentRecord { ProductId = 10 };
        Assert.Equal(10, record.ProductId);
    }

    [Fact]
    public void UserId_CanBeSetAndGet()
    {
        var record = new PaymentRecord { UserId = "user-123" };
        Assert.Equal("user-123", record.UserId);
    }

    [Fact]
    public void UserId_CanBeNull()
    {
        var record = new PaymentRecord { UserId = null };
        Assert.Null(record.UserId);
    }

    [Fact]
    public void Address_CanBeSetAndGet()
    {
        var record = new PaymentRecord { Address = "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh" };
        Assert.Equal("bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh", record.Address);
    }

    [Fact]
    public void Address_DefaultsToEmptyString()
    {
        var record = new PaymentRecord();
        Assert.Equal("", record.Address);
    }

    [Fact]
    public void PaymentId_CanBeSetAndGet()
    {
        var record = new PaymentRecord { PaymentId = "pay-abc123" };
        Assert.Equal("pay-abc123", record.PaymentId);
    }

    [Fact]
    public void PaymentId_CanBeNull()
    {
        var record = new PaymentRecord { PaymentId = null };
        Assert.Null(record.PaymentId);
    }

    [Fact]
    public void PaymentMethod_CanBeSetAndGet()
    {
        var record = new PaymentRecord { PaymentMethod = "Pix" };
        Assert.Equal("Pix", record.PaymentMethod);
    }

    [Fact]
    public void PaymentMethod_CanBeNull()
    {
        var record = new PaymentRecord { PaymentMethod = null };
        Assert.Null(record.PaymentMethod);
    }

    [Fact]
    public void Amount_CanBeSetAndGet()
    {
        var record = new PaymentRecord { Amount = 100.50m };
        Assert.Equal(100.50m, record.Amount);
    }

    [Fact]
    public void IsPaid_CanBeSetAndGet()
    {
        var record = new PaymentRecord { IsPaid = true };
        Assert.True(record.IsPaid);
    }

    [Fact]
    public void CreatedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var record = new PaymentRecord { CreatedAt = now };
        Assert.Equal(now, record.CreatedAt);
    }

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var record = new PaymentRecord();
        var after = DateTime.UtcNow;
        Assert.InRange(record.CreatedAt, before, after);
    }

    [Fact]
    public void PaidAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var record = new PaymentRecord { PaidAt = now };
        Assert.Equal(now, record.PaidAt);
    }

    [Fact]
    public void PaidAt_CanBeNull()
    {
        var record = new PaymentRecord { PaidAt = null };
        Assert.Null(record.PaidAt);
    }

    [Fact]
    public void PrivateKey_CanBeSetAndGet()
    {
        var record = new PaymentRecord { PrivateKey = "private-key-123" };
        Assert.Equal("private-key-123", record.PrivateKey);
    }

    [Fact]
    public void PrivateKey_CanBeNull()
    {
        var record = new PaymentRecord { PrivateKey = null };
        Assert.Null(record.PrivateKey);
    }

    [Fact]
    public void SellerId_CanBeSetAndGet()
    {
        var record = new PaymentRecord { SellerId = "seller-456" };
        Assert.Equal("seller-456", record.SellerId);
    }

    [Fact]
    public void SellerId_CanBeNull()
    {
        var record = new PaymentRecord { SellerId = null };
        Assert.Null(record.SellerId);
    }

    [Fact]
    public void Currency_CanBeSetAndGet()
    {
        var record = new PaymentRecord { Currency = "BRL" };
        Assert.Equal("BRL", record.Currency);
    }

    [Fact]
    public void Currency_CanBeNull()
    {
        var record = new PaymentRecord { Currency = null };
        Assert.Null(record.Currency);
    }

    [Fact]
    public void Currency_CanBeSetToUSD()
    {
        var record = new PaymentRecord { Currency = "USD" };
        Assert.Equal("USD", record.Currency);
    }

    [Fact]
    public void Currency_CanBeSetToBTC()
    {
        var record = new PaymentRecord { Currency = "BTC" };
        Assert.Equal("BTC", record.Currency);
    }
}
