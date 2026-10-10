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
/// C39-B F9 + delta da review — o jogador do cash escolhe a mesa na
/// inscricao: opcao obrigatoria quando o evento tem mesas ativas, opcao
/// invalida/de outro evento/inativa recusada (nao grava o id), carimbo de
/// PriceOptionId + preco + taxa da mesa, e troca de mesa recarimba so quem
/// ainda nao pagou nem enviou comprovante.
/// </summary>
public class C39CashTableConfirmTests
{
    private static (IDbContextFactory<AppDbContext> factory, PokerDetailService svc) Setup()
    {
        var factory = TestDbContextFactory.CreateInMemoryFactory($"cash-confirm-{Guid.NewGuid()}");
        var policy = new PlatformFeePolicy(Options.Create(new FeeOptions
        {
            ManualPlatformFeeFixed = 0.75m,
            PokerFeePercentMin = 5m,
            PokerFeePercentMax = 10m,
        }));
        return (factory, new PokerDetailService(factory, policy));
    }

    private static async Task<(int eventId, int table1Id, int table2Id)> SeedCashAsync(
        IDbContextFactory<AppDbContext> factory, bool secondTableActive = true)
    {
        await using var db = factory.CreateDbContext();
        var group = new Group
        {
            Name = "Poker Group", Sport = Sport.Poker,
            CreatedByUserId = "creator-1",
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        db.GroupMembers.Add(new GroupMember
        {
            GroupId = group.Id, UserId = "player-1",
            Role = GroupMemberRole.Member, CreatedAt = DateTime.UtcNow,
        });
        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Poker,
            PokerEventType = PokerEventType.CashGame,
            StartsAt = DateTime.UtcNow.AddDays(1),
            CreatedByUserId = "creator-1",
            IsActive = true,
        };
        ev.PriceOptions.Add(new EventPriceOption
        {
            Label = "Mesa 1/2", Price = 200m, PlatformFeePercent = 5m, SortOrder = 0, IsActive = true,
        });
        ev.PriceOptions.Add(new EventPriceOption
        {
            Label = "Mesa 2/5", Price = 500m, PlatformFeePercent = 10m, SortOrder = 1, IsActive = secondTableActive,
        });
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var opts = ev.PriceOptions.OrderBy(o => o.SortOrder).ToList();
        return (ev.Id, opts[0].Id, opts[1].Id);
    }

    [Fact]
    public async Task ConfirmAsync_CashWithoutOption_Refused()
    {
        var (factory, svc) = Setup();
        var (eventId, _, _) = await SeedCashAsync(factory);

        var result = await svc.ConfirmAsync(eventId, "player-1");

        Assert.Equal(PokerConfirmStatus.PriceOptionRequired, result.Status);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.EventConfirmations.CountAsync());
    }

    [Fact]
    public async Task ConfirmAsync_InvalidOption_Refused()
    {
        var (factory, svc) = Setup();
        var (eventId, _, inactiveId) = await SeedCashAsync(factory, secondTableActive: false);

        var otherEvent = await svc.ConfirmAsync(eventId, "player-1", priceOptionId: 99999);
        var inactive = await svc.ConfirmAsync(eventId, "player-1", priceOptionId: inactiveId);

        Assert.Equal(PokerConfirmStatus.InvalidPriceOption, otherEvent.Status);
        Assert.Equal(PokerConfirmStatus.InvalidPriceOption, inactive.Status);
        await using var db = factory.CreateDbContext();
        Assert.Equal(0, await db.EventConfirmations.CountAsync());
    }

    [Fact]
    public async Task ConfirmAsync_OptionFromAnotherEvent_Refused()
    {
        var (factory, svc) = Setup();
        var (eventId, _, _) = await SeedCashAsync(factory);
        var (_, otherTableId, _) = await SeedCashAsync(factory);

        var result = await svc.ConfirmAsync(eventId, "player-1", priceOptionId: otherTableId);

        Assert.Equal(PokerConfirmStatus.InvalidPriceOption, result.Status);
    }

    [Fact]
    public async Task ConfirmAsync_ValidOption_StampsPriceFeeAndOption()
    {
        var (factory, svc) = Setup();
        var (eventId, _, table2Id) = await SeedCashAsync(factory);

        var result = await svc.ConfirmAsync(eventId, "player-1", priceOptionId: table2Id);

        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.SingleAsync();
        Assert.Equal(table2Id, conf.PriceOptionId);
        Assert.Equal(500m, conf.ChargedPrice);
        Assert.Equal(50m, conf.PlatformFeeAmount); // 10% de 500
    }

    [Fact]
    public async Task ChangeTableAsync_Unpaid_RestampsPriceAndFee()
    {
        var (factory, svc) = Setup();
        var (eventId, table1Id, table2Id) = await SeedCashAsync(factory);
        await svc.ConfirmAsync(eventId, "player-1", priceOptionId: table1Id);

        var result = await svc.ChangeTableAsync(eventId, "player-1", table2Id);

        Assert.Equal(PokerChangeTableStatus.Changed, result.Status);
        await using var db = factory.CreateDbContext();
        var conf = await db.EventConfirmations.SingleAsync();
        Assert.Equal(table2Id, conf.PriceOptionId);
        Assert.Equal(500m, conf.ChargedPrice);
        Assert.Equal(50m, conf.PlatformFeeAmount);
    }

    [Fact]
    public async Task ChangeTableAsync_PaidOrProof_Refused()
    {
        var (factory, svc) = Setup();
        var (eventId, table1Id, table2Id) = await SeedCashAsync(factory);
        await svc.ConfirmAsync(eventId, "player-1", priceOptionId: table1Id);

        await using (var db = factory.CreateDbContext())
        {
            var conf = await db.EventConfirmations.SingleAsync();
            conf.HasPaid = true;
            await db.SaveChangesAsync();
        }

        var result = await svc.ChangeTableAsync(eventId, "player-1", table2Id);

        Assert.Equal(PokerChangeTableStatus.PaidOrProofSent, result.Status);
        await using var db2 = factory.CreateDbContext();
        var kept = await db2.EventConfirmations.SingleAsync();
        Assert.Equal(table1Id, kept.PriceOptionId);
        Assert.Equal(200m, kept.ChargedPrice);
    }

    [Fact]
    public async Task ConfirmAsync_CashWithoutTables_FreeConfirmOk()
    {
        var (factory, svc) = Setup();
        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "G", Sport = Sport.Poker, CreatedByUserId = "c" };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        var ev = new Event
        {
            GroupId = group.Id, Sport = Sport.Poker,
            PokerEventType = PokerEventType.CashGame,
            StartsAt = DateTime.UtcNow.AddDays(1),
            CreatedByUserId = "c", IsActive = true,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var result = await svc.ConfirmAsync(ev.Id, "player-1");

        Assert.Equal(PokerConfirmStatus.Confirmed, result.Status);
        var conf = await db.EventConfirmations.SingleAsync();
        Assert.Null(conf.ChargedPrice);
        Assert.Null(conf.PriceOptionId);
    }
}
