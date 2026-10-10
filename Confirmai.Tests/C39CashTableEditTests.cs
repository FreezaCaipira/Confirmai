using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Confirmai.Services.Poker;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

/// <summary>
/// C39-B F10 — diff das mesas do cash na edicao: validacao (rotulo, preco,
/// % na faixa, mesa com pagante congelada), add/update/delete/desativa e
/// recarimbo limitado aos devedores da mesa alterada.
/// </summary>
public class C39CashTableEditTests
{
    private static PlatformFeePolicy Policy() => new(Options.Create(new FeeOptions
    {
        ManualPlatformFeeFixed = 0.75m,
        PokerFeePercentMin = 5m,
        PokerFeePercentMax = 10m,
    }));

    private static Event CashEvent()
    {
        var ev = new Event
        {
            Id = 1, Sport = Sport.Poker, PokerEventType = PokerEventType.CashGame,
            Group = new Group { Name = "G", Sport = Sport.Poker },
            IsActive = true,
        };
        ev.PriceOptions.Add(new EventPriceOption { Id = 1, Label = "M1", Price = 200m, PlatformFeePercent = 5m, IsActive = true, EventId = 1 });
        ev.PriceOptions.Add(new EventPriceOption { Id = 2, Label = "M2", Price = 500m, PlatformFeePercent = 10m, IsActive = true, EventId = 1 });
        return ev;
    }

    private static Dictionary<int, EventPriceOption> ById(Event ev)
        => ev.PriceOptions.ToDictionary(o => o.Id);

    [Fact]
    public void Validate_RequiredFieldsAndFeeRange()
    {
        var ev = CashEvent();
        var policy = Policy();
        var none = new Dictionary<int, EventPriceOption>();

        Assert.Equal("Poker.Create.TableLabelRequired",
            CashTableEdit.Validate([new CashTableFormRow(null, " ", 100m, 5m, true)], none, new HashSet<int>(), policy));
        Assert.Equal("Poker.Create.TablePriceInvalid",
            CashTableEdit.Validate([new CashTableFormRow(null, "M", -1m, 5m, true)], none, new HashSet<int>(), policy));
        Assert.Equal("Poker.Create.FeePercentOutOfRange",
            CashTableEdit.Validate([new CashTableFormRow(null, "M", 100m, 99m, true)], none, new HashSet<int>(), policy));
        // mesa gratis fora da faixa nao importa
        Assert.Null(CashTableEdit.Validate(
            [new CashTableFormRow(null, "M", 0m, 99m, true)], none, new HashSet<int>(), policy));
    }

    [Fact]
    public void Validate_LockedTable_FreezesPriceAndFee()
    {
        var ev = CashEvent();
        var policy = Policy();
        var locked = new HashSet<int> { 1 };

        // preco mudou em mesa com pagante -> recusa
        Assert.Equal("Poker.Edit.PriceLockedPaid",
            CashTableEdit.Validate(
                [new CashTableFormRow(1, "M1", 300m, 5m, true)], ById(ev), locked, policy));
        // % mudou -> recusa
        Assert.Equal("Poker.Edit.PriceLockedPaid",
            CashTableEdit.Validate(
                [new CashTableFormRow(1, "M1", 200m, 8m, true)], ById(ev), locked, policy));
        // rotulo e desativacao livres
        Assert.Null(CashTableEdit.Validate(
            [new CashTableFormRow(1, "Nome novo", 200m, 5m, false)], ById(ev), locked, policy));
    }

    [Fact]
    public void Apply_UpdatesAddsDeactivatesAndDeletes()
    {
        var ev = CashEvent();
        var used = new HashSet<int> { 1 }; // mesa 1 tem inscritos

        var plan = CashTableEdit.Apply(ev,
        [
            new CashTableFormRow(2, "M2 nova", 600m, 8m, true),
            new CashTableFormRow(null, "M3", 100m, 5m, true),
            // mesa 1 sumiu do form -> com inscritos, vira desativada
        ], used);

        var m1 = ev.PriceOptions.Single(o => o.Id == 1);
        var m2 = ev.PriceOptions.Single(o => o.Id == 2);
        Assert.False(m1.IsActive);
        Assert.Single(plan.Deactivated);
        Assert.Equal("M2 nova", m2.Label);
        Assert.Equal(600m, m2.Price);
        Assert.Equal(8m, m2.PlatformFeePercent);
        Assert.Single(plan.RestampedOptionIds);
        Assert.Contains(2, plan.RestampedOptionIds);
        Assert.Single(plan.Added);
        Assert.Equal("M3", plan.Added[0].Label);
    }

