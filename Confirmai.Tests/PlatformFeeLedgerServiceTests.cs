using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

public class PlatformFeeLedgerServiceTests
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
        return new PlatformFeeLedgerService(ctx.factory, options);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_FutsalManualWithPrice_StampsFee()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.True(result);
        await using var verifyDb = ctx.factory.CreateDbContext();
        var saved = verifyDb.EventConfirmations.Single();
        Assert.Equal(0.75m, saved.PlatformFeeAmount);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_AlreadyStamped_DoesNotDoubleStamp()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_NonFutsal_DoesNotStamp()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Poker;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_GatewayEnabled_DoesNotStamp()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: true);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_ZeroPrice_DoesNotStamp()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_NotPaid_DoesNotStamp()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Pending;
        conf.HasPaid = false;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task StampFeeOnPaidAsync_ZeroManualFee_DoesNotStamp()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx, manualFee: 0m);
        var result = await service.StampFeeOnPaidAsync(conf.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task GetGroupBalanceAsync_NoSettlements_ReturnsAccruedOnly()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var (accrued, settled, due) = await service.GetGroupBalanceAsync(group.Id);

        Assert.Equal(0.75m, accrued);
        Assert.Equal(0m, settled);
        Assert.Equal(0.75m, due);
    }

    [Fact]
    public async Task GetGroupBalanceAsync_WithPaidSettlement_ReturnsCorrectDue()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.50m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Pago
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var (accrued, settled, due) = await service.GetGroupBalanceAsync(group.Id);

        Assert.Equal(0.75m, accrued);
        Assert.Equal(0.50m, settled);
        Assert.Equal(0.25m, due);
    }

    [Fact]
    public async Task GetGroupBalanceAsync_WithRejectedSettlement_DoesNotCountAsSettled()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = TestDataFactory.CreateGroup("Test", enablePaymentGateways: false);
        group.Sport = Sport.Futsal;
        ctx.db.Groups.Add(group);
        await ctx.db.SaveChangesAsync();

        var user = TestDataFactory.CreateUserWithPixKey("u1", "Player 1", "pix@test");
        ctx.db.Users.Add(user);
        await ctx.db.SaveChangesAsync();

        var evt = TestDataFactory.CreateEvent(group, "2026-08-10", 20.0m);
        ctx.db.Events.Add(evt);
        await ctx.db.SaveChangesAsync();

        var conf = TestDataFactory.CreateEventConfirmation(evt, user);
        conf.PaymentStatus = EventConfirmationPaymentStatus.Paid;
        conf.HasPaid = true;
        conf.PlatformFeeAmount = 0.75m;
        ctx.db.EventConfirmations.Add(conf);
        await ctx.db.SaveChangesAsync();

        var settlement = new PlatformFeeSettlement
        {
            GroupId = group.Id,
            Amount = 0.75m,
            SubmittedByUserId = user.Id,
            Status = PlatformFeeSettlementStatus.Rejeitado
        };
        ctx.db.PlatformFeeSettlements.Add(settlement);
        await ctx.db.SaveChangesAsync();

        var service = CreateService(ctx);
        var (accrued, settled, due) = await service.GetGroupBalanceAsync(group.Id);

        Assert.Equal(0.75m, accrued);
        Assert.Equal(0m, settled);
        Assert.Equal(0.75m, due);
    }
}
