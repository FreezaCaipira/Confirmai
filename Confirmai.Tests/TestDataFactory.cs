using Confirmai.Data;
using Confirmai.Enums;
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

    public static Group CreateGroup(string name, bool enablePaymentGateways = false)
    {
        return new Group { Name = name, EnablePaymentGateways = enablePaymentGateways };
    }

    public static GroupPayoutAccount CreateGroupPayoutAccount(int groupId, string pixKey, PixKeyType keyType = PixKeyType.Email)
    {
        return new GroupPayoutAccount
        {
            GroupId = groupId,
            PixKeyType = keyType,
            PixKeyValue = pixKey,
            IsActive = true
        };
    }

    public static ApplicationUser CreateUserWithPixKey(string userId, string userName, string pixKey)
    {
        return new ApplicationUser
        {
            Id = userId,
            UserName = userName,
            FullName = userName,
            PixKey = pixKey
        };
    }

    public static Event CreateEvent(Group group, string dateStr, decimal price)
    {
        return new Event
        {
            GroupId = group.Id,
            Group = group,
            StartsAt = DateTime.Parse(dateStr),
            Price = price
        };
    }

    public static EventConfirmation CreateEventConfirmation(Event evt, ApplicationUser user)
    {
        return new EventConfirmation
        {
            EventId = evt.Id,
            Event = evt,
            UserId = user.Id,
            User = user,
            ConfirmedAt = DateTime.UtcNow
        };
    }
}


