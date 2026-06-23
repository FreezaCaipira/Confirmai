using Confirmai.Models;
using Confirmai.Enums;

namespace Confirmai.Tests;

public class EventTests
{
    [Fact]
    public void Id_CanBeSetAndGet()
    {
        var @event = new Event { Id = 1 };
        Assert.Equal(1, @event.Id);
    }

    [Fact]
    public void GroupId_CanBeSetAndGet()
    {
        var @event = new Event { GroupId = 5 };
        Assert.Equal(5, @event.GroupId);
    }

    [Fact]
    public void Sport_CanBeSetAndGet()
    {
        var @event = new Event { Sport = Sport.Futsal };
        Assert.Equal(Sport.Futsal, @event.Sport);
    }

    [Fact]
    public void Location_CanBeSetAndGet()
    {
        var @event = new Event { Location = "Test Location" };
        Assert.Equal("Test Location", @event.Location);
    }

    [Fact]
    public void Location_DefaultsToEmptyString()
    {
        var @event = new Event();
        Assert.Equal(string.Empty, @event.Location);
    }

    [Fact]
    public void StartsAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var @event = new Event { StartsAt = now };
        Assert.Equal(now, @event.StartsAt);
    }

    [Fact]
    public void MaxPlayers_CanBeSetAndGet()
    {
        var @event = new Event { MaxPlayers = 10 };
        Assert.Equal(10, @event.MaxPlayers);
    }

    [Fact]
    public void IsActive_CanBeSetAndGet()
    {
        var @event = new Event { IsActive = false };
        Assert.False(@event.IsActive);
    }

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        var @event = new Event();
        Assert.True(@event.IsActive);
    }

    [Fact]
    public void CreatedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var @event = new Event { CreatedAt = now };
        Assert.Equal(now, @event.CreatedAt);
    }

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var @event = new Event();
        var after = DateTime.UtcNow;
        Assert.InRange(@event.CreatedAt, before, after);
    }

    [Fact]
    public void CreatedByUserId_CanBeSetAndGet()
    {
        var @event = new Event { CreatedByUserId = "user-123" };
        Assert.Equal("user-123", @event.CreatedByUserId);
    }

    [Fact]
    public void CreatedByUserId_CanBeNull()
    {
        var @event = new Event { CreatedByUserId = null };
        Assert.Null(@event.CreatedByUserId);
    }

    [Fact]
    public void MaxGoalkeepers_CanBeSetAndGet()
    {
        var @event = new Event { MaxGoalkeepers = 2 };
        Assert.Equal(2, @event.MaxGoalkeepers);
    }

    [Fact]
    public void MaxGoalkeepers_CanBeNull()
    {
        var @event = new Event { MaxGoalkeepers = null };
        Assert.Null(@event.MaxGoalkeepers);
    }

    [Fact]
    public void PlayersPerSide_CanBeSetAndGet()
    {
        var @event = new Event { PlayersPerSide = 5 };
        Assert.Equal(5, @event.PlayersPerSide);
    }

    [Fact]
    public void PlayersPerSide_CanBeNull()
    {
        var @event = new Event { PlayersPerSide = null };
        Assert.Null(@event.PlayersPerSide);
    }

    [Fact]
    public void LocalName_CanBeSetAndGet()
    {
        var @event = new Event { LocalName = "Racha" };
        Assert.Equal("Racha", @event.LocalName);
    }

    [Fact]
    public void LocalName_CanBeNull()
    {
        var @event = new Event { LocalName = null };
        Assert.Null(@event.LocalName);
    }

    [Fact]
    public void VenueId_CanBeSetAndGet()
    {
        var @event = new Event { VenueId = 10 };
        Assert.Equal(10, @event.VenueId);
    }

    [Fact]
    public void VenueId_CanBeNull()
    {
        var @event = new Event { VenueId = null };
        Assert.Null(@event.VenueId);
    }

    [Fact]
    public void DurationMinutes_CanBeSetAndGet()
    {
        var @event = new Event { DurationMinutes = 90 };
        Assert.Equal(90, @event.DurationMinutes);
    }

    [Fact]
    public void DurationMinutes_CanBeNull()
    {
        var @event = new Event { DurationMinutes = null };
        Assert.Null(@event.DurationMinutes);
    }

    [Fact]
    public void Price_CanBeSetAndGet()
    {
        var @event = new Event { Price = 50.00m };
        Assert.Equal(50.00m, @event.Price);
    }

    [Fact]
    public void Price_CanBeNull()
    {
        var @event = new Event { Price = null };
        Assert.Null(@event.Price);
    }

    [Fact]
    public void RachaScheduleId_CanBeSetAndGet()
    {
        var @event = new Event { RachaScheduleId = 100 };
        Assert.Equal(100, @event.RachaScheduleId);
    }

    [Fact]
    public void RachaScheduleId_CanBeNull()
    {
        var @event = new Event { RachaScheduleId = null };
        Assert.Null(@event.RachaScheduleId);
    }

    [Fact]
    public void PokerEventType_CanBeSetAndGet()
    {
        var @event = new Event { PokerEventType = PokerEventType.Tournament };
        Assert.Equal(PokerEventType.Tournament, @event.PokerEventType);
    }

    [Fact]
    public void PokerEventType_CanBeNull()
    {
        var @event = new Event { PokerEventType = null };
        Assert.Null(@event.PokerEventType);
    }

    [Fact]
    public void Modality_CanBeSetAndGet()
    {
        var @event = new Event { Modality = PokerModality.Vanilla };
        Assert.Equal(PokerModality.Vanilla, @event.Modality);
    }

    [Fact]
    public void Modality_CanBeNull()
    {
        var @event = new Event { Modality = null };
        Assert.Null(@event.Modality);
    }

    [Fact]
    public void PokerHouseName_CanBeSetAndGet()
    {
        var @event = new Event { PokerHouseName = "Poker House" };
        Assert.Equal("Poker House", @event.PokerHouseName);
    }

    [Fact]
    public void PokerHouseName_CanBeNull()
    {
        var @event = new Event { PokerHouseName = null };
        Assert.Null(@event.PokerHouseName);
    }

    [Fact]
    public void LateRegEndsAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var @event = new Event { LateRegEndsAt = now };
        Assert.Equal(now, @event.LateRegEndsAt);
    }

    [Fact]
    public void LateRegEndsAt_CanBeNull()
    {
        var @event = new Event { LateRegEndsAt = null };
        Assert.Null(@event.LateRegEndsAt);
    }

    [Fact]
    public void StartingStack_CanBeSetAndGet()
    {
        var @event = new Event { StartingStack = 10000 };
        Assert.Equal(10000, @event.StartingStack);
    }

    [Fact]
    public void StartingStack_CanBeNull()
    {
        var @event = new Event { StartingStack = null };
        Assert.Null(@event.StartingStack);
    }

    [Fact]
    public void InitialBlindBB_CanBeSetAndGet()
    {
        var @event = new Event { InitialBlindBB = 25 };
        Assert.Equal(25, @event.InitialBlindBB);
    }

    [Fact]
    public void InitialBlindBB_CanBeNull()
    {
        var @event = new Event { InitialBlindBB = null };
        Assert.Null(@event.InitialBlindBB);
    }

    [Fact]
    public void GTD_CanBeSetAndGet()
    {
        var @event = new Event { GTD = 5000.00m };
        Assert.Equal(5000.00m, @event.GTD);
    }

    [Fact]
    public void GTD_CanBeNull()
    {
        var @event = new Event { GTD = null };
        Assert.Null(@event.GTD);
    }

    [Fact]
    public void BuyInAmount_CanBeSetAndGet()
    {
        var @event = new Event { BuyInAmount = 100.00m };
        Assert.Equal(100.00m, @event.BuyInAmount);
    }

    [Fact]
    public void BuyInAmount_CanBeNull()
    {
        var @event = new Event { BuyInAmount = null };
        Assert.Null(@event.BuyInAmount);
    }

    [Fact]
    public void RebuyAmount_CanBeSetAndGet()
    {
        var @event = new Event { RebuyAmount = 50.00m };
        Assert.Equal(50.00m, @event.RebuyAmount);
    }

    [Fact]
    public void RebuyAmount_CanBeNull()
    {
        var @event = new Event { RebuyAmount = null };
        Assert.Null(@event.RebuyAmount);
    }

    [Fact]
    public void AddonAmount_CanBeSetAndGet()
    {
        var @event = new Event { AddonAmount = 30.00m };
        Assert.Equal(30.00m, @event.AddonAmount);
    }

    [Fact]
    public void AddonAmount_CanBeNull()
    {
        var @event = new Event { AddonAmount = null };
        Assert.Null(@event.AddonAmount);
    }

    [Fact]
    public void RebuyDoubleAmount_CanBeSetAndGet()
    {
        var @event = new Event { RebuyDoubleAmount = 90.00m };
        Assert.Equal(90.00m, @event.RebuyDoubleAmount);
    }

    [Fact]
    public void RebuyDoubleAmount_CanBeNull()
    {
        var @event = new Event { RebuyDoubleAmount = null };
        Assert.Null(@event.RebuyDoubleAmount);
    }

    [Fact]
    public void AddonDoubleAmount_CanBeSetAndGet()
    {
        var @event = new Event { AddonDoubleAmount = 55.00m };
        Assert.Equal(55.00m, @event.AddonDoubleAmount);
    }

    [Fact]
    public void AddonDoubleAmount_CanBeNull()
    {
        var @event = new Event { AddonDoubleAmount = null };
        Assert.Null(@event.AddonDoubleAmount);
    }

    [Fact]
    public void CashMinBuyIn_CanBeSetAndGet()
    {
        var @event = new Event { CashMinBuyIn = 100.00m };
        Assert.Equal(100.00m, @event.CashMinBuyIn);
    }

    [Fact]
    public void CashMinBuyIn_CanBeNull()
    {
        var @event = new Event { CashMinBuyIn = null };
        Assert.Null(@event.CashMinBuyIn);
    }

    [Fact]
    public void CashMaxBuyIn_CanBeSetAndGet()
    {
        var @event = new Event { CashMaxBuyIn = 500.00m };
        Assert.Equal(500.00m, @event.CashMaxBuyIn);
    }

    [Fact]
    public void CashMaxBuyIn_CanBeNull()
    {
        var @event = new Event { CashMaxBuyIn = null };
        Assert.Null(@event.CashMaxBuyIn);
    }

    [Fact]
    public void CashIncludes_CanBeSetAndGet()
    {
        var @event = new Event { CashIncludes = "Dinner included" };
        Assert.Equal("Dinner included", @event.CashIncludes);
    }

    [Fact]
    public void CashIncludes_CanBeNull()
    {
        var @event = new Event { CashIncludes = null };
        Assert.Null(@event.CashIncludes);
    }

    [Fact]
    public void HomeGameCode_CanBeSetAndGet()
    {
        var @event = new Event { HomeGameCode = "XK7-492" };
        Assert.Equal("XK7-492", @event.HomeGameCode);
    }

    [Fact]
    public void HomeGameCode_CanBeNull()
    {
        var @event = new Event { HomeGameCode = null };
        Assert.Null(@event.HomeGameCode);
    }

    [Fact]
    public void BuyIn_CanBeSetAndGet()
    {
        var @event = new Event { BuyIn = "100" };
        Assert.Equal("100", @event.BuyIn);
    }

    [Fact]
    public void BuyIn_CanBeNull()
    {
        var @event = new Event { BuyIn = null };
        Assert.Null(@event.BuyIn);
    }

    [Fact]
    public void Rebuy_CanBeSetAndGet()
    {
        var @event = new Event { Rebuy = "50" };
        Assert.Equal("50", @event.Rebuy);
    }

    [Fact]
    public void Rebuy_CanBeNull()
    {
        var @event = new Event { Rebuy = null };
        Assert.Null(@event.Rebuy);
    }

    [Fact]
    public void Addon_CanBeSetAndGet()
    {
        var @event = new Event { Addon = "30" };
        Assert.Equal("30", @event.Addon);
    }

    [Fact]
    public void Addon_CanBeNull()
    {
        var @event = new Event { Addon = null };
        Assert.Null(@event.Addon);
    }

    [Fact]
    public void BonusInfo_CanBeSetAndGet()
    {
        var @event = new Event { BonusInfo = "Bonus info" };
        Assert.Equal("Bonus info", @event.BonusInfo);
    }

    [Fact]
    public void BonusInfo_CanBeNull()
    {
        var @event = new Event { BonusInfo = null };
        Assert.Null(@event.BonusInfo);
    }

    [Fact]
    public void LineupConfirmedAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var @event = new Event { LineupConfirmedAt = now };
        Assert.Equal(now, @event.LineupConfirmedAt);
    }

    [Fact]
    public void LineupConfirmedAt_CanBeNull()
    {
        var @event = new Event { LineupConfirmedAt = null };
        Assert.Null(@event.LineupConfirmedAt);
    }

    [Fact]
    public void ScoreTeamA_CanBeSetAndGet()
    {
        var @event = new Event { ScoreTeamA = 3 };
        Assert.Equal(3, @event.ScoreTeamA);
    }

    [Fact]
    public void ScoreTeamA_CanBeNull()
    {
        var @event = new Event { ScoreTeamA = null };
        Assert.Null(@event.ScoreTeamA);
    }

    [Fact]
    public void ScoreTeamB_CanBeSetAndGet()
    {
        var @event = new Event { ScoreTeamB = 2 };
        Assert.Equal(2, @event.ScoreTeamB);
    }

    [Fact]
    public void ScoreTeamB_CanBeNull()
    {
        var @event = new Event { ScoreTeamB = null };
        Assert.Null(@event.ScoreTeamB);
    }

    [Fact]
    public void ScoreRegisteredByUserId_CanBeSetAndGet()
    {
        var @event = new Event { ScoreRegisteredByUserId = "user-123" };
        Assert.Equal("user-123", @event.ScoreRegisteredByUserId);
    }

    [Fact]
    public void ScoreRegisteredByUserId_CanBeNull()
    {
        var @event = new Event { ScoreRegisteredByUserId = null };
        Assert.Null(@event.ScoreRegisteredByUserId);
    }

    [Fact]
    public void ScoreRegisteredAt_CanBeSetAndGet()
    {
        var now = DateTime.UtcNow;
        var @event = new Event { ScoreRegisteredAt = now };
        Assert.Equal(now, @event.ScoreRegisteredAt);
    }

    [Fact]
    public void ScoreRegisteredAt_CanBeNull()
    {
        var @event = new Event { ScoreRegisteredAt = null };
        Assert.Null(@event.ScoreRegisteredAt);
    }

    [Fact]
    public void TeamAName_CanBeSetAndGet()
    {
        var @event = new Event { TeamAName = "Team Yellow" };
        Assert.Equal("Team Yellow", @event.TeamAName);
    }

    [Fact]
    public void TeamAName_CanBeNull()
    {
        var @event = new Event { TeamAName = null };
        Assert.Null(@event.TeamAName);
    }

    [Fact]
    public void TeamBName_CanBeSetAndGet()
    {
        var @event = new Event { TeamBName = "Team Blue" };
        Assert.Equal("Team Blue", @event.TeamBName);
    }

    [Fact]
    public void TeamBName_CanBeNull()
    {
        var @event = new Event { TeamBName = null };
        Assert.Null(@event.TeamBName);
    }

    [Fact]
    public void Confirmations_DefaultsToEmptyList()
    {
        var @event = new Event();
        Assert.NotNull(@event.Confirmations);
        Assert.Empty(@event.Confirmations);
    }

    [Fact]
    public void WaitingList_DefaultsToEmptyList()
    {
        var @event = new Event();
        Assert.NotNull(@event.WaitingList);
        Assert.Empty(@event.WaitingList);
    }

    [Fact]
    public void PostMatchVotes_DefaultsToEmptyList()
    {
        var @event = new Event();
        Assert.NotNull(@event.PostMatchVotes);
        Assert.Empty(@event.PostMatchVotes);
    }
}
