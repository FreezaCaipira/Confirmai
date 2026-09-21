using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C34 Fase 3 — adversarial tests on the manual money path.
/// Every mutation must verify the caller's role in the service layer;
/// a UI guard alone is not an authorization boundary.
/// </summary>
public class C34SecurityAdversarialTests
{
    private static readonly byte[] JpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0 };
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) Ctx() =>
        TestDataFactory.CreateDbContextWithFactory();

    private static LogService NewLog(IDbContextFactory<AppDbContext> f) =>
        new(f, NullLogger<LogService>.Instance);

    private static AdminConfirmationService NewAdminSvc(IDbContextFactory<AppDbContext> f) =>
        new(f, NewLog(f));

    private static EventDetailService NewEventSvc(IDbContextFactory<AppDbContext> f) =>
        new(f, NewLog(f),
            new EventNotificationService(f, new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>().Object,
                NullLogger<EventNotificationService>.Instance),
            new PlatformFeePolicy(Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m })));

    private static GroupPaymentsService NewPaymentsSvc(IDbContextFactory<AppDbContext> f, string? authUserId = "admin-1")
    {
        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = authUserId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, authUserId) }, "Test")
            : new ClaimsIdentity();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));
        var emailMock = new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
        var notif = new EventNotificationService(f, emailMock.Object, NullLogger<EventNotificationService>.Instance);
        var feeOptions = Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m });
        var policy = new PlatformFeePolicy(feeOptions);
        return new GroupPaymentsService(f, authMock.Object, notif, NewLog(f),
            new PlatformFeeLedgerService(f, policy), policy, NullLogger<GroupPaymentsService>.Instance);
    }

    /// <summary>Group + Event + admin member + a pending confirmation for "payer-1".</summary>
    private static async Task<(Group group, Event evt, EventConfirmation conf)> SeedMoneyPathAsync(
        AppDbContext db, string adminId = "admin-1", string payerId = "payer-1")
    {
        var (group, evt) = await TestDataFactory.SeedEventWithAdminAsync(db, adminId);
        db.Users.Add(new ApplicationUser { Id = payerId, UserName = payerId, FullName = "Payer" });
        var conf = new EventConfirmation
        {
            EventId = evt.Id,
            UserId = payerId,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            HasPaid = false,
            Position = FutsalPosition.Outfield,
            ConfirmedAt = DateTime.UtcNow
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();
        return (group, evt, conf);
    }

    // ── AdminConfirmationService.TogglePaidAsync ─────────────────────────────

    [Fact]
    public async Task TogglePaidAsync_Stranger_CannotMarkPaid()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);

        var result = await NewAdminSvc(factory).TogglePaidAsync(conf.Id, "stranger-1");

        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.Forbidden, result.DenyReason);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.EventConfirmations.FindAsync(conf.Id);
        Assert.False(saved!.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Pending, saved.PaymentStatus);
        Assert.Null(saved.MarkedPaidByUserId);
    }

    [Fact]
    public async Task TogglePaidAsync_RegularMember_CannotMarkPaid()
    {
        var (db, factory) = Ctx();
        var (group, _, conf) = await SeedMoneyPathAsync(db);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = "member-1", Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();

        var result = await NewAdminSvc(factory).TogglePaidAsync(conf.Id, "member-1");

        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.Forbidden, result.DenyReason);
    }

    [Fact]
    public async Task TogglePaidAsync_AdminOfOtherGroup_CannotMarkPaid()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);
        // "other-admin" is admin of a DIFFERENT group — must not touch this one.
        var otherGroup = new Group { Name = "Outro" };
        db.Groups.Add(otherGroup);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { GroupId = otherGroup.Id, UserId = "other-admin", Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();

        var result = await NewAdminSvc(factory).TogglePaidAsync(conf.Id, "other-admin");

        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.Forbidden, result.DenyReason);

        await using var verify = factory.CreateDbContext();
        Assert.False((await verify.EventConfirmations.FindAsync(conf.Id))!.HasPaid);
    }

    [Fact]
    public async Task TogglePaidAsync_GroupAdmin_CanMarkPaid()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);

        var result = await NewAdminSvc(factory).TogglePaidAsync(conf.Id, "admin-1");

        Assert.True(result.Updated);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, result.NewStatus);
    }

    [Fact]
    public async Task RemoveConfirmationAsync_Stranger_CannotRemove()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);

        var result = await NewAdminSvc(factory).RemoveConfirmationAsync(conf.Id, "stranger-1");

        Assert.True(result.Found);
        Assert.False(result.Updated);
        Assert.Equal(AdminMutationDenyReason.Forbidden, result.DenyReason);

        await using var verify = factory.CreateDbContext();
        Assert.Equal(1, verify.EventConfirmations.Count());
    }

    [Fact]
    public async Task RemoveConfirmationAsync_GroupAdmin_CanRemove()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);

        var result = await NewAdminSvc(factory).RemoveConfirmationAsync(conf.Id, "admin-1");

        Assert.True(result.Updated);
        await using var verify = factory.CreateDbContext();
        Assert.Equal(0, verify.EventConfirmations.Count());
    }

    // ── EventDetailService.AdminRemoveConfirmationAsync ──────────────────────

    [Fact]
    public async Task AdminRemoveConfirmation_Stranger_NoOp()
    {
        var (db, factory) = Ctx();
        var (_, evt, conf) = await SeedMoneyPathAsync(db);

        await NewEventSvc(factory).AdminRemoveConfirmationAsync(conf.Id, "stranger-1", evt.Id);

        await using var verify = factory.CreateDbContext();
        Assert.NotNull(await verify.EventConfirmations.FindAsync(conf.Id));
    }

    [Fact]
    public async Task AdminRemoveConfirmation_GroupAdmin_Removes()
    {
        var (db, factory) = Ctx();
        var (_, evt, conf) = await SeedMoneyPathAsync(db);

        await NewEventSvc(factory).AdminRemoveConfirmationAsync(conf.Id, "admin-1", evt.Id);

        await using var verify = factory.CreateDbContext();
        Assert.Null(await verify.EventConfirmations.FindAsync(conf.Id));
    }

    // ── GroupPaymentsService.MarkPaidAsync / RejectProofAsync ────────────────

    [Fact]
    public async Task MarkPaidAsync_Stranger_NoOp()
    {
        var (db, factory) = Ctx();
        var (group, _, conf) = await SeedMoneyPathAsync(db);

        await NewPaymentsSvc(factory).MarkPaidAsync(conf.Id, "payer-1", "stranger-1", group.Id);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.EventConfirmations.FindAsync(conf.Id);
        Assert.False(saved!.HasPaid);
    }

    [Fact]
    public async Task MarkPaidAsync_CrossGroupAdmin_CannotTouchForeignConfirmation()
    {
        var (db, factory) = Ctx();
        var (group, _, conf) = await SeedMoneyPathAsync(db);
        // "foreign-admin" administers another group and passes ITS groupId —
        // the confirmation belongs to `group`, so both checks must deny.
        var foreignGroup = new Group { Name = "B" };
        db.Groups.Add(foreignGroup);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { GroupId = foreignGroup.Id, UserId = "foreign-admin", Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();

        // Case A: admin of foreign group, foreign groupId — admin check fails
        await NewPaymentsSvc(factory).MarkPaidAsync(conf.Id, "payer-1", "foreign-admin", foreignGroup.Id);
        // Case B: admin of THIS group but wrong groupId — conf∉groupId fails
        await NewPaymentsSvc(factory).MarkPaidAsync(conf.Id, "payer-1", "admin-1", foreignGroup.Id);

        await using var verify = factory.CreateDbContext();
        Assert.False((await verify.EventConfirmations.FindAsync(conf.Id))!.HasPaid);
    }

    [Fact]
    public async Task RejectProofAsync_Stranger_ProofPreserved()
    {
        var (db, factory) = Ctx();
        var (group, _, conf) = await SeedMoneyPathAsync(db);
        conf.PixProofImageData = JpegBytes;
        conf.PixProofContentType = "image/jpeg";
        conf.PixProofUploadedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await NewPaymentsSvc(factory).RejectProofAsync(conf.Id, "stranger-1", group.Id);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.EventConfirmations.FindAsync(conf.Id);
        Assert.NotNull(saved!.PixProofImageData);
        Assert.NotNull(saved.PixProofUploadedAt);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_Stranger_SendsNothing()
    {
        var (db, factory) = Ctx();
        var (group, _, _) = await SeedMoneyPathAsync(db);
        var d = new UserDelinquency("payer-1", "Payer", new List<DelinquencyEntry>
        {
            new(1, 1, DateTime.UtcNow, 10m, "/futsal/1", false)
        });

        await NewPaymentsSvc(factory).NotifyDelinquencyAsync(d, "stranger-1", group.Name, group.Id);

        await using var verify = factory.CreateDbContext();
        Assert.Equal(0, verify.UserMailboxMessages.Count());
    }

    // ── PixProofUploadService — ownership + signature ────────────────────────

    [Fact]
    public async Task UploadProofAsync_NonOwner_CannotOverwriteProof()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);
        conf.PixProofImageData = JpegBytes;
        conf.PixProofContentType = "image/jpeg";
        conf.PixProofUploadedAt = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        var originalStamp = conf.PixProofUploadedAt;

        var svc = new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance, NewLog(factory));
        var result = await svc.UploadProofAsync(conf.Id, PngBytes, "image/png", "intruder-1");

        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.Forbidden, result.Error);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.EventConfirmations.FindAsync(conf.Id);
        Assert.Equal(JpegBytes, saved!.PixProofImageData);
        Assert.Equal("image/jpeg", saved.PixProofContentType);
        Assert.Equal(originalStamp, saved.PixProofUploadedAt);
    }

    [Fact]
    public async Task UploadProofAsync_ExeDisguisedAsJpeg_Rejected()
    {
        var (db, factory) = Ctx();
        var (_, _, conf) = await SeedMoneyPathAsync(db);

        var svc = new PixProofUploadService(factory, NullLogger<PixProofUploadService>.Instance, NewLog(factory));
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 }; // MZ header
        var result = await svc.UploadProofAsync(conf.Id, exeBytes, "image/jpeg", "payer-1");

        Assert.False(result.Success);
        Assert.Equal(PixProofUploadError.InvalidImage, result.Error);

        await using var verify = factory.CreateDbContext();
        Assert.Null((await verify.EventConfirmations.FindAsync(conf.Id))!.PixProofImageData);
    }

    // ── SubmitSettlementAsync — magic bytes ──────────────────────────────────

    [Fact]
    public async Task SubmitSettlementAsync_ForgedContentType_Rejected()
    {
        var (db, factory) = Ctx();
        var (group, evt, conf) = await SeedMoneyPathAsync(db);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = 0.75m;
        await db.SaveChangesAsync();

        var svc = new PlatformFeeSettlementService(
            factory, NullLogger<PlatformFeeSettlementService>.Instance,
            new UiTextService(new LanguagePreferenceService()), NewLog(factory));

        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        var result = await svc.SubmitSettlementAsync(
            group.Id, "admin-1", 0.75m, exeBytes, "image/png", new[] { evt.Id });

        Assert.False(result.Success);
        await using var verify = factory.CreateDbContext();
        Assert.Equal(0, verify.PlatformFeeSettlements.Count());
    }
}
