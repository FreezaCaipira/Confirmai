using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Groups;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Confirmai.Tests;

public class GroupPaymentsServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, GroupPaymentsService svc) Setup(string? userId = null, decimal manualFee = 0m)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"gpay-{Guid.NewGuid()}");
        var logService = new LogService(factory, NullLogger<LogService>.Instance);
        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = userId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")
            : new ClaimsIdentity();
        authMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));
        var emailSenderMock = new Mock<IEmailSender>();
        var notificationService = new EventNotificationService(factory, emailSenderMock.Object, NullLogger<EventNotificationService>.Instance);
        var svc = new GroupPaymentsService(factory, authMock.Object, notificationService, logService,
            Microsoft.Extensions.Options.Options.Create(new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = manualFee }));
        return (factory, svc);
    }

    private static async Task<(IDbContextFactory<AppDbContext> factory, GroupPaymentsService svc, int groupId, Group group)> SetupWithGroupAndAdminAsync(decimal manualFee = 0m)
    {
        var (factory, svc) = Setup("admin-1", manualFee);
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin", FullName = "Admin User" });
        db.Users.Add(new ApplicationUser { Id = "player-1", UserName = "Player1", FullName = "Player One" });
        var group = new Group { Name = "Test Group", Sport = Sport.Futsal, EnablePaymentGateways = false, CreatedByUserId = "admin-1" };
        db.Groups.Add(group);
        db.GroupMembers.Add(new GroupMember { UserId = "admin-1", GroupId = 0, Role = GroupMemberRole.Admin });
        await db.SaveChangesAsync();
        // Need to set GroupId after save for the member
        var member = await db.GroupMembers.FirstAsync(m => m.UserId == "admin-1");
        member.GroupId = group.Id;
        db.GroupMembers.Add(new GroupMember { UserId = "player-1", GroupId = group.Id, Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();
        return (factory, svc, group.Id, group);
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

    // ── LoadGroupAndCheckAdminAsync ───────────────────────────────────

    [Fact]
    public async Task LoadGroupAndCheckAdminAsync_ReturnsGroupAndIsAdmin_WhenUserIsAdmin()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();

        var (group, isAdmin) = await svc.LoadGroupAndCheckAdminAsync(groupId, "admin-1");

        Assert.NotNull(group);
        Assert.True(isAdmin);
    }

    [Fact]
    public async Task LoadGroupAndCheckAdminAsync_ReturnsFalse_WhenUserIsNotAdmin()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();

        var (group, isAdmin) = await svc.LoadGroupAndCheckAdminAsync(groupId, "player-1");

        Assert.NotNull(group);
        Assert.False(isAdmin);
    }

    [Fact]
    public async Task LoadGroupAndCheckAdminAsync_ReturnsNullGroup_WhenNotFound()
    {
        var (factory, svc) = Setup();

        var (group, isAdmin) = await svc.LoadGroupAndCheckAdminAsync(999, "user-1");

        Assert.Null(group);
        Assert.False(isAdmin);
    }

    // ── LoadPaymentsDataAsync ─────────────────────────────────────────

    [Fact]
    public async Task LoadPaymentsDataAsync_ReturnsEmptyLists_WhenNoPayments()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Empty(data.DelinquencyList);
        Assert.Empty(data.PaymentHistory);
        Assert.Empty(data.PendingProofList);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_ReturnsDelinquency_WhenUnpaidConfirmationExists()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 10m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.NotEmpty(data.DelinquencyList);
        Assert.Equal("player-1", data.DelinquencyList[0].UserId);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_ExcludesGoalkeepers_FromDelinquency()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 10m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Goalkeeper, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Empty(data.DelinquencyList);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_ReturnsPaymentHistory_WhenManualPaidExists()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-2), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            MarkedPaidByUserId = "admin-1", MarkedPaidAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.NotEmpty(data.PaymentHistory);
        Assert.Equal("Player One", data.PaymentHistory[0].UserName);
    }

    // ── MarkPaidAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task MarkPaidAsync_MarksAsPaid_WhenConfirmationExistsAndNotPaid()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 10m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var conf = new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        await svc.MarkPaidAsync(conf.Id, "player-1", "admin-1", groupId);

        await using var db2 = factory.CreateDbContext();
        var dbConf = await db2.EventConfirmations.FindAsync(conf.Id);
        Assert.True(dbConf!.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, dbConf.PaymentStatus);
        Assert.Equal("admin-1", dbConf.MarkedPaidByUserId);
    }

    [Fact]
    public async Task MarkPaidAsync_DoesNothing_WhenAlreadyPaid()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 10m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var conf = new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            MarkedPaidByUserId = "old-admin", MarkedPaidAt = DateTime.UtcNow
        };
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        await svc.MarkPaidAsync(conf.Id, "player-1", "admin-1", groupId);

        await using var db2 = factory.CreateDbContext();
        var dbConf = await db2.EventConfirmations.FindAsync(conf.Id);
        Assert.Equal("old-admin", dbConf!.MarkedPaidByUserId);
    }

    [Fact]
    public async Task MarkPaidAsync_DoesNothing_WhenConfirmationNotFound()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await svc.MarkPaidAsync(999, "player-1", "admin-1", groupId);
    }

    // ── NotifyDelinquencyAsync ────────────────────────────────────────

    [Fact]
    public async Task NotifyDelinquencyAsync_DoesNothing_WhenCurrentUserIdIsNull()
    {
        var (factory, svc) = Setup();
        var d = new UserDelinquency("u1", "Player", new List<DelinquencyEntry>
        {
            new(1, 10, DateTime.UtcNow.AddDays(-1), 10m, "/futsal/10", false)
        });
        await svc.NotifyDelinquencyAsync(d, null, "Group", 1);
    }

    [Fact]
    public async Task NotifyDelinquencyAsync_Completes_WhenValid()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        var d = new UserDelinquency("player-1", "Player One", new List<DelinquencyEntry>
        {
            new(1, 10, DateTime.UtcNow.AddDays(-1), 10m, "/futsal/10", false)
        });

        await svc.NotifyDelinquencyAsync(d, "admin-1", "Test Group", groupId);
    }

    // ── Manual platform fee inclusion (bug fix: admin must see Price + Fee) ──

    [Fact]
    public async Task LoadPaymentsDataAsync_DelinquencyIncludesManualFee_WhenFutsalManualMode()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync(manualFee: 0.75m);
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Single(data.DelinquencyList);
        Assert.Equal(15.75m, data.DelinquencyList[0].Entries[0].EventPrice);
        Assert.Equal(15.75m, data.DelinquencyList[0].TotalAmount);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_PendingProofIncludesManualFee_WhenFutsalManualMode()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync(manualFee: 0.75m);
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            PixProofUploadedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Single(data.PendingProofList);
        Assert.Equal(15.75m, data.PendingProofList[0].EventPrice);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_PaymentHistoryIncludesManualFee_WhenFutsalManualMode()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync(manualFee: 0.75m);
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-2), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            MarkedPaidByUserId = "admin-1", MarkedPaidAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Single(data.PaymentHistory);
        Assert.Equal(15.75m, data.PaymentHistory[0].EventPrice);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_DoesNotAddFee_WhenGatewaysEnabled()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync(manualFee: 0.75m);
        // Override: turn on gateways — fee should NOT apply
        group.EnablePaymentGateways = true;
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Single(data.DelinquencyList);
        Assert.Equal(15m, data.DelinquencyList[0].Entries[0].EventPrice);
    }
}
