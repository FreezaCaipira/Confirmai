using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

internal static class TestDataFactory
{
    public static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static IDbContextFactory<AppDbContext> CreateDbContextFactory(AppDbContext sharedContext)
    {
        return new InMemoryDbContextFactory(sharedContext.Database.GetDbConnection().ConnectionString
            ?? Guid.NewGuid().ToString());
    }

    public static (AppDbContext db, IDbContextFactory<AppDbContext> factory) CreateDbContextWithFactory()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return (new AppDbContext(options), new InMemoryDbContextFactory(dbName));
    }

    private sealed class InMemoryDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _dbName;
        public InMemoryDbContextFactory(string dbName) => _dbName = dbName;
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new AppDbContext(options);
        }
    }

    public static PaymentRecord SeedPayment(
        AppDbContext db,
        bool isPaid,
        decimal amount,
        string? method,
        string paymentId,
        string address = "tb1qexampleaddress",
        string buyerId = "buyer-1",
        string sellerId = "seller-1")
    {
        var product = new Product
        {
            Name = "Produto teste",
            Description = "Descrição",
            Price = amount,
            UserId = sellerId
        };

        db.Products.Add(product);
        db.SaveChanges();

        var payment = new PaymentRecord
        {
            ProductId = product.Id,
            Address = address,
            PaymentId = paymentId,
            PaymentMethod = method,
            Amount = amount,
            IsPaid = isPaid,
            UserId = buyerId
        };

        db.Payments.Add(payment);
        db.SaveChanges();
        return payment;
    }
}

