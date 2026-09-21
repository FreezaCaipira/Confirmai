using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Futsal;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Confirmai.Tests;

public class EventDetailServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, EventDetailService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"edetail-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var notificationService = new EventNotificationService(factory, Mock.Of<IEmailSender>(), NullLogger<EventNotificationService>.Instance);
        var feeOptions = Microsoft.Extensions.Options.Options.Create(
            new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = 0.75m });
        var svc = new EventDetailService(factory, logService, notificationService,
            new PlatformFeePolicy(feeOptions));
        return (factory, svc);
    }

    private static async Task<(IDbContextFactory<AppDbContext> factory, EventDetailService svc, int eventId, int groupId)> SetupWithEventAsync()
    {
        var (factory, svc) = Setup();
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "Test Group", CreatedByUserId = "creator-1" };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = "admin-1",
            Role = GroupMemberRole.Admin, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 120,
            MaxPlayers = 10,
            MaxGoalkeepers = 2,
            CreatedByUserId = "creator-1"
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return (factory, svc, ev.Id, group.Id);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullEvent_WhenNotFound()
    {
        var (factory, svc) = Setup();
        var result = await svc.LoadAsync(999, "user-1");
        Assert.Null(result.Event);
        Assert.Equal(0, result.EventNumber);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEventWithCreator()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "creator-1", UserName = "Creator", FullName = "Creator Name" });
        await db.SaveChangesAsync();

        var result = await svc.LoadAsync(eventId, "user-1");

        Assert.NotNull(result.Event);
        Assert.NotNull(result.CreatorUser);
        Assert.Equal("Creator Name", result.CreatorUser!.FullName);
    }

    [Fact]
    public async Task LoadAsync_ComputesEventNumber()
    {
        var (factory, svc, eventId, groupId) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.Events.Add(new Event { GroupId = groupId, Sport = Sport.Futsal, StartsAt = DateTime.UtcNow.AddDays(-1), MaxPlayers = 10 });
        await db.SaveChangesAsync();

        var result = await svc.LoadAsync(eventId, null);

        Assert.Equal(2, result.EventNumber);
    }

    [Fact]
    public async Task RequestToJoinAsync_CreatesPendingRequest()
    {
        var (factory, svc, _, groupId) = await SetupWithEventAsync();

        await svc.RequestToJoinAsync(groupId, "user-1");

        await using var db = factory.CreateDbContext();
        var req = await db.GroupJoinRequests.FirstOrDefaultAsync(r => r.GroupId == groupId && r.UserId == "user-1");
        Assert.NotNull(req);
        Assert.Equal(JoinRequestStatus.Pending, req!.Status);
    }

    [Fact]
    public async Task RequestToJoinAsync_DoesNotDuplicate_WhenAlreadyPending()
    {
        var (factory, svc, _, groupId) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.GroupJoinRequests.Add(new GroupJoinRequest { GroupId = groupId, UserId = "user-1", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await svc.RequestToJoinAsync(groupId, "user-1");

        await using var db2 = factory.CreateDbContext();
        var count = await db2.GroupJoinRequests.CountAsync(r => r.GroupId == groupId && r.UserId == "user-1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CancelJoinRequestAsync_RemovesPendingRequest()
    {
        var (factory, svc, _, groupId) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var req = new GroupJoinRequest { GroupId = groupId, UserId = "user-1", Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow };
        db.GroupJoinRequests.Add(req);
        await db.SaveChangesAsync();

        await svc.CancelJoinRequestAsync(req.Id);

        await using var db2 = factory.CreateDbContext();
        var found = await db2.GroupJoinRequests.FindAsync(req.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task ConfirmPresenceAsync_Confirms_WhenSlotAvailable()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();

        var result = await svc.ConfirmPresenceAsync(eventId, "user-1", FutsalPosition.Outfield);

        Assert.True(result.Success);
        Assert.False(result.AddedToWaitlist);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == "user-1");
        Assert.NotNull(conf);
    }

    [Fact]
    public async Task ConfirmPresenceAsync_AddsToWaitlist_WhenSlotFull()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var ev = await db.Events.FindAsync(eventId);
        ev!.MaxPlayers = 1;
        ev.MaxGoalkeepers = 0;
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "existing", ConfirmedAt = DateTime.UtcNow, Position = FutsalPosition.Outfield });
        await db.SaveChangesAsync();

        var result = await svc.ConfirmPresenceAsync(eventId, "user-1", FutsalPosition.Outfield);

        Assert.True(result.Success);
        Assert.True(result.AddedToWaitlist);
        await using var db2 = factory.CreateDbContext();
        var wl = await db2.WaitingLists.FirstOrDefaultAsync(w => w.EventId == eventId && w.UserId == "user-1");
        Assert.NotNull(wl);
    }

    [Fact]
    public async Task ConfirmPresenceAsync_ReturnsError_WhenAlreadyConfirmed()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "user-1", ConfirmedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await svc.ConfirmPresenceAsync(eventId, "user-1", FutsalPosition.Outfield);

        Assert.False(result.Success);
        Assert.Contains("confirmado", result.Error);
    }

    [Fact]
    public async Task ConfirmPresenceAsync_ReturnsError_WhenAlreadyOnWaitlist()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.WaitingLists.Add(new WaitingList { EventId = eventId, UserId = "user-1", Position = 1, JoinedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await svc.ConfirmPresenceAsync(eventId, "user-1", FutsalPosition.Outfield);

        Assert.False(result.Success);
        Assert.Contains("lista de espera", result.Error);
    }

    [Fact]
    public async Task CancelConfirmationAsync_RemovesConfirmation()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "user-1", ConfirmedAt = DateTime.UtcNow, Position = FutsalPosition.Outfield });
        await db.SaveChangesAsync();

        await svc.CancelConfirmationAsync(eventId, "user-1");

        await using var db2 = factory.CreateDbContext();
        var conf = await db2.EventConfirmations.FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == "user-1");
        Assert.Null(conf);
    }

    [Fact]
    public async Task CancelConfirmationAsync_PromotesFromWaitlist()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "user-1", ConfirmedAt = DateTime.UtcNow, Position = FutsalPosition.Outfield });
        db.WaitingLists.Add(new WaitingList { EventId = eventId, UserId = "user-2", Position = 1, JoinedAt = DateTime.UtcNow, DesiredPosition = null });
        await db.SaveChangesAsync();

        await svc.CancelConfirmationAsync(eventId, "user-1");

        await using var db2 = factory.CreateDbContext();
        var promoted = await db2.EventConfirmations.FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == "user-2");
        Assert.NotNull(promoted);
        var wlRemoved = await db2.WaitingLists.FirstOrDefaultAsync(w => w.EventId == eventId && w.UserId == "user-2");
        Assert.Null(wlRemoved);
    }

    [Fact]
    public async Task LeaveWaitlistAsync_RemovesEntry()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.WaitingLists.Add(new WaitingList { EventId = eventId, UserId = "user-1", Position = 1, JoinedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await svc.LeaveWaitlistAsync(eventId, "user-1");

        await using var db2 = factory.CreateDbContext();
        var wl = await db2.WaitingLists.FirstOrDefaultAsync(w => w.EventId == eventId && w.UserId == "user-1");
        Assert.Null(wl);
    }

    [Fact]
    public async Task AdminAddOutfieldSlotAsync_IncrementsMaxPlayers()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var originalMax = (await db.Events.FindAsync(eventId))!.MaxPlayers;

        await svc.AdminAddOutfieldSlotAsync(eventId, "admin-1");

        await using var db2 = factory.CreateDbContext();
        var dbEv = await db2.Events.FindAsync(eventId);
        Assert.Equal(originalMax + 1, dbEv!.MaxPlayers);
    }

    [Fact]
    public async Task AdminRemoveOutfieldSlotAsync_DecrementsMaxPlayers()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var originalMax = (await db.Events.FindAsync(eventId))!.MaxPlayers;

        await svc.AdminRemoveOutfieldSlotAsync(eventId, "admin-1");

        await using var db2 = factory.CreateDbContext();
        var dbEv = await db2.Events.FindAsync(eventId);
        Assert.Equal(originalMax - 1, dbEv!.MaxPlayers);
    }

    [Fact]
    public async Task AdminAddGoalkeeperSlotAsync_IncrementsMaxGk()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var originalGk = (await db.Events.FindAsync(eventId))!.MaxGoalkeepers ?? 0;

        await svc.AdminAddGoalkeeperSlotAsync(eventId, "admin-1");

        await using var db2 = factory.CreateDbContext();
        var dbEv = await db2.Events.FindAsync(eventId);
        Assert.Equal(originalGk + 1, dbEv!.MaxGoalkeepers);
    }

    [Fact]
    public async Task AdminRemoveGoalkeeperSlotAsync_SetsNewMaxGk()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();

        await svc.AdminRemoveGoalkeeperSlotAsync(eventId, 1, "admin-1");

        await using var db = factory.CreateDbContext();
        var dbEv = await db.Events.FindAsync(eventId);
        Assert.Equal(1, dbEv!.MaxGoalkeepers);
    }
}
