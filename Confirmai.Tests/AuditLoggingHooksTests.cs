using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Utility;

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

    // -- AdminSettingsService ---------------------------------------

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
}
