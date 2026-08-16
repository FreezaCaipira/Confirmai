using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeSettlementQueryServiceTests
{
    private static FeeOptions TestFeeOptions(decimal manualFee = 0.75m) => new()
    {
        Enabled = true,
        AppFeeFixed = 0.50m,
        GatewayFeeFixed = 0.25m,
        ManualPlatformFeeFixed = manualFee,
        PlatformPixKey = "plataforma@confirmai.com",
        PlatformPixCity = "Sao Paulo"
    };

    private static PlatformFeeSettlementQueryService CreateService(
        (AppDbContext db, IDbContextFactory<AppDbContext> factory) ctx,
        decimal manualFee = 0.75m)
    {
        var options = Options.Create(TestFeeOptions(manualFee));
        var ledger = new PlatformFeeLedgerService(ctx.factory, options);
        return new PlatformFeeSettlementQueryService(ctx.factory, options, ledger);
    }

    private static async Task<(Group group, Event evt, ApplicationUser user, EventConfirmation conf)> SeedPaidFutsalMatchAsync(
        AppDbContext db, decimal fee = 0.75m, decimal price = 15m)
    {
        var group = TestDataFactory.CreateGroup("Racha", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", price);
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Jogador", "pix@jogador");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = fee;
        db.EventConfirmations.Add(conf);
        await db.SaveChangesAsync();

        return (group, evt, user, conf);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_NoFees_ReturnsEmptyBreakdown()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Vazio", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Empty(overview.Matches);
        Assert.Equal(0m, overview.Accrued);
        Assert.Equal(0m, overview.Settled);
        Assert.Equal(0m, overview.Due);
        Assert.Equal(0m, overview.InAnalysis);
        Assert.Empty(overview.Settlements);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_WithPaidConfirmation_ReturnsMatchWithFee()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, evt, _, conf) = await SeedPaidFutsalMatchAsync(ctx.db);

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Single(overview.Matches);
        var match = overview.Matches[0];
        Assert.Equal(evt.Id, match.EventId);
        Assert.Equal(0.75m, match.FeeAmount);
        Assert.Equal(1, match.PaidPlayers);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, match.Status);
        Assert.Equal(0.75m, overview.Accrued);
        Assert.Equal(0.75m, overview.Due);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_GroupsConfirmationsByEvent()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Racha", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 15m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var u1 = TestDataFactory.CreateUserWithPixKey("u1", "J1", "k1");
        var u2 = TestDataFactory.CreateUserWithPixKey("u2", "J2", "k2");
        ctx.db.Users.AddRange(u1, u2);
        await ctx.db.SaveChangesAsync();

        var c1 = TestDataFactory.CreateEventConfirmation(evt, u1);
        c1.PaymentStatus = EventConfirmationPaymentStatus.Paid; c1.HasPaid = true; c1.PlatformFeeAmount = 0.75m;
        var c2 = TestDataFactory.CreateEventConfirmation(evt, u2);
        c2.PaymentStatus = EventConfirmationPaymentStatus.Paid; c2.HasPaid = true; c2.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.AddRange(c1, c2);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Single(overview.Matches);
        Assert.Equal(2, overview.Matches[0].PaidPlayers);
        Assert.Equal(1.50m, overview.Accrued);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_OnlyCountsPaidConfirmationsWithFee()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Racha", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 15m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var u1 = TestDataFactory.CreateUserWithPixKey("u1", "J1", "k1");
        var u2 = TestDataFactory.CreateUserWithPixKey("u2", "J2", "k2");
        ctx.db.Users.AddRange(u1, u2);
        await ctx.db.SaveChangesAsync();

        var c1 = TestDataFactory.CreateEventConfirmation(evt, u1);
        c1.PaymentStatus = EventConfirmationPaymentStatus.Paid; c1.HasPaid = true; c1.PlatformFeeAmount = 0.75m;
        var c2 = TestDataFactory.CreateEventConfirmation(evt, u2); // pending, no fee
        c2.PaymentStatus = EventConfirmationPaymentStatus.Pending; c2.HasPaid = false;
        ctx.db.EventConfirmations.AddRange(c1, c2);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Single(overview.Matches);
        Assert.Equal(1, overview.Matches[0].PaidPlayers);
        Assert.Equal(0.75m, overview.Accrued);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_WithEmAnaliseSettlement_ReturnsInAnalysisAmount()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, user, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Equal(0.75m, overview.InAnalysis);
        Assert.Equal(0m, overview.Settled);
        Assert.Equal(0.75m, overview.Due); // EmAnalise does not reduce Due
        Assert.Single(overview.Settlements);
        Assert.Equal(PlatformFeeSettlementStatus.EmAnalise, overview.Settlements[0].Status);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_WithPaidSettlement_ReturnsSettledAndReducesDue()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, evt, user, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Pago,
            SelectedEventIds = evt.Id.ToString()
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Equal(0.75m, overview.Settled);
        Assert.Equal(0m, overview.Due);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_SettlementsOrderedBySubmittedAtDesc()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, user, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        var older = new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            SubmittedAt = new DateTime(2026, 1, 1), Status = PlatformFeeSettlementStatus.Pago
        };
        var newer = new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            SubmittedAt = new DateTime(2026, 8, 1), Status = PlatformFeeSettlementStatus.EmAnalise
        };
        ctx.db.PlatformFeeSettlements.AddRange(older, newer);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Equal(2, overview.Settlements.Count);
        Assert.Equal(newer.SubmittedAt, overview.Settlements[0].SubmittedAt);
        Assert.Equal(older.SubmittedAt, overview.Settlements[1].SubmittedAt);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_ReturnsPlatformPixFromConfig()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, _, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Equal("plataforma@confirmai.com", overview.PlatformPixKey);
        Assert.Equal("Sao Paulo", overview.PlatformPixCity);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_NoPlatformPixConfig_ReturnsEmptyKey()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, _, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        var options = Options.Create(new FeeOptions { Enabled = true, ManualPlatformFeeFixed = 0.75m });
        var ledger = new PlatformFeeLedgerService(ctx.factory, options);
        var service = new PlatformFeeSettlementQueryService(ctx.factory, options, ledger);
        var overview = await service.GetGroupFeeOverviewAsync(group.Id);

        Assert.Equal(string.Empty, overview.PlatformPixKey);
    }

    [Fact]
    public async Task GetGroupFeeOverviewAsync_DoesNotLeakOtherGroupsData()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (groupA, _, _, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        var groupB = TestDataFactory.CreateGroup("Outro", enablePaymentGateways: false);
        groupB.Sport = Sport.Futsal;
        ctx.db.Groups.Add(groupB);
        await ctx.db.SaveChangesAsync();

        var evtB = TestDataFactory.CreateEvent(groupB, "2026-08-10", 15m);
        ctx.db.Events.Add(evtB);
        await ctx.db.SaveChangesAsync();

        var uB = TestDataFactory.CreateUserWithPixKey("ub", "JB", "kb");
        ctx.db.Users.Add(uB);
        await ctx.db.SaveChangesAsync();

        var cB = TestDataFactory.CreateEventConfirmation(evtB, uB);
        cB.PaymentStatus = EventConfirmationPaymentStatus.Paid; cB.HasPaid = true; cB.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(cB);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var overview = await service.GetGroupFeeOverviewAsync(groupA.Id);

        Assert.Equal(0.75m, overview.Accrued); // only groupA's fee
        Assert.Single(overview.Matches);
    }

    // ── GetReviewQueueAsync (Fase B) ───────────────────────────────────────

    [Fact]
    public async Task GetReviewQueueAsync_NoSettlements_ReturnsEmpty()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var service = CreateService(ctx);
        var queue = await service.GetReviewQueueAsync();

        Assert.Empty(queue.Groups);
        Assert.Empty(queue.PendingSettlements);
    }

    [Fact]
    public async Task GetReviewQueueAsync_GroupOwingWithoutAnySettlement_IsListed()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, _, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        var service = CreateService(ctx);
        var queue = await service.GetReviewQueueAsync();

        var g = Assert.Single(queue.Groups);
        Assert.Equal(group.Id, g.GroupId);
        Assert.Equal(0.75m, g.Accrued);
        Assert.Equal(0m, g.Settled);
        Assert.Equal(0.75m, g.Due);
        Assert.Empty(queue.PendingSettlements);
    }

    [Fact]
    public async Task GetReviewQueueAsync_ReturnsGroupsWithAccruedAndSettled()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, user, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.50m, SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Pago
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var queue = await service.GetReviewQueueAsync();

        Assert.Single(queue.Groups);
        var g = queue.Groups[0];
        Assert.Equal(group.Id, g.GroupId);
        Assert.Equal("Racha", g.GroupName);
        Assert.Equal(0.75m, g.Accrued);
        Assert.Equal(0.50m, g.Settled);
        Assert.Equal(0.25m, g.Due);
        Assert.Equal(0m, g.InAnalysis);
    }

    [Fact]
    public async Task GetReviewQueueAsync_PendingSettlementsOrderedBySubmittedAtDesc()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, user, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            SubmittedAt = new DateTime(2026, 1, 1), Status = PlatformFeeSettlementStatus.EmAnalise
        });
        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            SubmittedAt = new DateTime(2026, 8, 1), Status = PlatformFeeSettlementStatus.EmAnalise
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var queue = await service.GetReviewQueueAsync();

        Assert.Equal(2, queue.PendingSettlements.Count);
        Assert.Equal(new DateTime(2026, 8, 1), queue.PendingSettlements[0].SubmittedAt);
        Assert.Equal(new DateTime(2026, 1, 1), queue.PendingSettlements[1].SubmittedAt);
    }

    [Fact]
    public async Task GetReviewQueueAsync_PendingSettlementsOnlyEmAnalise()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _, user, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        });
        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Pago
        });
        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 0.75m, SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Rejeitado
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var queue = await service.GetReviewQueueAsync();

        Assert.Single(queue.PendingSettlements);
        Assert.Equal(0.75m, queue.Groups[0].InAnalysis);
    }

    [Fact]
    public async Task GetReviewQueueAsync_GroupsWithInAnalysisFirst()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (groupA, _, userA, _) = await SeedPaidFutsalMatchAsync(ctx.db);

        var groupB = TestDataFactory.CreateGroup("Outro", enablePaymentGateways: false);
        groupB.Sport = Sport.Futsal;
        ctx.db.Groups.Add(groupB);
        await ctx.db.SaveChangesAsync();

        var evtB = TestDataFactory.CreateEvent(groupB, "2026-08-10", 15m);
        ctx.db.Events.Add(evtB);
        await ctx.db.SaveChangesAsync();
        var uB = TestDataFactory.CreateUserWithPixKey("ub", "JB", "kb");
        ctx.db.Users.Add(uB);
        await ctx.db.SaveChangesAsync();
        var cB = TestDataFactory.CreateEventConfirmation(evtB, uB);
        cB.PaymentStatus = EventConfirmationPaymentStatus.Paid; cB.HasPaid = true; cB.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(cB);
        await ctx.db.SaveChangesAsync();

        // groupA has EmAnalise; groupB has only Pago
        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = groupA.Id, Amount = 0.75m, SubmittedByUserId = userA.Id,
            Status = PlatformFeeSettlementStatus.EmAnalise
        });
        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = groupB.Id, Amount = 0.75m, SubmittedByUserId = uB.Id,
            Status = PlatformFeeSettlementStatus.Pago
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var queue = await service.GetReviewQueueAsync();

        Assert.Equal(groupA.Id, queue.Groups[0].GroupId); // InAnalysis first
        Assert.Equal(groupB.Id, queue.Groups[1].GroupId);
    }
}
