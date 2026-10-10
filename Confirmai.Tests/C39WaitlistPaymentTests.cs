using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Services.Events;
using Confirmai.Services.Factories;
using Confirmai.Services.Groups;
using Confirmai.Services.Interfaces;
using Confirmai.Services.Payment;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C39-A F5 — a lista de espera do poker nao paga: a tela /pagamento/evento
/// redireciona, o servidor recusa a cobranca, e inadimplencia/saida-com-
/// divida/"Meus pagamentos" ignoram quem esta alem do MaxPlayers.
/// </summary>
public class C39WaitlistPaymentTests
{
    private static IDbContextFactory<AppDbContext> NewFactory()
        => TestDbContextFactory.CreateInMemoryFactory($"waitlist-{Guid.NewGuid()}");

    private static EventPaymentService NewPaymentSvc(IDbContextFactory<AppDbContext> factory)
    {
        var gatewayService = new GatewayService(factory);
        var gwFactory = new EventPaymentGatewayFactory(Array.Empty<IEventPaymentGateway>(), gatewayService);
        return new EventPaymentService(factory, gwFactory, Options.Create(new FeeOptions()));
    }

    private static GroupPaymentsService NewGroupPaymentsSvc(IDbContextFactory<AppDbContext> factory, string? userId)
    {
        var authMock = new Mock<AuthenticationStateProvider>();
        var identity = userId is not null
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test")
            : new ClaimsIdentity();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(identity)));
        var notif = new EventNotificationService(factory, Mock.Of<IEmailSender>(), NullLogger<EventNotificationService>.Instance);
        var log = new LogService(factory, NullLogger<LogService>.Instance);
        var policy = new PlatformFeePolicy(Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m }));
        return new GroupPaymentsService(factory, authMock.Object, notif, log,
            new PlatformFeeLedgerService(factory, policy), policy, NullLogger<GroupPaymentsService>.Instance);
    }

    private static GroupDetailService NewGroupDetailSvc(IDbContextFactory<AppDbContext> factory)
    {
        var authMock = new Mock<AuthenticationStateProvider>();
        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        var log = new LogService(factory, NullLogger<LogService>.Instance);
        var notif = new EventNotificationService(factory, Mock.Of<IEmailSender>(), NullLogger<EventNotificationService>.Instance);
        var eventSvc = new Confirmai.Services.Futsal.EventDetailService(factory, log, notif,
            new PlatformFeePolicy(Options.Create(new FeeOptions { ManualPlatformFeeFixed = 0.75m })));
        return new GroupDetailService(factory, authMock.Object, log, eventSvc);
    }

    /// <summary>
    /// Torneio pago com MaxPlayers=1: slot (player-1) e espera (player-2).
    /// </summary>
    private static async Task<(int groupId, int eventId, int slotConfId, int waitConfId)>
        SeedPokerWaitlistAsync(IDbContextFactory<AppDbContext> factory, bool pastEvent = false)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "admin-1", UserName = "Admin", PixKey = "a@pix" });
        db.Users.Add(new ApplicationUser { Id = "player-1", UserName = "P1", FullName = "Player One" });
        db.Users.Add(new ApplicationUser { Id = "player-2", UserName = "P2", FullName = "Player Two" });
        var group = new Group
        {
            Name = "Poker", Sport = Sport.Poker,
            CreatedByUserId = "admin-1", InviteCode = "XYZ789",
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember { UserId = "admin-1", GroupId = group.Id, Role = GroupMemberRole.Admin });
        db.GroupMembers.Add(new GroupMember { UserId = "player-1", GroupId = group.Id, Role = GroupMemberRole.Member });
        db.GroupMembers.Add(new GroupMember { UserId = "player-2", GroupId = group.Id, Role = GroupMemberRole.Member });
        var ev = new Event
        {
            GroupId = group.Id, Sport = Sport.Poker, PokerEventType = PokerEventType.Tournament,
            StartsAt = pastEvent ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(1),
            Price = 100m, MaxPlayers = 1, IsActive = true, CreatedByUserId = "admin-1",
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var slot = new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-1",
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-10),
            ChargedPrice = 100m, PlatformFeeAmount = 10m,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
        };
        var wait = new EventConfirmation
        {
            EventId = ev.Id, UserId = "player-2",
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-5),
            ChargedPrice = 100m, PlatformFeeAmount = 10m,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
        };
        db.EventConfirmations.AddRange(slot, wait);
        await db.SaveChangesAsync();
        return (group.Id, ev.Id, slot.Id, wait.Id);
    }

    [Fact]
    public async Task LoadConfirmationAsync_Waitlisted_RedirectsToEvent()
    {
        var factory = NewFactory();
        var (_, eventId, slotConfId, waitConfId) = await SeedPokerWaitlistAsync(factory);
        var svc = NewPaymentSvc(factory);

        var waitResult = await svc.LoadConfirmationAsync(waitConfId, "player-2");
        Assert.Equal($"/poker/{eventId}", waitResult.RedirectUrl);

        var slotResult = await svc.LoadConfirmationAsync(slotConfId, "player-1");
        Assert.Null(slotResult.RedirectUrl);
    }

    [Fact]
    public async Task GeneratePixChargeAsync_Waitlisted_Refused()
    {
        var factory = NewFactory();
        var (_, _, _, waitConfId) = await SeedPokerWaitlistAsync(factory);
        var svc = NewPaymentSvc(factory);

        var result = await svc.GeneratePixChargeAsync(waitConfId, "any", groupGatewaysEnabled: true);

        Assert.False(result.Success);
        Assert.Contains("espera", result.ErrorMessage);
    }

    [Fact]
    public async Task LoadPaymentsDataAsync_Delinquency_IgnoresWaitlisted()
    {
        var factory = NewFactory();
        var (groupId, _, _, _) = await SeedPokerWaitlistAsync(factory, pastEvent: true);
        var svc = NewGroupPaymentsSvc(factory, "admin-1");
        Group group;
        await using (var db = factory.CreateDbContext())
        {
            group = await db.Groups.Include(g => g.Members).ThenInclude(m => m.User).FirstAsync(g => g.Id == groupId);
        }

        var data = await svc.LoadPaymentsDataAsync(groupId, group);

        var entry = Assert.Single(data.DelinquencyList);
        Assert.Equal("player-1", entry.UserId);
    }

    [Fact]
    public async Task LoadMyPaymentsAsync_Waitlisted_GetsNoEntry()
    {
        var factory = NewFactory();
        var (groupId, _, _, _) = await SeedPokerWaitlistAsync(factory);

        var svc = NewGroupPaymentsSvc(factory, "player-2");
        var waitEntries = await svc.LoadMyPaymentsAsync(groupId, "player-2");
        Assert.Empty(waitEntries);

        var svc2 = NewGroupPaymentsSvc(factory, "player-1");
        var slotEntries = await svc2.LoadMyPaymentsAsync(groupId, "player-1");
        Assert.Single(slotEntries);
    }

    [Fact]
    public async Task LeaveGroupAsync_Waitlisted_CanLeave()
    {
        var factory = NewFactory();
        var (groupId, _, _, _) = await SeedPokerWaitlistAsync(factory);
        var svc = NewGroupDetailSvc(factory);

        var result = await svc.LeaveGroupAsync(groupId, "player-2");

        Assert.Equal(LeaveGroupResult.Left, result);
    }
}
