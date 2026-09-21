using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C36-C Fase 2 — join-request approvals and event slot/waitlist mutations
/// must verify the caller is an admin of THAT group in the service layer.
/// Includes cross-group IDOR: an admin of group A must not act on group B.
/// </summary>
public class C36CGroupAccessTests
{
    private static (AppDbContext db, IDbContextFactory<AppDbContext> factory) Ctx() =>
        TestDataFactory.CreateDbContextWithFactory();

    private static LogService NewLog(IDbContextFactory<AppDbContext> f) =>
        new(f, NullLogger<LogService>.Instance);

    private static GroupDetailService NewGroupSvc(IDbContextFactory<AppDbContext> f)
    {
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        return new GroupDetailService(f, authMock.Object, NewLog(f));
    }

    private static EventDetailService NewEventSvc(IDbContextFactory<AppDbContext> f) =>
        new(f, NewLog(f),
            new EventNotificationService(f, new Mock<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>().Object,
                NullLogger<EventNotificationService>.Instance),
            new PlatformFeePolicy(Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m })));

    private static async Task<GroupJoinRequest> SeedPendingRequestAsync(
        AppDbContext db, int groupId, string userId = "req-1")
    {
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userId, FullName = "Req" });
        var req = new GroupJoinRequest
        {
            GroupId = groupId, UserId = userId,
            RequestedAt = DateTime.UtcNow, Status = JoinRequestStatus.Pending,
        };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();
        return req;
    }

    // ── join requests — non-admin is a no-op ────────────────────────────────

    [Fact]
    public async Task ApproveRequest_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (group, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var req = await SeedPendingRequestAsync(db, group.Id);

        await NewGroupSvc(factory).ApproveRequestAsync(req.Id, "intruder-1", group.Name);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Pending, saved!.Status);
        Assert.Empty(verify.GroupMembers.Where(m => m.UserId == "req-1"));
    }

    [Fact]
    public async Task RejectRequest_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (group, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var req = await SeedPendingRequestAsync(db, group.Id);

        await NewGroupSvc(factory).RejectRequestAsync(req.Id, "intruder-1");

        await using var verify = factory.CreateDbContext();
        var saved = await verify.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Pending, saved!.Status);
    }

    [Fact]
    public async Task ApproveSelected_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (group, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var req = await SeedPendingRequestAsync(db, group.Id);

        await NewGroupSvc(factory).ApproveSelectedAsync(new HashSet<int> { req.Id }, "intruder-1", group.Name);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Pending, saved!.Status);
        Assert.Empty(verify.GroupMembers.Where(m => m.UserId == "req-1"));
    }

    [Fact]
    public async Task RejectSelected_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (group, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var req = await SeedPendingRequestAsync(db, group.Id);

        await NewGroupSvc(factory).RejectSelectedAsync(new HashSet<int> { req.Id }, "intruder-1");

        await using var verify = factory.CreateDbContext();
        var saved = await verify.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Pending, saved!.Status);
    }

    // ── join requests — cross-group IDOR: admin of A cannot act on B ────────

    [Fact]
    public async Task ApproveRequest_AdminOfOtherGroup_NoOp()
    {
        var (db, factory) = Ctx();
        var (groupA, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-A");
        var (groupB, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-B");
        var req = await SeedPendingRequestAsync(db, groupB.Id);

        await NewGroupSvc(factory).ApproveRequestAsync(req.Id, "admin-A", groupB.Name);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Pending, saved!.Status);
        Assert.Empty(verify.GroupMembers.Where(m => m.GroupId == groupB.Id && m.UserId == "req-1"));
    }

    [Fact]
    public async Task ApproveRequest_GroupAdmin_Works()
    {
        var (db, factory) = Ctx();
        var (group, _) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var req = await SeedPendingRequestAsync(db, group.Id);

        await NewGroupSvc(factory).ApproveRequestAsync(req.Id, "admin-1", group.Name);

        await using var verify = factory.CreateDbContext();
        var saved = await verify.GroupJoinRequests.FindAsync(req.Id);
        Assert.Equal(JoinRequestStatus.Approved, saved!.Status);
        Assert.Contains(verify.GroupMembers, m => m.GroupId == group.Id && m.UserId == "req-1");
    }

    // ── waitlist removal — service-level admin check ─────────────────────────

    [Fact]
    public async Task AdminRemoveFromWaitlist_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (group, evt) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var wl = new WaitingList { EventId = evt.Id, UserId = "wl-1", Position = (int)FutsalPosition.Outfield };
        db.WaitingLists.Add(wl);
        await db.SaveChangesAsync();

        await NewEventSvc(factory).AdminRemoveFromWaitlistAsync(wl.Id, "intruder-1", evt.Id);

        await using var verify = factory.CreateDbContext();
        Assert.NotNull(await verify.WaitingLists.FindAsync(wl.Id));
    }

    [Fact]
    public async Task AdminRemoveFromWaitlist_AdminOfOtherGroup_NoOp()
    {
        var (db, factory) = Ctx();
        var (_, evtB) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-B");
        await TestDataFactory.SeedEventWithAdminAsync(db, "admin-A");
        var wl = new WaitingList { EventId = evtB.Id, UserId = "wl-1", Position = (int)FutsalPosition.Outfield };
        db.WaitingLists.Add(wl);
        await db.SaveChangesAsync();

        await NewEventSvc(factory).AdminRemoveFromWaitlistAsync(wl.Id, "admin-A", evtB.Id);

        await using var verify = factory.CreateDbContext();
        Assert.NotNull(await verify.WaitingLists.FindAsync(wl.Id));
    }

    // ── slot mutations — non-admin cannot change capacity ───────────────────

    [Fact]
    public async Task AdminAddOutfieldSlot_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (_, evt) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var max = evt.MaxPlayers;

        await NewEventSvc(factory).AdminAddOutfieldSlotAsync(evt.Id, "intruder-1");

        await using var verify = factory.CreateDbContext();
        Assert.Equal(max, (await verify.Events.FindAsync(evt.Id))!.MaxPlayers);
    }

    [Fact]
    public async Task AdminRemoveOutfieldSlot_AdminOfOtherGroup_NoOp()
    {
        var (db, factory) = Ctx();
        var (_, evtB) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-B");
        await TestDataFactory.SeedEventWithAdminAsync(db, "admin-A");
        var max = evtB.MaxPlayers;

        await NewEventSvc(factory).AdminRemoveOutfieldSlotAsync(evtB.Id, "admin-A");

        await using var verify = factory.CreateDbContext();
        Assert.Equal(max, (await verify.Events.FindAsync(evtB.Id))!.MaxPlayers);
    }

    [Fact]
    public async Task AdminAddGoalkeeperSlot_NonAdmin_NoOp()
    {
        var (db, factory) = Ctx();
        var (_, evt) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var gk = evt.MaxGoalkeepers ?? 0;

        await NewEventSvc(factory).AdminAddGoalkeeperSlotAsync(evt.Id, "intruder-1");

        await using var verify = factory.CreateDbContext();
        Assert.Equal(gk, (await verify.Events.FindAsync(evt.Id))!.MaxGoalkeepers ?? 0);
    }

    [Fact]
    public async Task AdminRemoveGoalkeeperSlot_NullUser_NoOp()
    {
        var (db, factory) = Ctx();
        var (_, evt) = await TestDataFactory.SeedEventWithAdminAsync(db, "admin-1");
        var gk = evt.MaxGoalkeepers ?? 0;

        await NewEventSvc(factory).AdminRemoveGoalkeeperSlotAsync(evt.Id, gk - 1, null);

        await using var verify = factory.CreateDbContext();
        Assert.Equal(gk, (await verify.Events.FindAsync(evt.Id))!.MaxGoalkeepers ?? 0);
    }
}
