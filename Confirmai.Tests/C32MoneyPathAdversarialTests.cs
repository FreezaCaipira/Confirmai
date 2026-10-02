using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C32 Fase A — adversarial pen-test of the manual money path: only the gaps
/// not already covered by C33/C34. Each test starts from "what would a
/// malicious user try". Where a test passes on first run it is registered as
/// covered; where it would fail, the fix goes in the service layer.
///
/// Item 4 of the plan (HTTP: /api/pix-proof/{id} and
/// /api/fee-settlement-proof/{id} anonymous / unrelated) is already covered by
/// C33CriticalUseCaseTests (pix-proof: anonymous → no 200, stranger → 403/404,
/// payer → 200, admin → 200; fee-settlement-proof: anonymous, stranger, group
/// admin → denied, sysadmin → 200) — referenced here, not duplicated.
///
/// Related existing coverage: LoadMyPaymentsAsync_OutsiderGetsEmptyList and
/// LoadMyPaymentsAsync_NullUser_GetsEmptyList in GroupPaymentsServiceTests.
/// </summary>
public class C32MoneyPathAdversarialTests
{
    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) Ctx() =>
        TestDataFactory.CreateDbContextWithFactory();

    private static LogService NewLog(IDbContextFactory<AppDbContext> f) =>
        new(f, NullLogger<LogService>.Instance);

    private static PlatformFeeSettlementService NewSettlementSvc(IDbContextFactory<AppDbContext> f) =>
        new(f, NullLogger<PlatformFeeSettlementService>.Instance,
            new UiTextService(new LanguagePreferenceService()), NewLog(f));

    private static GroupPaymentsService NewPaymentsSvc(IDbContextFactory<AppDbContext> f)
    {
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        var notif = new EventNotificationService(f,
            new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>().Object,
            NullLogger<EventNotificationService>.Instance);
        var policy = new PlatformFeePolicy(Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m }));
        return new GroupPaymentsService(f, authMock.Object, notif, NewLog(f),
            new PlatformFeeLedgerService(f, policy), policy, NullLogger<GroupPaymentsService>.Instance);
    }

    private static async Task<(Group group, ApplicationUser organizer)> SeedGroupWithOrganizerAsync(
        AppDbContext db)
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

    private static async Task<string> SeedSystemAdminAsync(AppDbContext db, string userId = "sysadmin-1")
    {
        var role = new IdentityRole("admin") { NormalizedName = "ADMIN" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync();
        return userId;
    }

    private static PlatformFeeSettlement NewSettlement(int groupId, string submitterId,
        PlatformFeeSettlementStatus status) => new()
    {
        GroupId = groupId,
        Amount = 0.75m,
        SubmittedByUserId = submitterId,
        Status = status
    };

    // 1. A group admin (who is NOT a system admin) tries to approve the very
    //    settlement that settles their own group's debt -> refused, batch stays
    //    EmAnalise. Same class as ReviewSettlementAsync_Organizer_CannotSettleOwnDebt.
    [Fact]
    public async Task ReviewSettlementAsync_GroupAdminNotSysadmin_CannotApproveOwnBatch()
    {
        var (db, factory) = Ctx();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(db);
        var settlement = NewSettlement(group.Id, organizer.Id, PlatformFeeSettlementStatus.EmAnalise);
        db.PlatformFeeSettlements.Add(settlement);
        await db.SaveChangesAsync();

        var result = await NewSettlementSvc(factory)
            .ReviewSettlementAsync(settlement.Id, organizer.Id, approved: true);

        Assert.False(result.Success);
        await using var verify = factory.CreateDbContext();
        var saved = await verify.PlatformFeeSettlements.FindAsync(settlement.Id);
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, saved!.Status);
        Assert.Null(saved.ReviewedByUserId);
        Assert.Null(saved.ReviewedAt);
    }

    // 2a. Approving an already-approved batch a second time -> refused, no
    //     second effect (status and original review stamp untouched).
    [Fact]
    public async Task ReviewSettlementAsync_ApproveTwice_Refused_NoSecondEffect()
    {
        var (db, factory) = Ctx();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(db);
        var sysadminId = await SeedSystemAdminAsync(db);
        var firstReviewedAt = DateTime.UtcNow.AddHours(-1);
        var settlement = NewSettlement(group.Id, organizer.Id, PlatformFeeSettlementStatus.Pago);
        settlement.ReviewedByUserId = sysadminId;
        settlement.ReviewedAt = firstReviewedAt;
        db.PlatformFeeSettlements.Add(settlement);
        await db.SaveChangesAsync();

        var result = await NewSettlementSvc(factory)
            .ReviewSettlementAsync(settlement.Id, sysadminId, approved: true);

        Assert.False(result.Success);
        await using var verify = factory.CreateDbContext();
        var saved = await verify.PlatformFeeSettlements.FindAsync(settlement.Id);
        Assert.Equal(PlatformFeeSettlementStatus.Pago, saved!.Status);
        Assert.Equal(sysadminId, saved.ReviewedByUserId);
        Assert.Equal(firstReviewedAt, saved.ReviewedAt);
    }

    // 2b. Approving a batch that was already rejected -> refused; the rejection
    //     stands, no resurrection of the debt payment.
    [Fact]
    public async Task ReviewSettlementAsync_ApproveRejected_Refused_RejectedStands()
    {
        var (db, factory) = Ctx();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(db);
        var sysadminId = await SeedSystemAdminAsync(db);
        var settlement = NewSettlement(group.Id, organizer.Id, PlatformFeeSettlementStatus.Rejeitado);
        settlement.ReviewedByUserId = sysadminId;
        settlement.ReviewedAt = DateTime.UtcNow.AddHours(-1);
        settlement.ReviewNote = "comprovante ilegivel";
        db.PlatformFeeSettlements.Add(settlement);
        await db.SaveChangesAsync();

        var result = await NewSettlementSvc(factory)
            .ReviewSettlementAsync(settlement.Id, sysadminId, approved: true);

        Assert.False(result.Success);
        await using var verify = factory.CreateDbContext();
        var saved = await verify.PlatformFeeSettlements.FindAsync(settlement.Id);
        Assert.Equal(PlatformFeeSettlementStatus.Rejeitado, saved!.Status);
        Assert.Equal("comprovante ilegivel", saved.ReviewNote);
    }

    // 3. A player calls "Meus pagamentos" with the groupId of a group they are
    //    NOT a member of -> empty list, never third-party data.
    [Fact]
    public async Task LoadMyPaymentsAsync_NonMemberGroupId_ReturnsEmpty()
    {
        var (db, factory) = Ctx();
        var (group, _) = await SeedGroupWithOrganizerAsync(db);
        var outsider = new ApplicationUser { Id = "outsider-1", UserName = "Out", FullName = "Out" };
        db.Users.Add(outsider);
        // A paid event + a pending confirmation belonging to someone else in the group.
        var evt = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(1),
            Price = 10m
        };
        db.Events.Add(evt);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evt.Id,
            UserId = "u1",
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield,
            ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await NewPaymentsSvc(factory).LoadMyPaymentsAsync(group.Id, outsider.Id);

        Assert.Empty(result);
    }
}
