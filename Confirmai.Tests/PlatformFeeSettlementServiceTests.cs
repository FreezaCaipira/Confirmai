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

    /// <summary>Seeds a paid futsal match with an accrued platform fee in the group.</summary>
    private static async Task<Event> SeedMatchWithFeeAsync(
        AppDbContext db, Group group, string dateStr, string userSuffix, decimal fee = 0.75m)
    {
        var evt = TestDataFactory.CreateEvent(group, dateStr, 15m);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var player = TestDataFactory.CreateUserWithPixKey($"p-{userSuffix}", $"J{userSuffix}", $"pix@{userSuffix}");
        db.Users.Add(player);
        await db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, player);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = fee;
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        return evt;
    }

    [Fact]
    public async Task SubmitSettlementAsync_ValidInput_CreatesSettlement()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-10", "a");

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { evt.Id });

        Assert.True(result.Success);
        Assert.NotNull(result.SettlementId);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var settlement = await verifyDb.PlatformFeeSettlements
            .Include(s => s.Items)
            .SingleAsync();
        Assert.Equal(0.75m, settlement.Amount);
        Assert.Equal(organizer.Id, settlement.SubmittedByUserId);
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, settlement.Status);
        var item = Assert.Single(settlement.Items);
        Assert.Equal(evt.Id, item.EventId);
        Assert.Equal(0.75m, item.FeeAmount);
    }

    [Fact]
    public async Task SubmitSettlementAsync_MultipleMatches_StoresFeeSnapshotPerItem()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        // Match A: 2 paid players x R$0,75 = R$1,50 of accrued fee
        var eA = await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-10", "a1");
        var playerA2 = TestDataFactory.CreateUserWithPixKey("p-a2", "JA2", "pix@a2");
        ctx.db.Users.Add(playerA2);
        await ctx.db.SaveChangesAsync();
        var confA2 = TestDataFactory.CreateEventConfirmation(eA, playerA2);
        confA2.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        confA2.HasPaid = true;
        confA2.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(confA2);
        await ctx.db.SaveChangesAsync();
        // Match B: 1 paid player x R$0,75 = R$0,75 of accrued fee
        var eB = await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-11", "b");

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 2.25m, FakeImage(), "image/jpeg", new[] { eA.Id, eB.Id });

        Assert.True(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        var settlement = await verifyDb.PlatformFeeSettlements
            .Include(s => s.Items.OrderBy(i => i.EventId))
            .SingleAsync();
        Assert.Equal(2, settlement.Items.Count);
        Assert.Equal(eA.Id, settlement.Items[0].EventId);
        Assert.Equal(1.50m, settlement.Items[0].FeeAmount); // snapshot of accrued fee
        Assert.Equal(eB.Id, settlement.Items[1].EventId);
        Assert.Equal(0.75m, settlement.Items[1].FeeAmount);
    }

    [Fact]
    public async Task SubmitSettlementAsync_WithoutMatchSelection_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-10", "a");

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg");

        Assert.False(result.Success);
        Assert.Empty(ctx.db.PlatformFeeSettlements);
    }

    [Fact]
    public async Task SubmitSettlementAsync_AmountBelowSelectedFees_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var e1 = await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-10", "a");
        var e2 = await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-11", "b");

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { e1.Id, e2.Id });

        Assert.False(result.Success);
        Assert.Empty(ctx.db.PlatformFeeSettlements);
    }

    [Fact]
    public async Task SubmitSettlementAsync_MatchFromAnotherGroup_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);

        var other = TestDataFactory.CreateGroup("Outro");
        ctx.db.Groups.Add(other);
        await ctx.db.SaveChangesAsync();
        var foreign = await SeedMatchWithFeeAsync(ctx.db, other, "2026-08-10", "x");

        var service = CreateService(ctx.factory);
        var result = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { foreign.Id });

        Assert.False(result.Success);
        Assert.Empty(ctx.db.PlatformFeeSettlements);
    }

    [Fact]
    public async Task SubmitSettlementAsync_MatchAlreadyInAnotherSettlement_IsRejected()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = await SeedMatchWithFeeAsync(ctx.db, group, "2026-08-10", "a");

        var service = CreateService(ctx.factory);
        var first = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { evt.Id });
        Assert.True(first.Success);

        var second = await service.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { evt.Id });

        Assert.False(second.Success);
        Assert.Single(ctx.factory.CreateDbContext().PlatformFeeSettlements);
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
    public async Task ReviewSettlementAsync_RejectWithoutNote_IsRejected()
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
            settlement.Id, adminId, approved: false, note: null);

        Assert.False(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        // Settlement must remain EmAnalise — the rejection was refused.
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise,
            verifyDb.PlatformFeeSettlements.Single().Status);
    }

    [Fact]
    public async Task ReviewSettlementAsync_RejectWithWhitespaceNote_IsRejected()
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
            settlement.Id, adminId, approved: false, note: "   ");

        Assert.False(result.Success);

        await using var verifyDb = ctx.factory.CreateDbContext();
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise,
            verifyDb.PlatformFeeSettlements.Single().Status);
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
