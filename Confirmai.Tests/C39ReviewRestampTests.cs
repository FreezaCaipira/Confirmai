using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Payment;
using Microsoft.Extensions.Options;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Review C39-A — o carimbo do preco protege quem ja pagou, mas nao pode
/// deixar a divida de quem ainda deve diferente do valor anunciado depois
/// de uma edicao; e torneio anterior ao C39 nao passa a cobrar ao ser editado.
/// </summary>
public class C39ReviewRestampTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static PlatformFeePolicy Policy() => new(Options.Create(new FeeOptions
    {
        ManualPlatformFeeFixed = 0.75m,
        PokerFeePercentMin = 3m,
        PokerFeePercentMax = 10m,
    }));

    private static Event PokerEvent(decimal? price, decimal? pct, params EventConfirmation[] confs)
    {
        var ev = new Event
        {
            Id = 1, Sport = Sport.Poker, Price = price, PlatformFeePercent = pct, BuyInAmount = price,
            Group = new Group { Id = 1, Name = "G", Sport = Sport.Poker, EnablePaymentGateways = false },
        };
        foreach (var c in confs) ev.Confirmations.Add(c);
        return ev;
    }

    private static EventConfirmation Conf(int id, decimal price, decimal fee) => new()
    {
        Id = id, EventId = 1, UserId = $"u{id}", ConfirmedAt = T0.AddMinutes(id),
        ChargedPrice = price, PlatformFeeAmount = fee,
    };

    [Fact]
    public void RestampUnpaid_DevedorSegueONovoValor_PagoEComprovanteMantemCarimbo()
    {
        var unpaid = Conf(1, 200m, 10m);
        var paid = Conf(2, 200m, 10m);
        paid.HasPaid = true;
        var proof = Conf(3, 200m, 10m);
        proof.PixProofUploadedAt = T0;
        var ev = PokerEvent(100m, 5m, unpaid, paid, proof);

        Policy().RestampUnpaid(ev);

        Assert.Equal(100m, unpaid.ChargedPrice);
        Assert.Equal(5m, unpaid.PlatformFeeAmount);
        Assert.Equal(200m, paid.ChargedPrice);
        Assert.Equal(10m, paid.PlatformFeeAmount);
        Assert.Equal(200m, proof.ChargedPrice);
        Assert.Equal(10m, proof.PlatformFeeAmount);
    }

    [Fact]
    public void RestampUnpaid_IsencaoVigenteNaConfirmacaoContinuaZero()
    {
        var conf = Conf(1, 200m, 0m);
        var ev = PokerEvent(300m, 5m, conf);
        ev.Group.PlatformFeeWaivedFrom = T0.AddDays(-1);
        ev.Group.PlatformFeeWaivedUntil = T0.AddDays(1);

        Policy().RestampUnpaid(ev);

        Assert.Equal(300m, conf.ChargedPrice);
        Assert.Equal(0m, conf.PlatformFeeAmount);
    }

    [Fact]
    public void RestampUnpaid_PartidaFicaGratis_DevedorNaoDeveMais()
    {
        var conf = Conf(1, 200m, 10m);
        var ev = PokerEvent(null, null, conf);

        Policy().RestampUnpaid(ev);

        Assert.Null(EventCharge.PriceOf(conf));
        Assert.Null(conf.PlatformFeeAmount);
    }

    [Fact]
    public void TournamentPrice_TorneioAnteriorAoC39_ContinuaPagoNaMesa()
        => Assert.Null(EventCharge.TournamentPrice(new Event { Price = null, BuyInAmount = 150m }, 150m));

    [Fact]
    public void TournamentPrice_TorneioGratisPassaACobrar()
        => Assert.Equal(80m, EventCharge.TournamentPrice(new Event { Price = null, BuyInAmount = 0m }, 80m));

    [Fact]
    public void TournamentPrice_TorneioPagoMudaOValor()
        => Assert.Equal(250m, EventCharge.TournamentPrice(new Event { Price = 200m, BuyInAmount = 200m }, 250m));

    [Fact]
    public void TournamentPrice_BuyInZero_SemCobranca()
        => Assert.Null(EventCharge.TournamentPrice(new Event { Price = 200m, BuyInAmount = 200m }, 0m));

    [Theory]
    [InlineData("Futsal")]
    [InlineData("Poker")]
    public void Edit_RecarimbaDevedoresAntesDeSalvar(string sport)
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", sport, "Edit.razor.cs"));
        var restamp = code.IndexOf("FeePolicy.RestampUnpaid(ev)", StringComparison.Ordinal);
        var save = code.IndexOf("await db.SaveChangesAsync()", StringComparison.Ordinal);
        Assert.True(restamp > 0, $"{sport}/Edit precisa recarimbar quem ainda deve ao mudar o valor");
        Assert.True(restamp < save);
        Assert.Contains(".Include(e => e.Confirmations)", code);
    }

    [Fact]
    public void PokerEdit_PixChecadoPeloPrecoEfetivo()
    {
        var code = File.ReadAllText(Path.Combine(RepoRoot(), "Pages", "Poker", "Edit.razor.cs"));
        Assert.Contains("EventCharge.TournamentPrice(ev, form.BuyInAmount)", code);
        Assert.Contains("newPrice != ev.Price", code);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }
}
