using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeSettlementServiceTests
{
    private static PlatformFeeSettlementService CreateService(IDbContextFactory<AppDbContext> factory)
        => new(factory, NullLogger<PlatformFeeSettlementService>.Instance, new UiTextService(new LanguagePreferenceService()));

    private static byte[] FakeImage(int size = 1024) =>
        Enumerable.Range(0, size).Select(_ => (byte)0xFF).ToArray();

    /// <summary>Group + an organizer that is an admin of that group (allowed to submit a settlement).</summary>
    private static async Task<(Group group, ApplicationUser organizer)> SeedGroupWithOrganizerAsync(AppDbContext db)
    {
        var group = TestDataFactory.CreateGroup("Test");
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var organizer = TestDataFactory.CreateUserWithPixKey("u1", "Org", "pix@org");
        db.Users.Add(organizer);
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = organizer.Id,
            Role = GroupMemberRole.Admin
        });
        await db.SaveChangesAsync();

        return (group, organizer);
    }

    /// <summary>A user carrying the "admin" role — the only one allowed to settle the debt.</summary>
    private static async Task<string> SeedSystemAdminAsync(AppDbContext db, string userId = "admin-1")
    {
        var role = new IdentityRole("admin") { NormalizedName = "ADMIN" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task SubmitSettlementAsync_ValidInput_CreatesSettlement()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg");

        Assert.True(result.Success);
        Assert.NotNull(result.SettlementId);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var settlement = verifyDb.PlatformFeeSettlements.Single();
        Assert.Equal(0.75m, settlement.Amount);
        Assert.Equal(organizer.Id, settlement.SubmittedByUserId);
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, settlement.Status);
    }

    [Fact]
    public async Task SubmitSettlementAsync_NonAdminMember_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _) = await SeedGroupWithOrganizerAsync(ctx.db);

        var member = TestDataFactory.CreateUserWithPixKey("u2", "Jogador", "pix@player");
        ctx.db.Users.Add(member);
        ctx.db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = member.Id,
            Role = GroupMemberRole.Member
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, member.Id, 0.75m, FakeImage(), "image/jpeg");

        Assert.False(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.Empty(verifyDb.PlatformFeeSettlements);
    }

    [Fact]
    public async Task SubmitSettlementAsync_AdminOfAnotherGroup_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (_, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var otherGroup = TestDataFactory.CreateGroup("Outro");
        ctx.db.Groups.Add(otherGroup);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            otherGroup.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SubmitSettlementAsync_InvalidMimeType_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "application/pdf");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SubmitSettlementAsync_EmptyFile_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, Array.Empty<byte>(), "image/jpeg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SubmitSettlementAsync_OversizedFile_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(5 * 1024 * 1024 + 1), "image/jpeg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SubmitSettlementAsync_ZeroAmount_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0m, FakeImage(), "image/jpeg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReviewSettlementAsync_Approve_ChangesStatusToPago()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var adminId = await SeedSystemAdminAsync(ctx.db);

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(settlement.Id, adminId, approved: true);

        Assert.True(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var saved = verifyDb.PlatformFeeSettlements.Single();
        Assert.Equal(PlatformFeeSettlementStatus.Pago, saved.Status);
        Assert.Equal(adminId, saved.ReviewedByUserId);
        Assert.NotNull(saved.ReviewedAt);
    }

    [Fact]
    public async Task ReviewSettlementAsync_Organizer_CannotSettleOwnDebt()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(settlement.Id, organizer.Id, approved: true);

        Assert.False(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, verifyDb.PlatformFeeSettlements.Single().Status);
    }

    [Fact]
    public async Task ReviewSettlementAsync_Reject_ChangesStatusToRejeitado()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var adminId = await SeedSystemAdminAsync(ctx.db);

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(
            settlement.Id, adminId, approved: false, note: "Valor incorreto");

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
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var adminId = await SeedSystemAdminAsync(ctx.db);

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = PlatformFeeSettlementStatus.Pago,
            ReviewedByUserId = "admin-0",
            ReviewedAt = DateTime.UtcNow
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(settlement.Id, adminId, approved: false);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ReviewSettlementAsync_NotFound_ReturnsError()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        await SeedSystemAdminAsync(ctx.db);
        var service = CreateService(ctx.factory);
        var result = await service.ReviewSettlementAsync(999, "admin-1", approved: true);

        Assert.False(result.Success);
    }
}
