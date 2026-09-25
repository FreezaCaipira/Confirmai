using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

public class GroupDetailServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, GroupDetailService svc) Setup(string? userId = null)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"gdetail-{Guid.NewGuid()}");
        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = userId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")
            : new ClaimsIdentity();
        authMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var notificationService = new Confirmai.Services.Events.EventNotificationService(
            factory, Mock.Of<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(),
            NullLogger<Confirmai.Services.Events.EventNotificationService>.Instance);
        var eventSvc = new Confirmai.Services.Futsal.EventDetailService(
            factory, logService, notificationService,
            new Confirmai.Services.Payment.PlatformFeePolicy(
                Microsoft.Extensions.Options.Options.Create(
                    new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = 0.75m })));
        var svc = new GroupDetailService(factory, authMock.Object, logService, eventSvc);
        return (factory, svc);
    }

    private static async Task<(IDbContextFactory<AppDbContext> factory, GroupDetailService svc, int groupId)> SetupWithGroupAsync(string creatorId = "creator-1")
    {
        var (factory, svc) = Setup();
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "Test Group", InviteCode = "ABC123", CreatedByUserId = creatorId };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = "admin-1",
            Role = GroupMemberRole.Admin, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return (factory, svc, group.Id);
    }

    // ── GetCurrentUserIdAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserIdAsync_ReturnsUserId_WhenAuthenticated()
    {
        var (factory, svc) = Setup("user-1");
        var userId = await svc.GetCurrentUserIdAsync();
        Assert.Equal("user-1", userId);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ReturnsNull_WhenNotAuthenticated()
    {
        var (factory, svc) = Setup();
        var userId = await svc.GetCurrentUserIdAsync();
        Assert.Null(userId);
    }

    // ── LoadAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_ReturnsGroup_WhenFound()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        var data = await svc.LoadAsync(groupId, "user-1");

        Assert.NotNull(data.Group);
        Assert.Equal("Test Group", data.Group!.Name);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullGroup_WhenNotFound()
    {
        var (factory, svc) = Setup();

        var data = await svc.LoadAsync(999, "user-1");

        Assert.Null(data.Group);
    }

    [Fact]
    public async Task LoadAsync_ReturnsPendingRequests_WhenExist()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        db.GroupJoinRequests.Add(new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadAsync(groupId, "admin-1");

        Assert.NotEmpty(data.PendingRequests);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyPendingRequests_WhenNoneExist()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        var data = await svc.LoadAsync(groupId, "admin-1");

        Assert.Empty(data.PendingRequests);
    }

    [Fact]
    public async Task LoadAsync_ReturnsUserJoinRequest_WhenUserHasRequested()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        db.GroupJoinRequests.Add(new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadAsync(groupId, "u1");

        Assert.NotNull(data.UserJoinRequest);
    }

    // ── RequestToJoinAsync ────────────────────────────────────────────

    [Fact]
    public async Task RequestToJoinAsync_CreatesRequest_WhenNotAlreadyPending()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        await svc.RequestToJoinAsync(groupId, "u1");

        await using var db = factory.CreateDbContext();
        var req = await db.GroupJoinRequests.FirstOrDefaultAsync(r => r.GroupId == groupId && r.UserId == "u1");
        Assert.NotNull(req);
        Assert.Equal(JoinRequestStatus.Pending, req!.Status);
    }

    [Fact]
    public async Task RequestToJoinAsync_DoesNotDuplicate_WhenAlreadyPending()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await svc.RequestToJoinAsync(groupId, "u1");

        await svc.RequestToJoinAsync(groupId, "u1");

        await using var db = factory.CreateDbContext();
        var count = await db.GroupJoinRequests.CountAsync(r => r.GroupId == groupId && r.UserId == "u1");
        Assert.Equal(1, count);
    }

    // ── CancelJoinRequestAsync ────────────────────────────────────────

    [Fact]
    public async Task CancelJoinRequestAsync_RemovesRequest_WhenPending()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        var req = new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();

        await svc.CancelJoinRequestAsync(req.Id, "u1");

        await using var db2 = factory.CreateDbContext();
        var deleted = await db2.GroupJoinRequests.FindAsync(req.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task CancelJoinRequestAsync_DoesNothing_WhenRequestNotFound()
    {
        var (factory, svc) = Setup();
        await svc.CancelJoinRequestAsync(999, "u1");
    }

    // ── JoinWithCodeAsync ─────────────────────────────────────────────

    [Fact]
    public async Task JoinWithCodeAsync_Succeeds_WhenCodeMatches()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        var result = await svc.JoinWithCodeAsync(groupId, "u1", "ABC123");

        Assert.Equal(JoinWithCodeResult.Joined, result);
    }

    [Fact]
    public async Task JoinWithCodeAsync_AddsMember_WhenCodeMatches()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        await svc.JoinWithCodeAsync(groupId, "u1", "ABC123");

        await using var db = factory.CreateDbContext();
        var member = await db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == "u1");
        Assert.NotNull(member);
    }

    [Fact]
    public async Task JoinWithCodeAsync_Fails_WhenCodeDoesNotMatch()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        var result = await svc.JoinWithCodeAsync(groupId, "u1", "WRONG");

        Assert.Equal(JoinWithCodeResult.InvalidCode, result);
    }

    [Fact]
    public async Task JoinWithCodeAsync_Fails_WhenGroupNotFound()
    {
        var (factory, svc) = Setup();

        var result = await svc.JoinWithCodeAsync(999, "u1", "ABC123");

        Assert.Equal(JoinWithCodeResult.InvalidCode, result);
    }

    [Fact]
    public async Task JoinWithCodeAsync_ReturnsAlreadyMember_WhenAlreadyMember()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await svc.JoinWithCodeAsync(groupId, "u1", "ABC123");

        var result = await svc.JoinWithCodeAsync(groupId, "u1", "ABC123");

        Assert.Equal(JoinWithCodeResult.AlreadyMember, result);
        await using var db = factory.CreateDbContext();
        var count = await db.GroupMembers.CountAsync(m => m.GroupId == groupId && m.UserId == "u1");
        Assert.Equal(1, count);
    }

    // ── ApproveRequestAsync ───────────────────────────────────────────

    [Fact]
    public async Task ApproveRequestAsync_AddsMember_AndSetsApproved()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        var req = new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();

        await svc.ApproveRequestAsync(req.Id, "admin-1", "Test Group");

        await using var db2 = factory.CreateDbContext();
        var dbReq = await db2.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Approved, dbReq!.Status);
        var member = await db2.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == "u1");
        Assert.NotNull(member);
    }

    [Fact]
    public async Task ApproveRequestAsync_SendsMailboxMessage()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        var req = new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();

        await svc.ApproveRequestAsync(req.Id, "admin-1", "Test Group");

        await using var db2 = factory.CreateDbContext();
        var msg = await db2.UserMailboxMessages.FirstOrDefaultAsync(m => m.RecipientUserId == "u1");
        Assert.NotNull(msg);
        Assert.Contains("aprovada", msg!.Body);
    }

    [Fact]
    public async Task ApproveRequestAsync_DoesNothing_WhenRequestNotFound()
    {
        var (factory, svc) = Setup();
        await svc.ApproveRequestAsync(999, "admin-1", "Test Group");
    }

    // ── RejectRequestAsync ────────────────────────────────────────────

    [Fact]
    public async Task RejectRequestAsync_SetsRejectedStatus()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        var req = new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();

        await svc.RejectRequestAsync(req.Id, "admin-1");

        await using var db2 = factory.CreateDbContext();
        var dbReq = await db2.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Rejected, dbReq!.Status);
    }

    [Fact]
    public async Task RejectRequestAsync_DoesNothing_WhenRequestNotFound()
    {
        var (factory, svc) = Setup();
        await svc.RejectRequestAsync(999, "admin-1");
    }

    // ── ApproveSelectedAsync ──────────────────────────────────────────

    [Fact]
    public async Task ApproveSelectedAsync_ApprovesAllPendingRequests()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "User2" });
        var req1 = new GroupJoinRequest { GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow };
        var req2 = new GroupJoinRequest { GroupId = groupId, UserId = "u2", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow };
        db.GroupJoinRequests.AddRange(req1, req2);
        await db.SaveChangesAsync();

        await svc.ApproveSelectedAsync(new HashSet<int> { req1.Id, req2.Id }, "admin-1", "Test Group");

        await using var db2 = factory.CreateDbContext();
        var approved = await db2.GroupJoinRequests.Where(r => r.Status == JoinRequestStatus.Approved).ToListAsync();
        Assert.Equal(2, approved.Count);
    }

    // ── RejectSelectedAsync ───────────────────────────────────────────

    [Fact]
    public async Task RejectSelectedAsync_RejectsAllPendingRequests()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "User2" });
        var req1 = new GroupJoinRequest { GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow };
        var req2 = new GroupJoinRequest { GroupId = groupId, UserId = "u2", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow };
        db.GroupJoinRequests.AddRange(req1, req2);
        await db.SaveChangesAsync();

        await svc.RejectSelectedAsync(new HashSet<int> { req1.Id, req2.Id }, "admin-1");

        await using var db2 = factory.CreateDbContext();
        var rejected = await db2.GroupJoinRequests.Where(r => r.Status == JoinRequestStatus.Rejected).ToListAsync();
        Assert.Equal(2, rejected.Count);
    }

    // ── GetMyPendingRequestsAsync (C36-D Fase 4) ─────────────────────

    [Fact]
    public async Task GetMyPendingRequestsAsync_ReturnsOnlyOwnPending_WithGroup()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "User1" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "User2" });
        db.GroupJoinRequests.AddRange(
            new GroupJoinRequest { GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow },
            new GroupJoinRequest { GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Rejected, RequestedAt = DateTime.UtcNow },
            new GroupJoinRequest { GroupId = groupId, UserId = "u2", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var mine = await svc.GetMyPendingRequestsAsync("u1");

        Assert.Single(mine);
        Assert.Equal("u1", mine[0].UserId);
        Assert.Equal("Test Group", mine[0].Group.Name);
    }

    // ── CancelJoinRequestAsync ownership (C36-D Fase 4) ──────────────

    [Fact]
    public async Task CancelJoinRequestAsync_DoesNothing_WhenRequestBelongsToAnotherUser()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        var req = new GroupJoinRequest
        {
            GroupId = groupId, UserId = "u1", Status = JoinRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();

        await svc.CancelJoinRequestAsync(req.Id, "u2");

        await using var db2 = factory.CreateDbContext();
        Assert.NotNull(await db2.GroupJoinRequests.FindAsync(req.Id));
    }

    // ── LeaveGroupAsync (C36-D Fase 4) ───────────────────────────────

    [Fact]
    public async Task LeaveGroupAsync_RemovesMembership_ForRegularMember()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId, UserId = "u1", Role = GroupMemberRole.Member,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
        await using var db2 = factory.CreateDbContext();
        Assert.False(await db2.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "u1"));
        Assert.True(await db2.Logs.AnyAsync(l => l.EventType == AuditEvents.GroupMemberRemoved));
    }

    [Fact]
    public async Task LeaveGroupAsync_ReturnsNotMember_WhenUserIsNotInGroup()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        var result = await svc.LeaveGroupAsync(groupId, "outsider");

        Assert.Equal(LeaveGroupResult.NotMember, result);
    }

    [Fact]
    public async Task LeaveGroupAsync_BlocksSoleAdmin()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();

        var result = await svc.LeaveGroupAsync(groupId, "admin-1");

        Assert.Equal(LeaveGroupResult.SoleAdmin, result);
        await using var db = factory.CreateDbContext();
        Assert.True(await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "admin-1"));
    }

    [Fact]
    public async Task LeaveGroupAsync_AllowsAdmin_WhenAnotherAdminExists()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await using var db = factory.CreateDbContext();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId, UserId = "admin-2", Role = GroupMemberRole.Admin,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await svc.LeaveGroupAsync(groupId, "admin-1");

        Assert.Equal(LeaveGroupResult.Left, result);
        await using var db2 = factory.CreateDbContext();
        Assert.False(await db2.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "admin-1"));
        Assert.True(await db2.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "admin-2"));
    }

    // ── LeaveGroupAsync vs. debt (review C36-D ressalva 1) ────────────

    private static async Task<int> SeedEventAsync(
        IDbContextFactory<AppDbContext> factory, int groupId,
        DateTime startsAt, decimal? price, bool isActive = true)
    {
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, StartsAt = startsAt,
            DurationMinutes = 120, MaxPlayers = 10, Price = price,
            IsActive = isActive, CreatedByUserId = "creator-1",
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    private static async Task SeedMemberAsync(
        IDbContextFactory<AppDbContext> factory, int groupId, string userId,
        GroupMemberRole role = GroupMemberRole.Member)
    {
        await using var db = factory.CreateDbContext();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId, UserId = userId, Role = role,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedConfirmationAsync(
        IDbContextFactory<AppDbContext> factory, int eventId, string userId,
        bool hasPaid = false,
        EventConfirmationPaymentStatus status = EventConfirmationPaymentStatus.Pending,
        FutsalPosition? position = FutsalPosition.Outfield)
    {
        await using var db = factory.CreateDbContext();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = eventId, UserId = userId, Position = position,
            ConfirmedAt = DateTime.UtcNow, HasPaid = hasPaid,
            PaymentStatus = status,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task LeaveGroupAsync_BlocksPendingPayment_OnPastPricedEvent()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(-1), price: 15m);
        await SeedConfirmationAsync(factory, eventId, "u1");

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.PendingPayment, result);
        await using var db = factory.CreateDbContext();
        Assert.True(await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "u1"));
    }

    [Fact]
    public async Task LeaveGroupAsync_BlocksPendingPayment_OnFuturePricedEvent()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(1), price: 15m);
        await SeedConfirmationAsync(factory, eventId, "u1");

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.PendingPayment, result);
        await using var db = factory.CreateDbContext();
        Assert.True(await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "u1"));
    }

    [Fact]
    public async Task LeaveGroupAsync_Allows_WhenPastEventIsPaid()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(-1), price: 15m);
        await SeedConfirmationAsync(factory, eventId, "u1",
            hasPaid: true, status: EventConfirmationPaymentStatus.Paid);

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
        await using var db = factory.CreateDbContext();
        Assert.False(await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == "u1"));
        // The paid record is financial history — it is never deleted.
        Assert.True(await db.EventConfirmations.AnyAsync(c => c.EventId == eventId && c.UserId == "u1"));
    }

    [Fact]
    public async Task LeaveGroupAsync_Allows_GoalkeeperUnpaid_OnPricedEvent()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(-1), price: 15m);
        await SeedConfirmationAsync(factory, eventId, "u1",
            position: FutsalPosition.Goalkeeper);

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
    }

    [Fact]
    public async Task LeaveGroupAsync_Allows_WhenEventWasCancelled()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(-1), price: 15m, isActive: false);
        await SeedConfirmationAsync(factory, eventId, "u1");

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
    }

    [Fact]
    public async Task LeaveGroupAsync_CancelsFutureUnpaidConfirmations_AndPromotesWaitlist()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        await SeedMemberAsync(factory, groupId, "u2");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(1), price: null);
        await SeedConfirmationAsync(factory, eventId, "u1");
        await using (var db = factory.CreateDbContext())
        {
            db.WaitingLists.Add(new WaitingList
            {
                EventId = eventId, UserId = "u2", Position = 1,
                JoinedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
        await using var db2 = factory.CreateDbContext();
        Assert.False(await db2.EventConfirmations.AnyAsync(c => c.EventId == eventId && c.UserId == "u1"));
        // The freed spot goes to the next in line, same as a self-cancel.
        Assert.True(await db2.EventConfirmations.AnyAsync(c => c.EventId == eventId && c.UserId == "u2"));
        Assert.False(await db2.WaitingLists.AnyAsync(w => w.EventId == eventId && w.UserId == "u2"));
    }

    [Fact]
    public async Task LeaveGroupAsync_KeepsPaidFutureConfirmation()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(1), price: 15m);
        await SeedConfirmationAsync(factory, eventId, "u1",
            hasPaid: true, status: EventConfirmationPaymentStatus.Paid);

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
        await using var db = factory.CreateDbContext();
        Assert.True(await db.EventConfirmations.AnyAsync(c => c.EventId == eventId && c.UserId == "u1"));
    }

    [Fact]
    public async Task LeaveGroupAsync_RemovesWaitlistEntries()
    {
        var (factory, svc, groupId) = await SetupWithGroupAsync();
        await SeedMemberAsync(factory, groupId, "u1");
        var eventId = await SeedEventAsync(factory, groupId,
            DateTime.UtcNow.AddDays(1), price: 15m);
        await using (var db = factory.CreateDbContext())
        {
            db.WaitingLists.Add(new WaitingList
            {
                EventId = eventId, UserId = "u1", Position = 1,
                JoinedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var result = await svc.LeaveGroupAsync(groupId, "u1");

        Assert.Equal(LeaveGroupResult.Left, result);
        await using var db2 = factory.CreateDbContext();
        Assert.False(await db2.WaitingLists.AnyAsync(w => w.EventId == eventId && w.UserId == "u1"));
    }
}
