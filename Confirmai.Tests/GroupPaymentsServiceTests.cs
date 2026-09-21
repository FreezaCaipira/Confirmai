using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Groups;
using Confirmai.Services.Payment;
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
        var feeOptions = Microsoft.Extensions.Options.Options.Create(new Confirmai.Configuration.FeeOptions { ManualPlatformFeeFixed = manualFee });
        var feePolicy = new PlatformFeePolicy(feeOptions);
        var feeLedger = new PlatformFeeLedgerService(factory, feePolicy);
        var svc = new GroupPaymentsService(factory, authMock.Object, notificationService, logService,
            feeLedger, feePolicy, NullLogger<GroupPaymentsService>.Instance);
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

    [Fact]
    public async Task MarkPaidAsync_StampsPlatformFee_WhenFutsalManualMode()
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
        Assert.Equal(0.75m, dbConf.PlatformFeeAmount);
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

    // ── Caracterizacao: comportamentos que o DelinquencyService cobria e
    //    precisam ficar cobertos antes de remove-lo (Fase A do C29). ──

    [Fact]
    public async Task LoadPaymentsDataAsync_ExcludesFutureEvents_FromDelinquency()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(1), Price = 10m, MaxPlayers = 10
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

        Assert.Empty(data.DelinquencyList);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_ExcludesPaidConfirmations_FromDelinquency()
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
            EventId = ev.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Empty(data.DelinquencyList);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_ExcludesZeroAndNullPrices_FromDelinquency()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var evFree = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 0m, MaxPlayers = 10
        };
        var evNull = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra B",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = null, MaxPlayers = 10
        };
        db.Events.AddRange(evFree, evNull);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evFree.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evNull.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Empty(data.DelinquencyList);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_OrdersDelinquencyByTotalAmountDescending()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "player-2", UserName = "P2", FullName = "Player Two" });
        db.GroupMembers.Add(new GroupMember { UserId = "player-2", GroupId = groupId, Role = GroupMemberRole.Member });
        await db.SaveChangesAsync();

        var evCheap = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "A",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 5m, MaxPlayers = 10
        };
        var evPricey = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "B",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 50m, MaxPlayers = 10
        };
        db.Events.AddRange(evCheap, evPricey);
        await db.SaveChangesAsync();

        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evCheap.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = evPricey.Id, UserId = "player-2", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Reload group with updated members — LoadPaymentsDataAsync uses group.Members
        await using var db2 = factory.CreateDbContext();
        var groupWithMembers = await db2.Groups.Include(g => g.Members).FirstAsync(g => g.Id == groupId);

        var data = await svc.LoadPaymentsDataAsync(groupId, groupWithMembers);

        Assert.Equal(2, data.DelinquencyList.Count);
        Assert.True(data.DelinquencyList[0].TotalAmount >= data.DelinquencyList[1].TotalAmount);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_ExcludesNonGroupMembers_FromDelinquency()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "outsider", UserName = "Outsider", FullName = "Outsider" });
        await db.SaveChangesAsync();

        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Quadra",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 10m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "outsider", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Empty(data.DelinquencyList);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_HistoryExcludesNonManualPayments()
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
        // Gateway payment (not manual) — should be excluded from history
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            PaymentGatewayName = "EfiBank", PixTxId = "tx-1"
        });
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Empty(data.PaymentHistory);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_HistoryOrdersByMarkedAtDescending()
    {
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev1 = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "A",
            StartsAt = DateTime.UtcNow.AddDays(-3), Price = 10m, MaxPlayers = 10
        };
        var ev2 = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "B",
            StartsAt = DateTime.UtcNow.AddDays(-2), Price = 20m, MaxPlayers = 10
        };
        db.Events.AddRange(ev1, ev2);
        await db.SaveChangesAsync();

        var older = new EventConfirmation
        {
            EventId = ev1.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            MarkedPaidByUserId = "admin-1", MarkedPaidAt = DateTime.UtcNow.AddDays(-2)
        };
        var newer = new EventConfirmation
        {
            EventId = ev2.Id, UserId = "player-1", HasPaid = true,
            PaymentStatus = EventConfirmationPaymentStatus.Paid,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            MarkedPaidByUserId = "admin-1", MarkedPaidAt = DateTime.UtcNow
        };
        db.EventConfirmations.AddRange(older, newer);
        await db.SaveChangesAsync();

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        Assert.Equal(2, data.PaymentHistory.Count);
        Assert.True(data.PaymentHistory[0].MarkedAt >= data.PaymentHistory[1].MarkedAt);
    }

    // ── LoadGroupAccessAsync / LoadMyPaymentsAsync (C36-C Fase 3) ──────

    [Fact]
    public async Task LoadGroupAccessAsync_MemberSeesIsMember_NotAdmin()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();

        var (group, isAdmin, isMember) = await svc.LoadGroupAccessAsync(groupId, "player-1");

        Assert.NotNull(group);
        Assert.False(isAdmin);
        Assert.True(isMember);
    }

    [Fact]
    public async Task LoadGroupAccessAsync_OutsiderIsNeitherMemberNorAdmin()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();

        var (group, isAdmin, isMember) = await svc.LoadGroupAccessAsync(groupId, "outsider-1");

        Assert.NotNull(group);
        Assert.False(isAdmin);
        Assert.False(isMember);
    }

    [Fact]
    public async Task LoadMyPaymentsAsync_ReturnsOwnConfirmations_Only()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "A",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.AddRange(
            new EventConfirmation
            {
                EventId = ev.Id, UserId = "player-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
            },
            new EventConfirmation
            {
                EventId = ev.Id, UserId = "admin-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var mine = await svc.LoadMyPaymentsAsync(groupId, "player-1");

        Assert.Single(mine);
        Assert.Equal("player-1", "player-1"); // rows carry no other user's data
        Assert.Equal(15m, mine[0].TotalToPay);
        Assert.False(mine[0].HasPaid);
        Assert.Equal($"/pagamento/evento/{mine[0].ConfirmationId}", mine[0].PayHref);
    }

    [Fact]
    public async Task LoadMyPaymentsAsync_OutsiderGetsEmptyList()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "A",
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

        var result = await svc.LoadMyPaymentsAsync(groupId, "outsider-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadMyPaymentsAsync_NullUser_GetsEmptyList()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();

        var result = await svc.LoadMyPaymentsAsync(groupId, null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadMyPaymentsAsync_ExcludesGoalkeepersAndFreeEvents()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var paid = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Pago",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        var free = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "Gratis",
            StartsAt = DateTime.UtcNow.AddDays(-2), Price = 0m, MaxPlayers = 10
        };
        db.Events.AddRange(paid, free);
        await db.SaveChangesAsync();
        db.EventConfirmations.AddRange(
            new EventConfirmation
            {
                EventId = paid.Id, UserId = "player-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Goalkeeper, ConfirmedAt = DateTime.UtcNow
            },
            new EventConfirmation
            {
                EventId = free.Id, UserId = "player-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var mine = await svc.LoadMyPaymentsAsync(groupId, "player-1");

        Assert.Empty(mine);
    }

    [Fact]
    public async Task LoadMyPaymentsAsync_StampedFeeWins_OverLivePolicy()
    {
        // Group has a fee waiver now, but this confirmation was stamped with the
        // full fee at creation — the player must see what they were charged.
        var (factory, svc, groupId, group) = await SetupWithGroupAndAdminAsync(manualFee: 0.75m);
        await using var db = factory.CreateDbContext();
        var dbGroup = await db.Groups.FindAsync(groupId);
        dbGroup!.PlatformFeeWaivedFrom = DateTime.UtcNow.AddDays(-1);
        dbGroup.PlatformFeeWaivedUntil = DateTime.UtcNow.AddDays(30);
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "A",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1", HasPaid = false,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
            PlatformFeeAmount = 0.75m
        });
        await db.SaveChangesAsync();

        var mine = await svc.LoadMyPaymentsAsync(groupId, "player-1");

        Assert.Single(mine);
        Assert.Equal(15.75m, mine[0].TotalToPay);
    }

    // ── CountMyPendingAsync (hub badge, C36-C Fase 3/4) ────────────────

    [Fact]
    public async Task CountMyPendingAsync_CountsOnlyActionableDebts()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();
        await using var db = factory.CreateDbContext();
        var ev = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "A",
            StartsAt = DateTime.UtcNow.AddDays(-1), Price = 15m, MaxPlayers = 10
        };
        var evFuture = new Event
        {
            GroupId = groupId, Sport = Sport.Futsal, Location = "B",
            StartsAt = DateTime.UtcNow.AddDays(2), Price = 15m, MaxPlayers = 10
        };
        db.Events.AddRange(ev, evFuture);
        await db.SaveChangesAsync();
        db.EventConfirmations.AddRange(
            // owes — counts
            new EventConfirmation
            {
                EventId = ev.Id, UserId = "player-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
            },
            new EventConfirmation
            {
                EventId = evFuture.Id, UserId = "player-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
            },
            // proof sent — under review, not actionable
            new EventConfirmation
            {
                EventId = ev.Id, UserId = "player-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow,
                PixProofUploadedAt = DateTime.UtcNow
            },
            // other member's debt — never leaks into the badge
            new EventConfirmation
            {
                EventId = ev.Id, UserId = "admin-1", HasPaid = false,
                PaymentStatus = EventConfirmationPaymentStatus.Pending,
                Position = FutsalPosition.Outfield, ConfirmedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        Assert.Equal(2, await svc.CountMyPendingAsync(groupId, "player-1"));
    }

    [Fact]
    public async Task CountMyPendingAsync_Outsider_GetsZero()
    {
        var (factory, svc, groupId, _) = await SetupWithGroupAndAdminAsync();
        Assert.Equal(0, await svc.CountMyPendingAsync(groupId, "outsider-1"));
        Assert.Equal(0, await svc.CountMyPendingAsync(groupId, null));
    }
}
