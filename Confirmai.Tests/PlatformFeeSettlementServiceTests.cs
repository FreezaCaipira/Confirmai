using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeSettlementServiceTests
{
    private static PlatformFeeSettlementService CreateService(IDbContextFactory<AppDbContext> factory)
        => new(factory, NullLogger<PlatformFeeSettlementService>.Instance);

    private static byte[] FakeImage(int size = 1024) =>
        Enumerable.Range(0, size).Select(_ => (byte)0xFF).ToArray();

    [Fact]
    public async Task SubmitSettlementAsync_ValidInput_CreatesSettlement()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, user.Id, 0.75m, FakeImage(), "image/jpeg");

        Assert.True(result.Success);
        Assert.NotNull(result.SettlementId);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var settlement = verifyDb.PlatformFeeSettlements.Single();
        Assert.Equal(0.75m, settlement.Amount);
        Assert.Equal(user.Id, settlement.SubmittedByUserId);
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, settlement.Status);
    }

    [Fact]
    public async Task SubmitSettlementAsync_InvalidMimeType_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, user.Id, 0.75m, FakeImage(), "application/pdf");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SubmitSettlementAsync_EmptyFile_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, user.Id, 0.75m, Array.Empty<byte>(), "image/jpeg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SubmitSettlementAsync_ZeroAmount_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, user.Id, 0m, FakeImage(), "image/jpeg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReviewSettlementAsync_Approve_ChangesStatusToPago()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(settlement.Id, "admin-1", approved: true);

        Assert.True(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var saved = verifyDb.PlatformFeeSettlements.Single();
        Assert.Equal(PlatformFeeSettlementStatus.Pago, saved.Status);
        Assert.Equal("admin-1", saved.ReviewedByUserId);
        Assert.NotNull(saved.ReviewedAt);
    }

    [Fact]
    public async Task ReviewSettlementAsync_Reject_ChangesStatusToRejeitado()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(
            settlement.Id, "admin-1", approved: false, note: "Valor incorreto");

        Assert.True(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var saved = verifyDb.PlatformFeeSettlements.Single();
        Assert.Equal(PlatformFeeSettlementStatus.Rejeitado, saved.Status);
        Assert.Equal("Valor incorreto", saved.ReviewNote);
    }

    [Fact]
    public async Task ReviewSettlementAsync_AlreadyReviewed_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test");
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Pago,
            ReviewedByUserId = "admin-0",
            ReviewedAt = DateTime.UtcNow
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(settlement.Id, "admin-1", approved: false);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReviewSettlementAsync_NotFound_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(999, "admin-1", approved: true);

        Assert.False(result.Success);
    }
}
