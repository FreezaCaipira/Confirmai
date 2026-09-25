using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Confirmai.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Ciclo 34 Fase 2 — audit trail on the money/permission mutation paths:
/// settlement submit/review, pix proof upload/replace, group join flow,
/// and PixKey (receiving data) changes.
/// </summary>
public class C34AuditTrailTests
{
    // ── helpers ─────────────────────────────────────────────────────────

    private static LogService NewLog(IDbContextFactory<AppDbContext> factory)
        => new(factory, NullLogger<LogService>.Instance);

    private static PlatformFeeSettlementService NewSettlementService(IDbContextFactory<AppDbContext> factory)
        => new(factory, NullLogger<PlatformFeeSettlementService>.Instance,
            new UiTextService(new LanguagePreferenceService()), NewLog(factory));

    private static PixProofUploadService NewUploadService(IDbContextFactory<AppDbContext> factory)
        => new(factory, NullLogger<PixProofUploadService>.Instance, NewLog(factory));

    private static GroupDetailService NewGroupDetailService(IDbContextFactory<AppDbContext> factory)
    {
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        var eventSvc = new Confirmai.Services.Futsal.EventDetailService(
            factory, NewLog(factory),
            new Confirmai.Services.Events.EventNotificationService(
                factory, Mock.Of<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(),
                NullLogger<Confirmai.Services.Events.EventNotificationService>.Instance),
            new Confirmai.Services.Payment.PlatformFeePolicy(
                Microsoft.Extensions.Options.Options.Create(
                    new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = 0.75m })));
        return new GroupDetailService(factory, authMock.Object, NewLog(factory), eventSvc);
    }

    private static byte[] FakeImage(int size = 1024)
    {
        var bytes = Enumerable.Range(0, size).Select(_ => (byte)0xFF).ToArray();
        // JPEG signature so the bytes pass ImageSignatureValidator
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
        return bytes;
    }

    private static async Task<(Group group, ApplicationUser organizer)> SeedGroupWithOrganizerAsync(AppDbContext db)
    {
        var group = TestDataFactory.CreateGroup("Racha", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        db.Groups.Add(group);
        var organizer = TestDataFactory.CreateUserWithPixKey("org-1", "Org", "org@test");
        db.Users.Add(organizer);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = organizer.Id, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();
        return (group, organizer);
    }

    private static async Task<Event> SeedMatchWithFeeAsync(AppDbContext db, Group group, decimal fee = 0.75m)
    {
        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 15m);
        db.Events.Add(evt);
        var player = TestDataFactory.CreateUserWithPixKey("p-1", "J1", "p1@test");
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

    private static async Task<ApplicationUser> SeedSysAdminAsync(AppDbContext db)
    {
        var role = new IdentityRole { Name = "admin", NormalizedName = "ADMIN" };
        db.Roles.Add(role);
        var admin = TestDataFactory.CreateUserWithPixKey("admin-1", "Admin", "admin@test");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        return admin;
    }

    // ── settlement ──────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitSettlement_WritesSettlementSubmittedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = await SeedMatchWithFeeAsync(ctx.db, group);

        var svc = NewSettlementService(ctx.factory);
        var result = await svc.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { evt.Id });

        Assert.True(result.Success);
        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.SettlementSubmitted);
        Assert.Equal(AuditEntities.PlatformFeeSettlement, audit.EntityType);
        Assert.Equal(result.SettlementId.ToString(), audit.EntityId);
        Assert.Equal(organizer.Id, audit.UserId);
    }

    [Fact]
    public async Task ReviewSettlement_Approve_WritesSettlementConfirmedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = await SeedMatchWithFeeAsync(ctx.db, group);
        var admin = await SeedSysAdminAsync(ctx.db);
        var svc = NewSettlementService(ctx.factory);
        var submit = await svc.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { evt.Id });

        var result = await svc.ReviewSettlementAsync(submit.SettlementId!.Value, admin.Id, approved: true);

        Assert.True(result.Success);
        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.SettlementConfirmed);
        Assert.Equal(submit.SettlementId.ToString(), audit.EntityId);
        Assert.Equal(admin.Id, audit.UserId);
    }

    [Fact]
    public async Task ReviewSettlement_Reject_WritesSettlementRejectedAuditWithReason()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, organizer) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = await SeedMatchWithFeeAsync(ctx.db, group);
        var admin = await SeedSysAdminAsync(ctx.db);
        var svc = NewSettlementService(ctx.factory);
        var submit = await svc.SubmitSettlementAsync(
            group.Id, organizer.Id, 0.75m, FakeImage(), "image/jpeg", new[] { evt.Id });

        var result = await svc.ReviewSettlementAsync(
            submit.SettlementId!.Value, admin.Id, approved: false, note: "valor incorreto");

        Assert.True(result.Success);
        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.SettlementRejected);
        Assert.Equal(admin.Id, audit.UserId);
        Assert.Contains("valor incorreto", audit.Message);
    }

    // ── pix proof ───────────────────────────────────────────────────────

    [Fact]
    public async Task UploadProof_FirstUpload_WritesProofUploadedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 15m);
        ctx.db.Events.Add(evt);
        var player = TestDataFactory.CreateUserWithPixKey("p-1", "J1", "p1@test");
        ctx.db.Users.Add(player);
        await ctx.db.SaveChangesAsync();
        var conf = TestDataFactory.CreateEventConfirmation(evt, player);
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var svc = NewUploadService(ctx.factory);
        var result = await svc.UploadProofAsync(conf.Id, FakeImage(), "image/jpeg", player.Id);

        Assert.True(result.Success);
        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.ProofUploaded);
        Assert.Equal(AuditEntities.EventConfirmation, audit.EntityType);
        Assert.Equal(conf.Id.ToString(), audit.EntityId);
        Assert.Equal(player.Id, audit.UserId);
    }

    [Fact]
    public async Task UploadProof_SecondUpload_WritesProofReplacedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _) = await SeedGroupWithOrganizerAsync(ctx.db);
        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 15m);
        ctx.db.Events.Add(evt);
        var player = TestDataFactory.CreateUserWithPixKey("p-1", "J1", "p1@test");
        ctx.db.Users.Add(player);
        await ctx.db.SaveChangesAsync();
        var conf = TestDataFactory.CreateEventConfirmation(evt, player);
        conf.PixProofImageData = FakeImage(64);
        conf.PixProofContentType = "image/jpeg";
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var svc = NewUploadService(ctx.factory);
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var result = await svc.UploadProofAsync(conf.Id, pngBytes, "image/png", player.Id);

        Assert.True(result.Success);
        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.ProofReplaced);
        Assert.Equal(conf.Id.ToString(), audit.EntityId);
        Assert.Empty(ctx.db.Logs.Where(l => l.EventType == AuditEvents.ProofUploaded));
    }

    // ── group join ──────────────────────────────────────────────────────

    [Fact]
    public async Task RequestToJoin_WritesGroupJoinRequestedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Grupo", enablePaymentGateways: false);
        ctx.db.Groups.Add(group);
        var user = TestDataFactory.CreateUserWithPixKey("u-1", "U1", "u1@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var svc = NewGroupDetailService(ctx.factory);
        await svc.RequestToJoinAsync(group.Id, user.Id);

        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.GroupJoinRequested);
        Assert.Equal(AuditEntities.GroupJoinRequest, audit.EntityType);
        Assert.Equal(user.Id, audit.UserId);
    }

    [Fact]
    public async Task ApproveRequest_WritesGroupJoinApprovedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Grupo", enablePaymentGateways: false);
        ctx.db.Groups.Add(group);
        var user = TestDataFactory.CreateUserWithPixKey("u-1", "U1", "u1@test");
        var admin = TestDataFactory.CreateUserWithPixKey("gadmin", "GA", "ga@test");
        ctx.db.Users.AddRange(user, admin);
        ctx.db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = admin.Id,
            Role = GroupMemberRole.Admin, CreatedAt = DateTime.UtcNow,
        });
        await ctx.db.SaveChangesAsync();
        var req = new GroupJoinRequest
        {
            GroupId = group.Id, UserId = user.Id,
            RequestedAt = DateTime.UtcNow, Status = JoinRequestStatus.Pending,
        };
        ctx.db.GroupJoinRequests.Add(req);
        await ctx.db.SaveChangesAsync();

        var svc = NewGroupDetailService(ctx.factory);
        await svc.ApproveRequestAsync(req.Id, admin.Id, group.Name);

        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.GroupJoinApproved);
        Assert.Equal(req.Id.ToString(), audit.EntityId);
        Assert.Equal(admin.Id, audit.UserId);
    }

    [Fact]
    public async Task RejectRequest_WritesGroupJoinRejectedAudit()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Grupo", enablePaymentGateways: false);
        ctx.db.Groups.Add(group);
        var user = TestDataFactory.CreateUserWithPixKey("u-1", "U1", "u1@test");
        var admin = TestDataFactory.CreateUserWithPixKey("gadmin", "GA", "ga@test");
        ctx.db.Users.AddRange(user, admin);
        ctx.db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = admin.Id,
            Role = GroupMemberRole.Admin, CreatedAt = DateTime.UtcNow,
        });
        await ctx.db.SaveChangesAsync();
        var req = new GroupJoinRequest
        {
            GroupId = group.Id, UserId = user.Id,
            RequestedAt = DateTime.UtcNow, Status = JoinRequestStatus.Pending,
        };
        ctx.db.GroupJoinRequests.Add(req);
        await ctx.db.SaveChangesAsync();

        var svc = NewGroupDetailService(ctx.factory);
        await svc.RejectRequestAsync(req.Id, admin.Id);

        var audit = ctx.db.Logs.Single(l => l.EventType == AuditEvents.GroupJoinRejected);
        Assert.Equal(req.Id.ToString(), audit.EntityId);
        Assert.Equal(admin.Id, audit.UserId);
    }

    // ── profile / receiving data ────────────────────────────────────────

    private static (IDbContextFactory<AppDbContext> factory, ProfileService svc) NewProfileService()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"c34-profile-{Guid.NewGuid()}");
        var store = new Mock<IUserStore<ApplicationUser>>();
        store.Setup(x => x.GetUserIdAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser u, CancellationToken _) => u.Id);
        store.Setup(x => x.GetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser u, CancellationToken _) => u.UserName);
        store.Setup(x => x.GetNormalizedUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser u, CancellationToken _) => u.NormalizedUserName);
        store.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        store.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);
        var userManager = new UserManager<ApplicationUser>(
            store.Object, null!, new PasswordHasher<ApplicationUser>(),
            null!, null!, null!, null!, null!, null!);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(x => x.WebRootPath).Returns(Path.GetTempPath());
        var svc = new ProfileService(factory, userManager, envMock.Object, NewLog(factory));
        return (factory, svc);
    }

    [Fact]
    public async Task SaveOwnProfile_PixKeyChanged_WritesUserProfileUpdatedAudit()
    {
        var (factory, svc) = NewProfileService();
        var user = new ApplicationUser
        {
            Id = "u-1", UserName = "u1", NormalizedUserName = "U1", PixKey = "old-pix",
        };

        var (succeeded, pixChanged) = await svc.SaveOwnProfileAsync(user, null, null, "new-pix");

        Assert.True(succeeded);
        Assert.True(pixChanged);
        await using var db = factory.CreateDbContext();
        var audit = db.Logs.Single(l => l.EventType == AuditEvents.UserProfileUpdated);
        Assert.Equal(AuditEntities.User, audit.EntityType);
        Assert.Equal("u-1", audit.UserId);
    }

    [Fact]
    public async Task SaveOwnProfile_PixKeyUnchanged_NoAudit()
    {
        var (factory, svc) = NewProfileService();
        var user = new ApplicationUser
        {
            Id = "u-1", UserName = "u1", NormalizedUserName = "U1", PixKey = "same-pix",
        };

        var (succeeded, pixChanged) = await svc.SaveOwnProfileAsync(user, "insta", "disc", "same-pix");

        Assert.True(succeeded);
        Assert.False(pixChanged);
        await using var db = factory.CreateDbContext();
        Assert.Empty(db.Logs.Where(l => l.EventType == AuditEvents.UserProfileUpdated));
    }
}
