using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeFifoBreakdownTests
{
    private static FeeOptions TestFeeOptions(decimal manualFee = 0.75m) => new()
    {
        Enabled = true,
        AppFeeFixed = 0.50m,
        GatewayFeeFixed = 0.25m,
        ManualPlatformFeeFixed = manualFee
    };

    private static PlatformFeeLedgerService CreateService(
        (AppDbContext db, IDbContextFactory<AppDbContext> factory) ctx,
        decimal manualFee = 0.75m)
    {
        var options = Options.Create(TestFeeOptions(manualFee));
        return new PlatformFeeLedgerService(ctx.factory, new PlatformFeePolicy(options));
    }

    /// <summary>Seeds a group with N paid futsal confirmations spread across distinct events (one per match).</summary>
    private static async Task<(Group group, List<Event> events)> SeedMatchesAsync(
        AppDbContext db, int matchCount, decimal feePerMatch = 0.75m)
    {
        var group = TestDataFactory.CreateGroup("Racha", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var events = new List<Event>();
        for (int i = 0; i < matchCount; i++)
        {
            var evt = TestDataFactory.CreateEvent(group, $"2026-08-{10 + i:D2}", 15m);
            db.Events.Add(evt);
            await db.SaveChangesAsync();

            var user = TestDataFactory.CreateUserWithPixKey($"u{i}", $"J{i}", $"pix@j{i}");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var conf = TestDataFactory.CreateEventConfirmation(evt, user);
            conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
            conf.HasPaid = true;
            conf.PlatformFeeAmount = feePerMatch;
            db.EventConfirmations.Add(conf);
            await db.SaveChangesAsync();
            events.Add(evt);
        }

        return (group, events);
    }

    private static async Task AddPaidSettlementAsync(
        AppDbContext db, int groupId, string userId, decimal amount, DateTime submittedAt,
        int[]? selectedEventIds = null, decimal feePerMatch = 0.75m)
    {
        db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = groupId,
            Amount = amount,
            SubmittedByUserId = userId,
            SubmittedAt = submittedAt,
            Status = PlatformFeeSettlementStatus.Pago,
            Items = (selectedEventIds ?? Array.Empty<int>())
                .Select(eid => new PlatformFeeSettlementItem
                {
                    EventId = eid,
                    FeeAmount = feePerMatch
                })
                .ToList()
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_NoSettlements_AllPending()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _) = await SeedMatchesAsync(ctx.db, matchCount: 3);

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.All(breakdown, m => Assert.Equal(PlatformFeeMatchStatus.Pendente, m.Status));
        Assert.Equal(2.25m, breakdown.Sum(m => m.FeeAmount));
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_SettlementSelectsFirst_MarksFirstPaid()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 3);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 0.75m, new DateTime(2026, 8, 1),
            selectedEventIds: new[] { events[0].Id });

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.Equal(PlatformFeeMatchStatus.Pago, breakdown[0].Status);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, breakdown[1].Status);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, breakdown[2].Status);
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_SettlementSelectsTwo_MarksBothPaid()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 3, feePerMatch: 0.75m);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 1.50m, new DateTime(2026, 8, 1),
            selectedEventIds: new[] { events[0].Id, events[1].Id });

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.Equal(PlatformFeeMatchStatus.Pago, breakdown[0].Status);
        Assert.Equal(PlatformFeeMatchStatus.Pago, breakdown[1].Status);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, breakdown[2].Status);
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_SettlementSelectsAll_MarksAllPaid()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 2, feePerMatch: 0.75m);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 1.50m, new DateTime(2026, 8, 1),
            selectedEventIds: new[] { events[0].Id, events[1].Id });

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(2, breakdown.Count);
        Assert.All(breakdown, m => Assert.Equal(PlatformFeeMatchStatus.Pago, m.Status));
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_RejectedSettlement_DoesNotAbateAnything()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 3, feePerMatch: 0.75m);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 2.25m, SubmittedByUserId = user.Id,
            SubmittedAt = new DateTime(2026, 8, 1), Status = PlatformFeeSettlementStatus.Rejeitado,
            Items = events.Select(e => new PlatformFeeSettlementItem
            {
                EventId = e.Id,
                FeeAmount = 0.75m
            }).ToList()
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.All(breakdown, m => Assert.Equal(PlatformFeeMatchStatus.Pendente, m.Status));
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_EmAnaliseSettlement_DoesNotAbateAnything()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 3, feePerMatch: 0.75m);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        ctx.db.PlatformFeeSettlements.Add(new PlatformFeeSettlement
        {
            GroupId = group.Id, Amount = 2.25m, SubmittedByUserId = user.Id,
            SubmittedAt = new DateTime(2026, 8, 1), Status = PlatformFeeSettlementStatus.EmAnalise,
            Items = events.Select(e => new PlatformFeeSettlementItem
            {
                EventId = e.Id,
                FeeAmount = 0.75m
            }).ToList()
        });
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.All(breakdown, m => Assert.Equal(PlatformFeeMatchStatus.Pendente, m.Status));
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_MultiplePaidSettlements_SelectDifferentMatches()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 3, feePerMatch: 0.75m);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        // First settlement covers match 1; second settlement covers match 2
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 0.75m, new DateTime(2026, 8, 1),
            selectedEventIds: new[] { events[0].Id });
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 0.75m, new DateTime(2026, 8, 15),
            selectedEventIds: new[] { events[1].Id });

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.Equal(PlatformFeeMatchStatus.Pago, breakdown[0].Status);
        Assert.Equal(PlatformFeeMatchStatus.Pago, breakdown[1].Status);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, breakdown[2].Status);
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_PaidSettlementWithoutSelection_LeavesAllPending()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, _) = await SeedMatchesAsync(ctx.db, matchCount: 3, feePerMatch: 0.75m);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();
        // Legacy settlement without SelectedEventIds -> no matches covered
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 2.25m, new DateTime(2026, 8, 1));

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Equal(3, breakdown.Count);
        Assert.All(breakdown, m => Assert.Equal(PlatformFeeMatchStatus.Pendente, m.Status));
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_LatePayerAfterApprovedSettlement_KeepsResidualPending()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var (group, events) = await SeedMatchesAsync(ctx.db, matchCount: 1);
        var user = TestDataFactory.CreateUserWithPixKey("org", "Org", "pix@org");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();
        await AddPaidSettlementAsync(ctx.db, group.Id, user.Id, 0.75m, new DateTime(2026, 8, 1),
            selectedEventIds: new[] { events[0].Id });

        // A late player pays the same match after the settlement was approved:
        // the match accrues another R$0,75 that nobody has transferred yet.
        var late = TestDataFactory.CreateUserWithPixKey("late", "Late", "pix@late");
        ctx.db.Users.Add(late);
        await ctx.db.SaveChangesAsync();
        var lateConf = TestDataFactory.CreateEventConfirmation(events[0], late);
        lateConf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        lateConf.HasPaid = true;
        lateConf.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(lateConf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        var match = Assert.Single(breakdown);
        Assert.Equal(PlatformFeeMatchStatus.Pendente, match.Status);
        Assert.Equal(0.75m, match.FeeAmount);
    }

    [Fact]
    public async Task GetGroupFeeBreakdownByMatchAsync_NoFees_ReturnsEmpty()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Vazio", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var breakdown = await service.GetGroupFeeBreakdownByMatchAsync(group.Id);

        Assert.Empty(breakdown);
    }
}
