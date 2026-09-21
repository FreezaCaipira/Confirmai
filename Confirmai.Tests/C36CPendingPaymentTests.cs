using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Confirmai.Shared.Components;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C36-C Fase 4 — pending-payment signals: the home banner groups the player's
/// own unpaid confirmations; the ConfirmationCard offers "Pagar" only when the
/// player actually owes for a priced, active event.
/// </summary>
public class C36CPendingPaymentTests
{
    private static EventConfirmation Conf(
        int groupId = 1, string groupName = "Grupo",
        decimal? price = 15m, bool paid = false,
        EventConfirmationPaymentStatus status = EventConfirmationPaymentStatus.Pending,
        FutsalPosition position = FutsalPosition.Outfield,
        bool active = true, bool proofSent = false)
        => new()
        {
            Event = new Event
            {
                GroupId = groupId, Group = new Group { Id = groupId, Name = groupName },
                Sport = Sport.Futsal, Price = price, IsActive = active,
                StartsAt = DateTime.UtcNow.AddDays(-1),
            },
            HasPaid = paid, PaymentStatus = status, Position = position,
            PixProofUploadedAt = proofSent ? DateTime.UtcNow : null,
            ConfirmedAt = DateTime.UtcNow.AddDays(-2),
        };

    // ── PendingByGroup ──────────────────────────────────────────────────────

    [Fact]
    public void PendingByGroup_GroupsCountsPerGroup()
    {
        var confs = new[]
        {
            Conf(groupId: 1, groupName: "A"), Conf(groupId: 1, groupName: "A"),
            Conf(groupId: 2, groupName: "B"),
        };

        var result = GroupPaymentsService.PendingByGroup(confs);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].GroupId);
        Assert.Equal(2, result[0].Count);
        Assert.Equal(1, result[1].Count);
    }

    [Fact]
    public void PendingByGroup_ExcludesPaidProofSentGoalkeeperFreeCancelled()
    {
        var confs = new[]
        {
            Conf(paid: true, status: EventConfirmationPaymentStatus.Paid),
            Conf(proofSent: true),
            Conf(position: FutsalPosition.Goalkeeper),
            Conf(price: 0m),
            Conf(price: null),
            Conf(active: false),
            Conf(status: EventConfirmationPaymentStatus.Refunded),
        };

        Assert.Empty(GroupPaymentsService.PendingByGroup(confs));
    }

    // ── ConfirmationCard pay action (reflection — no bUnit) ────────────────

    private static bool EvalCard(EventConfirmation conf, bool isPast, bool isCancelled, string prop)
    {
        var card = new ConfirmationCard();
        typeof(ConfirmationCard).GetProperty("Confirmation")!.SetValue(card, conf);
        typeof(ConfirmationCard).GetProperty("IsPast")!.SetValue(card, isPast);
        typeof(ConfirmationCard).GetProperty("IsCancelled")!.SetValue(card, isCancelled);
        var p = typeof(ConfirmationCard).GetProperty(prop,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)p!.GetValue(card)!;
    }

    [Fact]
    public void Card_PendingPricedOutfield_ShowsPay()
        => Assert.True(EvalCard(Conf(), isPast: false, isCancelled: false, "ShowPayAction"));

    [Fact]
    public void Card_PastEventStillOwes_ShowsPay()
        => Assert.True(EvalCard(Conf(), isPast: true, isCancelled: false, "ShowPayAction"));

    [Fact]
    public void Card_ProofSent_HidesPay_ShowsProofBadge()
    {
        var conf = Conf(proofSent: true);
        Assert.False(EvalCard(conf, false, false, "ShowPayAction"));
        Assert.True(EvalCard(conf, false, false, "ProofSent"));
    }

    [Fact]
    public void Card_PaidGoalkeeperFreeCancelled_NeverShowPay()
    {
        Assert.False(EvalCard(Conf(paid: true, status: EventConfirmationPaymentStatus.Paid), false, false, "ShowPayAction"));
        Assert.False(EvalCard(Conf(position: FutsalPosition.Goalkeeper), false, false, "ShowPayAction"));
        Assert.False(EvalCard(Conf(price: null), false, false, "ShowPayAction"));
        Assert.False(EvalCard(Conf(), false, true, "ShowPayAction"));
    }
}
