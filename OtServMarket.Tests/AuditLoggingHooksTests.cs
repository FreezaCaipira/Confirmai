using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

/// <summary>
/// Verifies that domain services emit structured audit events
/// (<see cref="LogService.AuditAsync"/>) for the business actions wired up
/// in the April 2026 audit logging refactor. Each test exercises the real
/// service with a real <see cref="LogService"/> on an in-memory DbContext,
/// then asserts on the persisted <see cref="AppLog"/>.
/// </summary>
public class AuditLoggingHooksTests
{
    [Fact]
    public async Task ProductService_AddAsync_WritesProductCreatedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ProductService(db, CreateEnvironment(), log);

        var product = new Product
        {
            Name = "Crystal Coin",
            Description = "100cc",
            Price = 0.001m,
            UserId = "seller-1"
        };

        await service.AddAsync(product, imageFile: null);

        var entry = Assert.Single(db.Logs);
        Assert.Equal(AuditEvents.ProductCreated, entry.EventType);
        Assert.Equal(AuditEntities.Product, entry.EntityType);
        Assert.Equal(product.Id.ToString(), entry.EntityId);
        Assert.Equal("seller-1", entry.UserId);
        Assert.Equal(AdminAuditSources.Products, entry.Source);
        Assert.NotNull(entry.MetadataJson);
        Assert.Contains("\"Name\":\"Crystal Coin\"", entry.MetadataJson);
    }

    [Fact]
    public async Task ProductService_UpdateAsync_WritesProductUpdatedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ProductService(db, CreateEnvironment(), log);

        var product = new Product
        {
            Name = "Old name",
            Description = "x",
            Price = 0.01m,
            UserId = "seller-2"
        };

        await service.AddAsync(product, imageFile: null);
        product.Name = "New name";
        await service.UpdateAsync(product, imageFile: null);

        var updated = await db.Logs
            .Where(l => l.EventType == AuditEvents.ProductUpdated)
            .ToListAsync();

        var entry = Assert.Single(updated);
        Assert.Equal(AuditEntities.Product, entry.EntityType);
        Assert.Equal(product.Id.ToString(), entry.EntityId);
        Assert.Contains("\"Name\":\"New name\"", entry.MetadataJson);
    }

    [Fact]
    public async Task ProductService_DeleteAsync_WritesProductDeletedAudit_WhenNoOrders()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ProductService(db, CreateEnvironment(), log);

        var product = new Product
        {
            Name = "Disposable",
            Description = "x",
            Price = 0.01m,
            UserId = "seller-3"
        };
        await service.AddAsync(product, imageFile: null);

        var result = await service.DeleteAsync(product.Id);

        Assert.Equal(ProductService.ProductDeleteResult.Deleted, result);

        var deletedAudit = await db.Logs
            .SingleAsync(l => l.EventType == AuditEvents.ProductDeleted);
        Assert.Equal(AuditEntities.Product, deletedAudit.EntityType);
        Assert.Equal(product.Id.ToString(), deletedAudit.EntityId);
        Assert.Equal("Warning", deletedAudit.Level);
        Assert.Equal(AdminAuditSources.Products, deletedAudit.Source);
    }

    [Fact]
    public async Task ProductService_DeleteAsync_WritesProductArchivedAudit_WhenRelatedOrdersExist()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ProductService(db, CreateEnvironment(), log);

        var product = new Product
        {
            Name = "Has orders",
            Description = "x",
            Price = 0.01m,
            UserId = "seller-4"
        };
        await service.AddAsync(product, imageFile: null);

        db.Orders.Add(new OrderModel
        {
            ProductId = product.Id,
            BuyerId = "buyer-1",
            SellerId = "seller-4",
            Amount = 0.01m
        });
        await db.SaveChangesAsync();

        var result = await service.DeleteAsync(product.Id);

        Assert.Equal(ProductService.ProductDeleteResult.ArchivedWithOrders, result);
        var archivedAudit = await db.Logs
            .SingleAsync(l => l.EventType == AuditEvents.ProductArchived);
        Assert.Equal(product.Id.ToString(), archivedAudit.EntityId);
        Assert.Equal("Warning", archivedAudit.Level);
    }

    [Fact]
    public async Task ServerRegistrationRequestService_CreateAsync_WritesServerRequestedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ServerRegistrationRequestService(db, CreateEnvironment(), log);

        var input = new ServerRegistrationRequestService.CreateServerRegistrationRequestInput
        {
            Name = "Server Alpha",
            TibiaVersion = "8.60",
            WebsiteUrl = "https://alpha.example",
            Region = "BR",
            RequestNote = "first request"
        };

        var request = await service.CreateAsync(input, requesterUserId: "owner-1");

        var entry = Assert.Single(db.Logs);
        Assert.Equal(AuditEvents.ServerRequested, entry.EventType);
        Assert.Equal(AuditEntities.Server, entry.EntityType);
        Assert.Equal(request.Id.ToString(), entry.EntityId);
        Assert.Equal("owner-1", entry.UserId);
        Assert.NotNull(entry.MetadataJson);
        Assert.Contains("\"RequestedName\":\"Server Alpha\"", entry.MetadataJson);
        Assert.Contains("\"TibiaVersion\":\"8.60\"", entry.MetadataJson);
    }

    [Fact]
    public async Task LogService_AuditAsync_DoesNotPersistMetadata_WhenMetadataIsNull()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);

        await log.AuditAsync(
            AuditEvents.UserLockedOut,
            AuditEntities.User,
            entityId: "u-99",
            message: "Account locked after repeated failures.",
            actorUserId: "u-99",
            source: AdminAuditSources.Identity,
            level: "Warning");

        var entry = Assert.Single(db.Logs);
        Assert.Equal(AuditEvents.UserLockedOut, entry.EventType);
        Assert.Equal("Warning", entry.Level);
        Assert.Equal(AdminAuditSources.Identity, entry.Source);
        Assert.Null(entry.MetadataJson);
    }

    [Fact]
    public async Task LogService_AuditAsync_PersistsExceptionDetails_WhenProvided()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);

        await log.AuditAsync(
            AuditEvents.PaymentInvalid,
            AuditEntities.Payment,
            entityId: "inv-bad",
            message: "Invalid payment payload",
            level: "Error",
            ex: new InvalidOperationException("signature mismatch"));

        var entry = Assert.Single(db.Logs);
        Assert.Equal("Error", entry.Level);
        Assert.NotNull(entry.Exception);
        Assert.Contains("signature mismatch", entry.Exception);
    }

    private static IWebHostEnvironment CreateEnvironment()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        return env.Object;
    }

    // ── ItemOffer ──────────────────────────────────────────────────

    [Fact]
    public async Task TibiaServerService_CreateItemOfferAsync_WritesItemOfferCreatedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var env = CreateEnvironment();
        var service = new TibiaServerService(db, env, log: log);

        var server = new TibiaServer { Name = "Audit Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var request = new TibiaServerService.CreateItemOfferRequest
        {
            ServerId = server.Id,
            ItemKey = "crystal coin",
            ItemName = "Crystal Coin",
            Quantity = 10,
            UnitPrice = 0.05m,
            PricingGateway = "btcpay",
            SellerUserId = "seller-99"
        };

        var result = await service.CreateItemOfferAsync(request);

        Assert.True(result.Success);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ItemOfferCreated);
        Assert.Equal(AuditEntities.ItemOffer, entry.EntityType);
        Assert.Equal(result.Offer!.Id.ToString(), entry.EntityId);
        Assert.Equal("seller-99", entry.UserId);
        Assert.Equal(AdminAuditSources.ItemOffers, entry.Source);
        Assert.NotNull(entry.MetadataJson);
        Assert.Contains("\"Quantity\":10", entry.MetadataJson);
    }

    [Fact]
    public async Task TibiaServerService_DeactivateOwnItemOffersAsync_WritesItemOfferCancelledAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new TibiaServerService(db, CreateEnvironment(), log: log);

        var server = new TibiaServer { Name = "Cancel Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ItemOffers.Add(new ItemOffer
        {
            ServerId = server.Id,
            ItemKey = "CRYSTAL COIN",
            ItemName = "Crystal Coin",
            Quantity = 5,
            UnitPrice = 0.05m,
            PricingGateway = "btcpay",
            SellerUserId = "seller-cancel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var offer = db.ItemOffers.Single();
        var count = await service.DeactivateOwnItemOffersAsync(server.Id, "crystal coin", "seller-cancel");

        Assert.Equal(1, count);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ItemOfferCancelled);
        Assert.Equal(AuditEntities.ItemOffer, entry.EntityType);
        Assert.Equal(offer.Id.ToString(), entry.EntityId);
        Assert.Equal("seller-cancel", entry.UserId);
        Assert.Equal(AdminAuditSources.ItemOffers, entry.Source);
    }

    // ── ServerMember ───────────────────────────────────────────────

    [Fact]
    public async Task TibiaServerService_AddOrUpdateMemberByEmailAsync_Added_WritesServerMemberAddedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new TibiaServerService(db, CreateEnvironment(), log: log);

        var server = new TibiaServer { Name = "Member Server", IsActive = true };
        db.Servers.Add(server);
        var user = new ApplicationUser { Id = "member-u1", UserName = "newmember", Email = "newmember@test.com", NormalizedEmail = "NEWMEMBER@TEST.COM" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await service.AddOrUpdateMemberByEmailAsync(server.Id, "newmember@test.com", ServerMemberRole.User);

        Assert.Equal(TibiaServerService.AddServerMemberResult.Added, result);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ServerMemberAdded);
        Assert.Equal(AuditEntities.Server, entry.EntityType);
        Assert.Equal(server.Id.ToString(), entry.EntityId);
        Assert.Equal(AdminAuditSources.ServerMembers, entry.Source);
        Assert.NotNull(entry.MetadataJson);
        Assert.Contains("\"role\":\"User\"", entry.MetadataJson);
    }

    [Fact]
    public async Task TibiaServerService_AddOrUpdateMemberByEmailAsync_Updated_WritesServerMemberUpdatedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new TibiaServerService(db, CreateEnvironment(), log: log);

        var server = new TibiaServer { Name = "Member Server 2", IsActive = true };
        db.Servers.Add(server);
        var user = new ApplicationUser { Id = "member-u2", UserName = "existingmember", Email = "existing@test.com", NormalizedEmail = "EXISTING@TEST.COM" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "member-u2", Role = ServerMemberRole.User });
        await db.SaveChangesAsync();

        var result = await service.AddOrUpdateMemberByEmailAsync(server.Id, "existing@test.com", ServerMemberRole.ServerAdmin);

        Assert.Equal(TibiaServerService.AddServerMemberResult.Updated, result);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ServerMemberUpdated);
        Assert.Equal(AuditEntities.Server, entry.EntityType);
        Assert.Equal(AdminAuditSources.ServerMembers, entry.Source);
        Assert.NotNull(entry.MetadataJson);
        Assert.Contains("\"role\":\"ServerAdmin\"", entry.MetadataJson);
    }

    [Fact]
    public async Task TibiaServerService_RemoveMemberAsync_WritesServerMemberRemovedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new TibiaServerService(db, CreateEnvironment(), log: log);

        var server = new TibiaServer { Name = "Remove Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = "remove-u1", Role = ServerMemberRole.User });
        await db.SaveChangesAsync();

        var removed = await service.RemoveMemberAsync(server.Id, "remove-u1");

        Assert.True(removed);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ServerMemberRemoved);
        Assert.Equal(AuditEntities.Server, entry.EntityType);
        Assert.Equal(server.Id.ToString(), entry.EntityId);
        Assert.Equal("Warning", entry.Level);
        Assert.Equal(AdminAuditSources.ServerMembers, entry.Source);
    }

    // ── ServerApiKey ───────────────────────────────────────────────

    [Fact]
    public async Task ServerApiKeyService_CreateKeyAsync_WritesServerApiKeyIssuedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ServerApiKeyService(db, log);

        var server = new TibiaServer { Name = "ApiKey Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var result = await service.CreateKeyAsync(server.Id, label: "integration");

        Assert.True(result.Success);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ServerApiKeyIssued);
        Assert.Equal(AuditEntities.ApiKey, entry.EntityType);
        Assert.Equal(result.Entity!.Id.ToString(), entry.EntityId);
        Assert.Equal(AdminAuditSources.ApiKeys, entry.Source);
        Assert.NotNull(entry.MetadataJson);
        Assert.Contains("\"Label\":\"integration\"", entry.MetadataJson);
    }

    [Fact]
    public async Task ServerApiKeyService_RevokeKeyAsync_WritesServerApiKeyRevokedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new ServerApiKeyService(db, log);

        var server = new TibiaServer { Name = "Revoke Server", IsActive = true };
        db.Servers.Add(server);
        await db.SaveChangesAsync();

        var created = await service.CreateKeyAsync(server.Id, label: null);
        db.Logs.RemoveRange(db.Logs); // clear created-audit so we can assert only the revoke
        await db.SaveChangesAsync();

        var revoked = await service.RevokeKeyAsync(created.Entity!.Id);

        Assert.True(revoked);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.ServerApiKeyRevoked);
        Assert.Equal(AuditEntities.ApiKey, entry.EntityType);
        Assert.Equal(created.Entity.Id.ToString(), entry.EntityId);
        Assert.Equal("Warning", entry.Level);
        Assert.Equal(AdminAuditSources.ApiKeys, entry.Source);
    }

    // ── AdminSettingsService ───────────────────────────────────────

    [Fact]
    public async Task AdminSettingsService_SetOperationFeePercentAsync_WritesAdminSettingChangedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new AdminSettingsService(db, log);

        var result = await service.SetOperationFeePercentAsync(3.5m, actorUserId: "admin-1");

        Assert.True(result);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.AdminSettingChanged);
        Assert.Equal(AuditEntities.Setting, entry.EntityType);
        Assert.Equal(AdminSettingsService.OperationFeePercentKey, entry.EntityId);
        Assert.Equal("admin-1", entry.UserId);
        Assert.Equal(AdminAuditSources.AdminSettings, entry.Source);
    }

    [Fact]
    public async Task AdminSettingsService_SetSiteIntermediaryPixKeyAsync_WritesAdminSettingChangedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new AdminSettingsService(db, log);

        var result = await service.SetSiteIntermediaryPixKeyAsync("pix@example.com", actorUserId: "admin-2");

        Assert.True(result);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.AdminSettingChanged);
        Assert.Equal(AdminSettingsService.SiteIntermediaryPixKey, entry.EntityId);
        Assert.Equal("admin-2", entry.UserId);
        Assert.Equal(AdminAuditSources.AdminSettings, entry.Source);
    }

    [Fact]
    public async Task AdminSettingsService_SetLuaDeliveryEnabledAsync_WritesAdminSettingChangedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new AdminSettingsService(db, log);

        await service.SetLuaDeliveryEnabledAsync(true, actorUserId: "admin-3");

        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.AdminSettingChanged);
        Assert.Equal(AdminSettingsService.LuaDeliveryEnabledKey, entry.EntityId);
        Assert.Equal("admin-3", entry.UserId);
        Assert.Equal(AdminAuditSources.AdminSettings, entry.Source);
    }

    [Fact]
    public async Task AdminLogsQueryService_GetEntityTimelineAsync_ReturnsOnlyMatchingLogs()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        await using var _ = db;

        db.Logs.AddRange(
            new AppLog { EntityType = "Order", EntityId = "42", Level = "Info", Source = "Test", Message = "first",  Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new AppLog { EntityType = "Order", EntityId = "42", Level = "Info", Source = "Test", Message = "second", Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new AppLog { EntityType = "Order", EntityId = "99", Level = "Info", Source = "Test", Message = "other-order" },
            new AppLog { EntityType = "User",  EntityId = "42", Level = "Info", Source = "Test", Message = "other-type" }
        );
        await db.SaveChangesAsync();

        var service = new AdminLogsQueryService(factory);
        var result = await service.GetEntityTimelineAsync("Order", "42");

        Assert.Equal(2, result.Count);
        Assert.All(result, l => { Assert.Equal("Order", l.EntityType); Assert.Equal("42", l.EntityId); });
        Assert.Equal("first", result[0].Message);
        Assert.Equal("second", result[1].Message);
    }

    [Fact]
    public async Task AdminLogsQueryService_GetEntityTimelineAsync_ReturnsEmptyForUnknownEntity()
    {
        var (db, factory) = TestDataFactory.CreateDbContextWithFactory();
        await using var _ = db;

        db.Logs.Add(new AppLog { EntityType = "Order", EntityId = "1", Level = "Info", Source = "Test", Message = "x" });
        await db.SaveChangesAsync();

        var service = new AdminLogsQueryService(factory);
        var result = await service.GetEntityTimelineAsync("Order", "999");

        Assert.Empty(result);
    }

    // ── Order transitions ─────────────────────────────────────────

    [Fact]
    public async Task TibiaServerService_RejectPendingTradeAsync_WritesOrderDisputedAudit()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new TibiaServerService(db, CreateEnvironment(), log: log);

        var server = new TibiaServer { Name = "Dispute Server", IsActive = true };
        db.Servers.Add(server);
        var buyer = new ApplicationUser { Id = "buyer-d1", UserName = "buyer" };
        var seller = new ApplicationUser { Id = "seller-d1", UserName = "seller" };
        db.Users.AddRange(buyer, seller);
        await db.SaveChangesAsync();

        var order = new OrderModel
        {
            ServerId = server.Id,
            BuyerId = buyer.Id,
            SellerId = seller.Id,
            Amount = 0.001m,
            Status = PaymentStatus.AguardandoEntregaInGame
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await service.RejectPendingTradeAsync(server.Id, order.Id, "item nao entregue");

        Assert.True(result.Success);
        var entry = await db.Logs.SingleAsync(l => l.EventType == AuditEvents.OrderDisputed);
        Assert.Equal(AuditEntities.Order, entry.EntityType);
        Assert.Equal(order.Id.ToString(), entry.EntityId);
        Assert.Equal("ServerIntegration", entry.Source);
        Assert.Contains("Disputa", entry.Message);
    }

    [Fact]
    public async Task TibiaServerService_RejectPendingTradeAsync_DoesNotAudit_WhenOrderNotFound()
    {
        await using var db = TestDataFactory.CreateDbContext();
        var log = new LogService(db, NullLogger<LogService>.Instance);
        var service = new TibiaServerService(db, CreateEnvironment(), log: log);

        var result = await service.RejectPendingTradeAsync(serverId: 1, tradeId: 9999, rejectReason: "x");

        Assert.False(result.Success);
        Assert.Empty(db.Logs);
    }
}
