using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// End-to-end test for the V1 manual platform-fee flow:
///
///   1. Player uploads Pix proof
///   2. Group admin accepts the proof  (MarkPaidAsync / TogglePaidAsync)
///   3. Platform fee is stamped on the confirmation  (StampFeeOnPaidAsync)
///   4. Organizer submits a settlement with proof image  (SubmitSettlementAsync)
///   5. System admin approves the settlement  (ReviewSettlementAsync)
///   6. Group balance closes: Accrued == Settled, Due == 0
///
/// This stitches together the four services that the unit tests exercise
/// in isolation, proving the whole chain agrees on the fee amount.
/// </summary>
public class ManualPlatformFeeFlowE2ETests
{
    private static byte[] FakeImage(int size = 1024) =>
        Enumerable.Range(0, size).Select(_ => (byte)0xFF).ToArray();

    private sealed class TestInMemoryDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _dbName;
        public TestInMemoryDbContextFactory(string dbName) => _dbName = dbName;
        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
            return new AppDbContext(options);
        }
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
    public async Task FullManualFlow_PlayerPays_AdminConfirms_FeeStamped_SettlementApproved_BalanceCloses()
    {
        // ── Arrange: shared in-memory DB + all four services wired together ──
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AppDbContext(options);
        var factory = new TestInMemoryDbContextFactory(dbName);

        var feeOptions = Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m });
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var uploadSvc = new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance);
        var feeLedger = new PlatformFeeLedgerService(factory, new PlatformFeePolicy(feeOptions));
        var confirmSvc = new AdminConfirmationService(factory, logService, feeLedger, NullLogger<AdminConfirmationService>.Instance);
        var settlementSvc = new PlatformFeeSettlementService(
            factory, NullLogger<PlatformFeeSettlementService>.Instance,
            new UiTextService(new LanguagePreferenceService()));

        // Seed: group (futsal, manual), organizer (group admin), player, event R$15,00
        var group = TestDataFactory.CreateGroup("Racha do Zé");
        group.Sport = Sport.Futsal;
        group.EnablePaymentGateways = false;
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var organizer = TestDataFactory.CreateUserWithPixKey("org-1", "Zé", "pix@ze");
        var player = TestDataFactory.CreateUserWithPixKey("player-1", "João", "pix@joao");
        db.Users.AddRange(organizer, player);
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = organizer.Id, Role = GroupMemberRole.Admin
        });
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = player.Id, Role = GroupMemberRole.Member
        });
        await db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-15 18:00", 15m);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, player);
        conf.Position = FutsalPosition.Outfield;
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var sysAdminId = await SeedSystemAdminAsync(db);

        // ── Step 1: Player uploads Pix proof ──
        var uploadResult = await uploadSvc.UploadProofAsync(conf.Id, FakeImage(), "image/jpeg");
        Assert.True(uploadResult.Success);

        // ── Step 2: Group admin accepts the proof (via AdminConfirmationService) ──
        var toggleResult = await confirmSvc.TogglePaidAsync(conf.Id, organizer.Id);
        Assert.True(toggleResult.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, toggleResult.NewStatus);

        // ── Step 3: Fee is stamped on the confirmation ──
        await using var verifyDb1 = factory.CreateDbContext();
        var stampedConf = verifyDb1.EventConfirmations.Find(conf.Id);
        Assert.Equal(0.75m, stampedConf!.PlatformFeeAmount);

        // ── Step 4: Organizer submits a settlement for the accrued fee ──
        var (accrued, settledBefore, dueBefore) = await feeLedger.GetGroupBalanceAsync(group.Id);
        Assert.Equal(0.75m, accrued);
        Assert.Equal(0m, settledBefore);
        Assert.Equal(0.75m, dueBefore);

        var submitResult = await settlementSvc.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg",
            new[] { evt.Id });
        Assert.True(submitResult.Success);
        Assert.NotNull(submitResult.SettlementId);

        // ── Step 5: System admin approves the settlement ──
        var reviewResult = await settlementSvc.ReviewSettlementAsync(
            submitResult.SettlementId!.Value, sysAdminId, approved: true);
        Assert.True(reviewResult.Success);

        // ── Step 6: Balance closes — accrued == settled, due == 0 ──
        var (accruedAfter, settledAfter, dueAfter) = await feeLedger.GetGroupBalanceAsync(group.Id);
        Assert.Equal(0.75m, accruedAfter);
        Assert.Equal(0.75m, settledAfter);
        Assert.Equal(0m, dueAfter);
    }

    [Fact]
    public async Task FullManualFlow_TwoPlayersSameMatch_FeeIsDoubled_SettlementCoversBoth()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AppDbContext(options);
        var factory = new TestInMemoryDbContextFactory(dbName);

        var feeOptions = Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m });
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var uploadSvc = new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance);
        var feeLedger = new PlatformFeeLedgerService(factory, new PlatformFeePolicy(feeOptions));
        var confirmSvc = new AdminConfirmationService(factory, logService, feeLedger, NullLogger<AdminConfirmationService>.Instance);
        var settlementSvc = new PlatformFeeSettlementService(
            factory, NullLogger<PlatformFeeSettlementService>.Instance,
            new UiTextService(new LanguagePreferenceService()));

        var group = TestDataFactory.CreateGroup("Racha 2");
        group.Sport = Sport.Futsal;
        group.EnablePaymentGateways = false;
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var organizer = TestDataFactory.CreateUserWithPixKey("org-2", "Org2", "pix@org2");
        var p1 = TestDataFactory.CreateUserWithPixKey("p1", "P1", "pix@p1");
        var p2 = TestDataFactory.CreateUserWithPixKey("p2", "P2", "pix@p2");
        db.Users.AddRange(organizer, p1, p2);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = organizer.Id, Role = GroupMemberRole.Admin });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = p1.Id, Role = GroupMemberRole.Member });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = p2.Id, Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-16 18:00", 15m);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var conf1 = TestDataFactory.CreateEventConfirmation(evt, p1);
        conf1.Position = FutsalPosition.Outfield;
        var conf2 = TestDataFactory.CreateEventConfirmation(evt, p2);
        conf2.Position = FutsalPosition.Outfield;
        db.EventConfirmations.AddRange(conf1, conf2);
        await db.SaveChangesAsync();

        var sysAdminId = await SeedSystemAdminAsync(db);

        // Both players upload proof and admin confirms both
        await uploadSvc.UploadProofAsync(conf1.Id, FakeImage(), "image/jpeg");
        await uploadSvc.UploadProofAsync(conf2.Id, FakeImage(), "image/png");
        await confirmSvc.TogglePaidAsync(conf1.Id, organizer.Id);
        await confirmSvc.TogglePaidAsync(conf2.Id, organizer.Id);

        // Fee stamped on both: 2 x 0.75 = 1.50 accrued for this match
        var (accrued, _, due) = await feeLedger.GetGroupBalanceAsync(group.Id);
        Assert.Equal(1.50m, accrued);
        Assert.Equal(1.50m, due);

        // Settlement must cover the full 1.50 for this match
        var submit = await settlementSvc.SubmitSettlementAsync(
            group.Id, organizer.Id, 1.50m, FakeImage(), "image/jpeg",
            new[] { evt.Id });
        Assert.True(submit.Success);

        var review = await settlementSvc.ReviewSettlementAsync(
            submit.SettlementId!.Value, sysAdminId, approved: true);
        Assert.True(review.Success);

        var (_, settledAfter, dueAfter) = await feeLedger.GetGroupBalanceAsync(group.Id);
        Assert.Equal(1.50m, settledAfter);
        Assert.Equal(0m, dueAfter);
    }

    [Fact]
    public async Task FullManualFlow_SettlementRejected_MatchRemainsDue()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AppDbContext(options);
        var factory = new TestInMemoryDbContextFactory(dbName);

        var feeOptions = Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m });
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var uploadSvc = new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance);
        var feeLedger = new PlatformFeeLedgerService(factory, new PlatformFeePolicy(feeOptions));
        var confirmSvc = new AdminConfirmationService(factory, logService, feeLedger, NullLogger<AdminConfirmationService>.Instance);
        var settlementSvc = new PlatformFeeSettlementService(
            factory, NullLogger<PlatformFeeSettlementService>.Instance,
            new UiTextService(new LanguagePreferenceService()));

        var group = TestDataFactory.CreateGroup("Racha 3");
        group.Sport = Sport.Futsal;
        group.EnablePaymentGateways = false;
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var organizer = TestDataFactory.CreateUserWithPixKey("org-3", "Org3", "pix@org3");
        var player = TestDataFactory.CreateUserWithPixKey("p3", "P3", "pix@p3");
        db.Users.AddRange(organizer, player);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = organizer.Id, Role = GroupMemberRole.Admin });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = player.Id, Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-17 18:00", 15m);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, player);
        conf.Position = FutsalPosition.Outfield;
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        var sysAdminId = await SeedSystemAdminAsync(db);

        // Player pays, admin confirms, fee stamped
        await uploadSvc.UploadProofAsync(conf.Id, FakeImage(), "image/jpeg");
        await confirmSvc.TogglePaidAsync(conf.Id, organizer.Id);

        // Organizer submits settlement
        var submit = await settlementSvc.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg",
            new[] { evt.Id });
        Assert.True(submit.Success);

        // System admin rejects with a reason
        var review = await settlementSvc.ReviewSettlementAsync(
            submit.SettlementId!.Value, sysAdminId, approved: false, note: "Comprovante ilegível");
        Assert.True(review.Success);

        // Balance: accrued still 0.75, settled still 0, due still 0.75
        var (accrued, settled, due) = await feeLedger.GetGroupBalanceAsync(group.Id);
        Assert.Equal(0.75m, accrued);
        Assert.Equal(0m, settled);
        Assert.Equal(0.75m, due);

        // The match is still Pendente in the breakdown
        var breakdown = await feeLedger.GetGroupFeeBreakdownByMatchAsync(group.Id);
        var match = Assert.Single(breakdown);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, match.Status);
    }
}
