using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared.Components;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Review C39-B: cash game keeps <c>Event.Price</c> null and charges the table price
/// stamped on the confirmation, so every "owes / price" reader must go through
/// <c>ChargedPrice ?? Event.Price</c>.
/// </summary>
public class C39BReviewChargedPriceReadersTests
{
    private static EventConfirmation CashConf(decimal? charged, bool paid = false, bool proofSent = false)
        => new()
        {
            Event = new Event
            {
                GroupId = 1, Group = new Group { Id = 1, Name = "Casa", Sport = Sport.Poker },
                Sport = Sport.Poker, PokerEventType = PokerEventType.CashGame,
                Price = null, IsActive = true, StartsAt = DateTime.UtcNow.AddDays(1),
            },
            PriceOptionId = 7, ChargedPrice = charged,
            HasPaid = paid, PaymentStatus = EventConfirmationPaymentStatus.Pending,
            PixProofUploadedAt = proofSent ? DateTime.UtcNow : null,
            ConfirmedAt = DateTime.UtcNow.AddHours(-1),
        };

    private static object? EvalCard(EventConfirmation conf, string prop)
    {
        var card = new ConfirmationCard();
        typeof(ConfirmationCard).GetProperty("Confirmation")!.SetValue(card, conf);
        typeof(ConfirmationCard).GetProperty("IsPast")!.SetValue(card, false);
        typeof(ConfirmationCard).GetProperty("IsCancelled")!.SetValue(card, false);
        return typeof(ConfirmationCard).GetProperty(prop,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(card);
    }

    [Fact]
    public void Card_CashTableUnpaid_ShowsPayFromStampedPrice()
    {
        var conf = CashConf(300m);
        Assert.Equal(300m, EvalCard(conf, "Price"));
        Assert.True((bool)EvalCard(conf, "ShowPayAction")!);
    }

    [Fact]
    public void Card_CashTableProofSent_ShowsProofBadge()
        => Assert.True((bool)EvalCard(CashConf(300m, proofSent: true), "ProofSent")!);

    [Fact]
    public void Card_CashFreeTable_DoesNotOwe()
        => Assert.False((bool)EvalCard(CashConf(null), "OwesPayment")!);

    [Fact]
    public void PaymentPage_GatewaysPrice_DoesNotDereferenceEventPrice()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot(), "Pages/Payment/EventPayment.razor"));
        Assert.DoesNotContain("ev.Price!.Value", src);
        Assert.Contains("EventCharge.PriceOf(conf)", src);
    }

    [Fact]
    public void GroupHub_NextEventOwes_ReadsStampedPrice()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot(), "Pages/Groups/Detail.razor"));
        Assert.Contains("myConfirmation.ChargedPrice ?? nextEvent.Price", src);
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