    [Fact]
    public void Apply_RemovesUnusedTable()
    {
        var ev = CashEvent();
        var plan = CashTableEdit.Apply(ev,
            [new CashTableFormRow(1, "M1", 200m, 5m, true)],
            new HashSet<int>()); // ninguem inscrito

        Assert.Single(ev.PriceOptions);
        Assert.Single(plan.Deleted);
        Assert.Equal(2, plan.Deleted[0].Id);
    }

    [Fact]
    public void Apply_IdDeOutroEvento_ViraNovaMesa()
    {
        var ev = CashEvent();
        var used = new HashSet<int>();
        // linha adulterada com Id inexistente neste evento
        var plan = CashTableEdit.Apply(ev,
            [new CashTableFormRow(999, "Fake", 10m, 5m, true)],
            used);

        Assert.Single(plan.Added);
        Assert.Equal("Fake", plan.Added[0].Label);
        Assert.Equal(2, plan.Deleted.Count); // mesas reais saem
    }

    [Fact]
    public void WillCharge_SomenteMesaAtivaComPreco()
    {
        Assert.True(CashTableEdit.WillCharge([new CashTableFormRow(null, "M", 100m, 5m, true)]));
        Assert.False(CashTableEdit.WillCharge([new CashTableFormRow(null, "M", 100m, 5m, false)]));
        Assert.False(CashTableEdit.WillCharge([new CashTableFormRow(null, "M", 0m, 5m, true)]));
        Assert.False(CashTableEdit.WillCharge(Array.Empty<CashTableFormRow>()));
    }

    [Fact]
    public void RestampUnpaidForTable_SoDevedoresDaquelaMesa()
    {
        var policy = Policy();
        var ev = CashEvent();
        var table = ev.PriceOptions.First(o => o.Id == 1);
        var other = ev.PriceOptions.First(o => o.Id == 2);

        ev.Confirmations.Add(new EventConfirmation
        {
            EventId = 1, UserId = "u1", PriceOptionId = 1,
            ChargedPrice = 200m, PlatformFeeAmount = 10m,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            ConfirmedAt = DateTime.UtcNow,
        });
        ev.Confirmations.Add(new EventConfirmation
        {
            EventId = 1, UserId = "u2", PriceOptionId = 1,
            ChargedPrice = 200m, PlatformFeeAmount = 10m,
            PaymentStatus = EventConfirmationPaymentStatus.Paid, HasPaid = true,
            ConfirmedAt = DateTime.UtcNow,
        });
        ev.Confirmations.Add(new EventConfirmation
        {
            EventId = 1, UserId = "u3", PriceOptionId = 2,
            ChargedPrice = 500m, PlatformFeeAmount = 50m,
            PaymentStatus = EventConfirmationPaymentStatus.Pending,
            ConfirmedAt = DateTime.UtcNow,
        });

        table.Price = 300m;
        table.PlatformFeePercent = 10m;
        policy.RestampUnpaidForTable(ev, table);

        var devedor = ev.Confirmations.Single(c => c.UserId == "u1");
        Assert.Equal(300m, devedor.ChargedPrice);
        Assert.Equal(30m, devedor.PlatformFeeAmount); // 10% de 300

        var pago = ev.Confirmations.Single(c => c.UserId == "u2");
        Assert.Equal(200m, pago.ChargedPrice); // carimbo protege quem pagou

        var outraMesa = ev.Confirmations.Single(c => c.UserId == "u3");
        Assert.Equal(500m, outraMesa.ChargedPrice); // mesa nao alterada intacta
    }
}
