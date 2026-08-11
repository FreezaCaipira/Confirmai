using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeSettlementProofAuthorizerTests
{
    private static PlatformFeeSettlementProofAuthorizer CreateService(
        (AppDbContext db, IDbContextFactory<AppDbContext> factory) ctx)
        => new(ctx.factory);

    private static byte[] FakeImage(int size = 512) =>
        Enumerable.Range(0, size).Select(_ => (byte)0xAB).ToArray();

    private static async Task<(Group group, ApplicationUser organizer, PlatformFeeSettlement settlement)> SeedSettlementWithProofAsync(
        AppDbContext db, PlatformFeeSettlementStatus status = PlatformFeeSettlementStatus.EmAnalise)
    {
        var group = TestDataFactory.CreateGroup("Racha");
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var organizer = TestDataFactory.CreateUserWithPixKey("org-1", "Org", "pix@org");
        db.Users.Add(organizer);
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = organizer.Id, Role = GroupMemberRole.Admin
        });
        await db.SaveChangesAsync();

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = status,
            ProofImageData = FakeImage(),
            ProofContentType = "image/jpeg"
        };
        db.PlatformFeeSettlements.Add(settlement);
        await db.SaveChangesAsync();

        return (group, organizer, settlement);
    }

    private static async Task<string> SeedSystemAdminAsync(AppDbContext db, string userId = "sysadmin-1")
    {
        var role = new IdentityRole("admin") { NormalizedName = "ADMIN" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task GetProofForUserAsync_Submitter_IsAuthorized()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (_, organizer, settlement) = await SeedSettlementWithProofAsync(ctx.db);

        var service = CreateService(ctx);
        var proof = await service.GetProofForUserAsync(settlement.Id, organizer.Id);

        Assert.NotNull(proof);
        Assert.Equal("image/jpeg", proof!.Value.ContentType);
        Assert.True(proof.Value.Data.Length > 0);
    }

    [Fact]
    public async Task GetProofForUserAsync_GroupAdmin_IsAuthorized()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, settlement) = await SeedSettlementWithProofAsync(ctx.db);

        var otherAdmin = TestDataFactory.CreateUserWithPixKey("admin2", "Admin2", "pix@admin2");
        ctx.db.Users.Add(otherAdmin);
        ctx.db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = otherAdmin.Id, Role = GroupMemberRole.Admin
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var proof = await service.GetProofForUserAsync(settlement.Id, otherAdmin.Id);

        Assert.NotNull(proof);
    }

    [Fact]
    public async Task GetProofForUserAsync_SystemAdmin_IsAuthorized()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (_, _, settlement) = await SeedSettlementWithProofAsync(ctx.db);
        var adminId = await SeedSystemAdminAsync(ctx.db);

        var service = CreateService(ctx);
        var proof = await service.GetProofForUserAsync(settlement.Id, adminId);

        Assert.NotNull(proof);
    }

    [Fact]
    public async Task GetProofForUserAsync_ThirdParty_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (_, _, settlement) = await SeedSettlementWithProofAsync(ctx.db);

        var stranger = TestDataFactory.CreateUserWithPixKey("stranger", "Stranger", "pix@stranger");
        ctx.db.Users.Add(stranger);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetProofForUserAsync(settlement.Id, stranger.Id));
    }

    [Fact]
    public async Task GetProofForUserAsync_NullUserId_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (_, _, settlement) = await SeedSettlementWithProofAsync(ctx.db);

        var service = CreateService(ctx);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetProofForUserAsync(settlement.Id, userId: null));
    }

    [Fact]
    public async Task GetProofForUserAsync_EmptyUserId_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (_, _, settlement) = await SeedSettlementWithProofAsync(ctx.db);

        var service = CreateService(ctx);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetProofForUserAsync(settlement.Id, userId: ""));
    }

    [Fact]
    public async Task GetProofForUserAsync_NonExistentSettlement_ReturnsNull()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var service = CreateService(ctx);
        var proof = await service.GetProofForUserAsync(999, "anyone");
        Assert.Null(proof);
    }

    [Fact]
    public async Task GetProofForUserAsync_SettlementWithoutProof_ReturnsNull()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer, _) = await SeedSettlementWithProofAsync(ctx.db);

        var noProof = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise,
            ProofImageData = null
        };
        ctx.db.PlatformFeeSettlements.Add(noProof);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var proof = await service.GetProofForUserAsync(noProof.Id, organizer.Id);
        Assert.Null(proof);
    }

    [Fact]
    public async Task GetProofForUserAsync_EmptyProofBytes_ReturnsNull()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer, _) = await SeedSettlementWithProofAsync(ctx.db);

        var emptyProof = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = organizer.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise,
            ProofImageData = Array.Empty<byte>()
        };
        ctx.db.PlatformFeeSettlements.Add(emptyProof);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var proof = await service.GetProofForUserAsync(emptyProof.Id, organizer.Id);
        Assert.Null(proof);
    }

    [Fact]
    public async Task GetProofForUserAsync_GroupMember_NotAdmin_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, settlement) = await SeedSettlementWithProofAsync(ctx.db);

        var member = TestDataFactory.CreateUserWithPixKey("member-1", "Membro", "pix@member");
        ctx.db.Users.Add(member);
        ctx.db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = member.Id, Role = GroupMemberRole.Member
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetProofForUserAsync(settlement.Id, member.Id));
    }
}
