using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeWaiverServiceTests
{
    private static PlatformFeeWaiverService CreateService(
        (AppDbContext db, IDbContextFactory<AppDbContext> factory) ctx,
        decimal manualFee = 0.75m)
    {
        var options = Options.Create(new FeeOptions { Enabled = true, ManualPlatformFeeFixed = manualFee });
        var log = new LogService(ctx.factory, NullLogger<LogService>.Instance);
        return new PlatformFeeWaiverService(ctx.factory, options, log);
    }

    private static async Task<ApplicationUser> SeedSysAdminAsync(AppDbContext db, string userId = "admin-1")
    {
        var role = new IdentityRole { Name = "admin", NormalizedName = "ADMIN" };
        db.Roles.Add(role);
        var user = TestDataFactory.CreateUserWithPixKey(userId, "Admin", "admin@test");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Group> SeedGroupAsync(AppDbContext db)
    {
        var group = TestDataFactory.CreateGroup("Racha Teste", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    [Fact]
    public async Task SetWaiverAsync_SysAdmin_SetsFieldsAndAudits()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var admin = await SeedSysAdminAsync(ctx.db);
        var group = await SeedGroupAsync(ctx.db);
        var until = DateTime.UtcNow.AddDays(30);
        var service = CreateService(ctx);

        var result = await service.SetWaiverAsync(group.Id, admin.Id, until, "grupo parceiro piloto");

        Assert.True(result.Success);
        await using var verifyDb = ctx.factory.CreateDbContext();
        var saved = verifyDb.Groups.Single(g => g.Id == group.Id);
        Assert.Equal(until, saved.PlatformFeeWaivedUntil);
        Assert.Equal("grupo parceiro piloto", saved.PlatformFeeWaiverReason);

        var audit = verifyDb.Logs.Single(l => l.EventType == AuditEvents.GroupFeeWaiverChanged);
        Assert.Equal(group.Id.ToString(), audit.EntityId);
        Assert.Equal(admin.Id, audit.UserId);
    }

    [Fact]
    public async Task SetWaiverAsync_RenewsExistingWaiver()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var admin = await SeedSysAdminAsync(ctx.db);
        var group = await SeedGroupAsync(ctx.db);
        var service = CreateService(ctx);

        var first = DateTime.UtcNow.AddDays(10);
        await service.SetWaiverAsync(group.Id, admin.Id, first, "piloto");
        var second = DateTime.UtcNow.AddDays(60);
        var result = await service.SetWaiverAsync(group.Id, admin.Id, second, "renovado");

        Assert.True(result.Success);
        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.Equal(second, verifyDb.Groups.Single().PlatformFeeWaivedUntil);
        Assert.Equal(2, verifyDb.Logs.Count(l => l.EventType == AuditEvents.GroupFeeWaiverChanged));
    }

    [Fact]
    public async Task SetWaiverAsync_NonAdmin_Denied()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player", "p@test");
        ctx.db.Users.Add(user);
        var group = await SeedGroupAsync(ctx.db);
        var service = CreateService(ctx);

        var result = await service.SetWaiverAsync(group.Id, user.Id, DateTime.UtcNow.AddDays(30), "motivo");

        Assert.False(result.Success);
        Assert.Equal(PlatformFeeWaiverError.NotAuthorized, result.Error);
        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.Null(verifyDb.Groups.Single().PlatformFeeWaivedUntil);
        Assert.Empty(verifyDb.Logs.Where(l => l.EventType == AuditEvents.GroupFeeWaiverChanged));
    }

    [Fact]
    public async Task SetWaiverAsync_GroupAdminButNotSysAdmin_Denied()
    {
        // The organizer (group admin) must never control the platform fee waiver.
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var user = TestDataFactory.CreateUserWithPixKey("org-1", "Organizer", "org@test");
        ctx.db.Users.Add(user);
        var group = await SeedGroupAsync(ctx.db);
        ctx.db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = user.Id,
            Role = GroupMemberRole.Admin,
        });
        await ctx.db.SaveChangesAsync();
        var service = CreateService(ctx);

        var result = await service.SetWaiverAsync(group.Id, user.Id, DateTime.UtcNow.AddDays(30), "auto-isenção");

        Assert.False(result.Success);
        Assert.Equal(PlatformFeeWaiverError.NotAuthorized, result.Error);
        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.Null(verifyDb.Groups.Single().PlatformFeeWaivedUntil);
    }

    [Fact]
    public async Task SetWaiverAsync_PastExpiration_Rejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var admin = await SeedSysAdminAsync(ctx.db);
        var group = await SeedGroupAsync(ctx.db);
        var service = CreateService(ctx);

        var result = await service.SetWaiverAsync(group.Id, admin.Id, DateTime.UtcNow.AddDays(-1), "motivo");

        Assert.False(result.Success);
        Assert.Equal(PlatformFeeWaiverError.ExpirationNotFuture, result.Error);
    }

    [Fact]
    public async Task SetWaiverAsync_MissingReason_Rejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var admin = await SeedSysAdminAsync(ctx.db);
        var group = await SeedGroupAsync(ctx.db);
        var service = CreateService(ctx);

        var result = await service.SetWaiverAsync(group.Id, admin.Id, DateTime.UtcNow.AddDays(30), "   ");

        Assert.False(result.Success);
        Assert.Equal(PlatformFeeWaiverError.ReasonRequired, result.Error);
    }

    [Fact]
    public async Task ClearWaiverAsync_RevokesAndAudits()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var admin = await SeedSysAdminAsync(ctx.db);
        var group = await SeedGroupAsync(ctx.db);
        var service = CreateService(ctx);
        await service.SetWaiverAsync(group.Id, admin.Id, DateTime.UtcNow.AddDays(30), "piloto");

        var result = await service.ClearWaiverAsync(group.Id, admin.Id);

        Assert.True(result.Success);
        await using var verifyDb = ctx.factory.CreateDbContext();
        var saved = verifyDb.Groups.Single();
        Assert.Null(saved.PlatformFeeWaivedUntil);
        Assert.Null(saved.PlatformFeeWaiverReason);
        Assert.Equal(2, verifyDb.Logs.Count(l => l.EventType == AuditEvents.GroupFeeWaiverChanged));
    }

    [Fact]
    public async Task ClearWaiverAsync_NonAdmin_Denied()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var admin = await SeedSysAdminAsync(ctx.db);
        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player", "p@test");
        ctx.db.Users.Add(user);
        var group = await SeedGroupAsync(ctx.db);
        var service = CreateService(ctx);
        await service.SetWaiverAsync(group.Id, admin.Id, DateTime.UtcNow.AddDays(30), "piloto");

        var result = await service.ClearWaiverAsync(group.Id, user.Id);

        Assert.False(result.Success);
        Assert.Equal(PlatformFeeWaiverError.NotAuthorized, result.Error);
        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.NotNull(verifyDb.Groups.Single().PlatformFeeWaivedUntil);
    }

    [Fact]
    public async Task GetWaiverOverviewAsync_ReturnsActiveAndExpired()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var active = TestDataFactory.CreateGroup("Ativo", enablePaymentGateways: false);
        active.PlatformFeeWaivedUntil = DateTime.UtcNow.AddDays(10);
        active.PlatformFeeWaiverReason = "piloto";
        var expired = TestDataFactory.CreateGroup("Expirado", enablePaymentGateways: false);
        expired.PlatformFeeWaivedUntil = DateTime.UtcNow.AddDays(-5);
        expired.PlatformFeeWaiverReason = "teste";
        var plain = TestDataFactory.CreateGroup("Comum", enablePaymentGateways: false);
        ctx.db.Groups.AddRange(active, expired, plain);
        await ctx.db.SaveChangesAsync();
        var service = CreateService(ctx);

        var rows = await service.GetWaiverOverviewAsync();

        Assert.Equal(2, rows.Count);
        Assert.True(rows.Single(r => r.GroupName == "Ativo").Active);
        Assert.False(rows.Single(r => r.GroupName == "Expirado").Active);
    }

    [Fact]
    public async Task GetWaivedStatsAsync_CountsZeroStampedConfirmationsInPeriod()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = await SeedGroupAsync(ctx.db);
        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player", "p@test");
        ctx.db.Users.Add(user);
        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        // Two waived confirmations (fee stamped as 0) inside the period…
        for (var i = 0; i < 2; i++)
        {
            ctx.db.EventConfirmations.Add(new EventConfirmation
            {
                EventId = evt.Id,
                UserId = user.Id,
                PaymentStatus = EventConfirmationPaymentStatus.Paid,
                HasPaid = true,
                PlatformFeeAmount = 0m,
                MarkedPaidAt = DateTime.UtcNow.AddDays(-2),
            });
        }
        // …one outside the period, and one normally stamped (not waived).
        ctx.db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evt.Id, UserId = user.Id,
            PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true,
            PlatformFeeAmount = 0m, MarkedPaidAt = DateTime.UtcNow.AddDays(-60),
        });
        ctx.db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evt.Id, UserId = user.Id,
            PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true,
            PlatformFeeAmount = 0.75m, MarkedPaidAt = DateTime.UtcNow.AddDays(-2),
        });
        await ctx.db.SaveChangesAsync();
        var service = CreateService(ctx);

        var stats = await service.GetWaivedStatsAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        Assert.Equal(2, stats.WaivedConfirmations);
        Assert.Equal(1.50m, stats.WaivedAmount);
    }
}
