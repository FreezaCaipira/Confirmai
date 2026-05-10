using Confirmai.Data;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class LogServiceTests
{
    [Fact]
    public async Task LogAsync_PersistsLogEntry_WithProvidedValues()
    {
        await using var db = CreateDbContext();
        var service = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        await service.LogAsync(
            message: "payment started",
            source: "Payment",
            level: "Warning",
            userId: "user-42");

        var saved = Assert.Single(db.Logs);
        Assert.Equal("payment started", saved.Message);
        Assert.Equal("Payment", saved.Source);
        Assert.Equal("Warning", saved.Level);
        Assert.Equal("user-42", saved.UserId);
        Assert.Null(saved.Exception);
    }

    [Fact]
    public async Task LogAsync_PersistsExceptionText_WhenExceptionIsProvided()
    {
        await using var db = CreateDbContext();
        var service = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        var ex = new InvalidOperationException("boom");

        await service.LogAsync(message: "failed", ex: ex);

        var saved = Assert.Single(db.Logs);
        Assert.NotNull(saved.Exception);
        Assert.Contains("InvalidOperationException", saved.Exception, StringComparison.Ordinal);
        Assert.Contains("boom", saved.Exception, StringComparison.Ordinal);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"log-service-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AuditAsync_PersistsStructuredAuditFields()
    {
        await using var db = CreateDbContext();
        var service = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        await service.AuditAsync(
            eventType: AuditEvents.OrderCreated,
            entityType: AuditEntities.Order,
            entityId: "123",
            message: "Pedido 123 criado.",
            actorUserId: "buyer-7",
            source: AdminAuditSources.Orders,
            metadata: new { OrderId = 123, Amount = 0.05m, BuyerId = "buyer-7", SellerId = "seller-9" });

        var saved = Assert.Single(db.Logs);
        Assert.Equal(AuditEvents.OrderCreated, saved.EventType);
        Assert.Equal(AuditEntities.Order, saved.EntityType);
        Assert.Equal("123", saved.EntityId);
        Assert.Equal("buyer-7", saved.UserId);
        Assert.Equal(AdminAuditSources.Orders, saved.Source);
        Assert.NotNull(saved.MetadataJson);
        Assert.Contains("\"OrderId\":123", saved.MetadataJson);
        Assert.Contains("\"BuyerId\":\"buyer-7\"", saved.MetadataJson);
    }

    [Fact]
    public async Task AuditAsync_KeepsLegacyFieldsBackwardCompatible()
    {
        await using var db = CreateDbContext();
        var service = new LogService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<Confirmai.Services.LogService>.Instance);

        await service.AuditAsync(
            eventType: AuditEvents.UserLoginFailed,
            entityType: AuditEntities.User,
            entityId: "u-1",
            message: "Bad password",
            level: "Warning");

        var saved = Assert.Single(db.Logs);
        Assert.Equal("Warning", saved.Level);
        Assert.Null(saved.MetadataJson); // no metadata supplied
        Assert.Null(saved.IpAddress);     // no HttpContext in this unit test
    }
}


