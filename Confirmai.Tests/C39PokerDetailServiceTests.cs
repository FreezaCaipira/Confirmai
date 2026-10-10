using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Confirmai.Services.Poker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

/// <summary>
/// C39-A F1 — inscricao do poker via service, com carimbo de preco e taxa.
/// </summary>
public class C39PokerDetailServiceTests
{
    private static (IDbContextFactory<AppDbContext> factory, PokerDetailService svc) Setup(decimal feeFixed = 0.75m, decimal feeMin = 5m, decimal feeMax = 10m)
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"pdetail-{Guid.NewGuid()}");
        var feeOptions = Options.Create(new FeeOptions
        {
            ManualPlatformFeeFixed = feeFixed,
            PokerFeePercentMin = feeMin,
            PokerFeePercentMax = feeMax,
        });
        return (factory, new PokerDetailService(factory, new PlatformFeePolicy(feeOptions)));
    }

    private static async Task<int> SeedEventAsync(
        IDbContextFactory<AppDbContext> factory,
        decimal? price = null,
        decimal? feePercent = null,
        int maxPlayers = 9,
        bool isPrivate = false,
        bool isMember = true,
        DateTime? waivedUntil = null)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group
        {
            Name = "Poker Group",
            Sport = Sport.Poker,
            IsPrivate = isPrivate,
            CreatedByUserId = "creator-1",
            PlatformFeeWaivedUntil = waivedUntil,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        if (isMember)
        {
            db.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id, UserId = "player-1",
                Role = GroupMemberRole.Member, CreatedAt = DateTime.UtcNow,
            });
        }
        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Poker,
            PokerEventType = PokerEventType.Tournament,
            StartsAt = DateTime.UtcNow.AddDays(1),
            MaxPlayers = maxPlayers,
            Price = price,
            PlatformFeePercent = feePercent,
            CreatedByUserId = "creator-1",
            IsActive = true,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    [Fact]
    public async Task ConfirmAsync_StampsChargedPrice_AndPercentFee()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory, price: 200m, feePercent: 5m);

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
        Assert.Equal(200m, conf.ChargedPrice);
        Assert.Equal(10m, conf.PlatformFeeAmount); // 5% de 200
    }

    [Fact]
    public async Task ConfirmAsync_FreeEvent_StampsNullFee()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory, price: null);

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.SingleAsync(c => c.EventId == eventId);
        Assert.Null(conf.ChargedPrice);
        Assert.Null(conf.PlatformFeeAmount);
    }

    [Fact]
    public async Task ConfirmAsync_WaivedGroup_StampsZeroFee()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory, price: 100m, feePercent: 10m,
            waivedUntil: DateTime.UtcNow.AddDays(30));

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.SingleAsync();
        Assert.Equal(0m, conf.PlatformFeeAmount);
    }

    [Fact]
    public async Task ConfirmAsync_Refuses_WhenAlreadyRegistered()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory);

        await svc.ConfirmAsync(eventId, "player-1");
        var second = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.AlreadyRegistered, second.Status);
        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.EventConfirmations.CountAsync(c => c.EventId == eventId));
    }

    [Fact]
    public async Task ConfirmAsync_Refuses_NonMember_InPrivateGroup()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory, isPrivate: true, isMember: false);

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.NotGroupMember, result.Status);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.EventConfirmations.CountAsync(c => c.EventId == eventId));
    }

    [Fact]
    public async Task ConfirmAsync_Allows_NonMember_InPublicGroup()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory, isPrivate: false, isMember: false);

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
    }

    [Fact]
    public async Task ConfirmAsync_Refuses_CancelledEvent()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory);
        await using (var db = factory.CreateDbContext())
        {
            (await db.Events.FindAsync(eventId))!.IsActive = false;
            await db.SaveChangesAsync();
        }

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.EventCancelled, result.Status);
    }

    [Fact]
    public async Task ConfirmAsync_FlagsWaitlist_WhenBeyondMaxPlayers()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory, maxPlayers: 1);

        var first = await svc.ConfirmAsync(eventId, "player-1");
        var second = await svc.ConfirmAsync(eventId, "player-2");

        Assert.False(first.IsWaitlisted);
        Assert.True(second.IsWaitlisted);
    }

    [Fact]
    public async Task CancelAsync_RemovesConfirmation()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory);
        await svc.ConfirmAsync(eventId, "player-1");

        var result = await svc.CancelAsync(eventId, "player-1");

        Assert.Equal(PokerCancelStatus.Cancelled, result.Status);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.EventConfirmations.CountAsync(c => c.EventId == eventId));
    }

    [Fact]
    public async Task CancelAsync_Blocked_WhenPaid()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory);
        await svc.ConfirmAsync(eventId, "player-1");
        await using (var db = factory.CreateDbContext())
        {
            var conf = await db.EventConfirmations.SingleAsync();
            conf.HasPaid = true;
            conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
            await db.SaveChangesAsync();
        }

        var result = await svc.CancelAsync(eventId, "player-1");

        Assert.Equal(PokerCancelStatus.PaidOrProofSent, result.Status);
        await using var check = factory.CreateDbContext();
        Assert.Equal(1, await check.EventConfirmations.CountAsync());
    }

    [Fact]
    public async Task CancelAsync_Blocked_WhenProofSent()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory);
        await svc.ConfirmAsync(eventId, "player-1");
        await using (var db = factory.CreateDbContext())
        {
            var conf = await db.EventConfirmations.SingleAsync();
            conf.PixProofUploadedAt = DateTime.UtcNow;
            conf.PixProofImageData = new byte[] { 1 };
            await db.SaveChangesAsync();
        }

        var result = await svc.CancelAsync(eventId, "player-1");

        Assert.Equal(PokerCancelStatus.PaidOrProofSent, result.Status);
    }

    [Fact]
    public async Task CancelAsync_NotRegistered_IsNoop()
    {
        var (factory, svc) = Setup();
        var eventId = await SeedEventAsync(factory);

        var result = await svc.CancelAsync(eventId, "player-1");

        Assert.Equal(PokerCancelStatus.NotRegistered, result.Status);
    }
}
