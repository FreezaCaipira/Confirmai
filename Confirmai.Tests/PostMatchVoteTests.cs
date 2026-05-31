using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class PostMatchVoteTests
{
    // ── Vote insert / upsert ─────────────────────────────────────────────────

    [Fact]
    public async Task CastVote_WhenNoneExists_InsertsNewRecord()
    {
        using var db = TestDataFactory.CreateDbContext();

        db.PostMatchVotes.Add(new PostMatchVote
        {
            EventId        = 1,
            VoterUserId    = "u1",
            VotedForUserId = "u2",
            VotedAt        = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var vote = await db.PostMatchVotes.SingleAsync();
        Assert.Equal("u1", vote.VoterUserId);
        Assert.Equal("u2", vote.VotedForUserId);
        Assert.Equal(1,    vote.EventId);
    }

    [Fact]
    public async Task CastVote_WhenVoteExists_UpdatesVotedForAndTimestamp()
    {
        using var db = TestDataFactory.CreateDbContext();

        var original = DateTime.UtcNow.AddMinutes(-5);
        db.PostMatchVotes.Add(new PostMatchVote
        {
            EventId        = 1,
            VoterUserId    = "u1",
            VotedForUserId = "u2",
            VotedAt        = original,
        });
        await db.SaveChangesAsync();

        // Mirrors the upsert path in CastVote
        var existing = await db.PostMatchVotes
            .FirstOrDefaultAsync(v => v.EventId == 1 && v.VoterUserId == "u1");
        Assert.NotNull(existing);

        existing.VotedForUserId = "u3";
        existing.VotedAt        = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var updated = await db.PostMatchVotes.SingleAsync();
        Assert.Equal("u3", updated.VotedForUserId);
        Assert.True(updated.VotedAt > original);
    }

    [Fact]
    public async Task CastVote_DifferentVoters_SameEvent_StoredSeparately()
    {
        using var db = TestDataFactory.CreateDbContext();

        db.PostMatchVotes.AddRange(
            new PostMatchVote { EventId = 1, VoterUserId = "u1", VotedForUserId = "u3" },
            new PostMatchVote { EventId = 1, VoterUserId = "u2", VotedForUserId = "u3" });
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.PostMatchVotes.CountAsync());
    }

    // ── MVP threshold reveal ──────────────────────────────────────────────────

    [Theory]
    [InlineData(10, 5,  true)]   // exactly half → revealed
    [InlineData(10, 6,  true)]   // above half → revealed
    [InlineData(10, 4,  false)]  // below half → hidden
    [InlineData(1,  1,  true)]   // single voter → threshold = 1
    [InlineData(1,  0,  false)]  // single voter, no vote yet
    [InlineData(3,  2,  true)]   // ceil(3 * 0.5) = 2, voteCount = 2
    [InlineData(3,  1,  false)]  // ceil(3 * 0.5) = 2, voteCount = 1
    public void MvpReveal_ThresholdIsHalfCeiling(int totalVoters, int voteCount, bool expectedRevealed)
    {
        // Mirrors the revealed formula in Escalacao.razor
        bool revealed = voteCount >= Math.Max(1, (int)Math.Ceiling(totalVoters * 0.5));
        Assert.Equal(expectedRevealed, revealed);
    }

    // ── MVP winner calculation ────────────────────────────────────────────────

    [Fact]
    public async Task MvpCalculation_ReturnsPlayerWithMostVotes()
    {
        using var db = TestDataFactory.CreateDbContext();

        // u3 receives 2 votes, u2 receives 1 → u3 is MVP
        db.PostMatchVotes.AddRange(
            new PostMatchVote { EventId = 1, VoterUserId = "u1", VotedForUserId = "u3" },
            new PostMatchVote { EventId = 1, VoterUserId = "u2", VotedForUserId = "u3" },
            new PostMatchVote { EventId = 1, VoterUserId = "u4", VotedForUserId = "u2" });
        await db.SaveChangesAsync();

        var allVotes = await db.PostMatchVotes.ToListAsync();
        var mvp = allVotes
            .GroupBy(v => v.VotedForUserId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        Assert.NotNull(mvp);
        Assert.Equal("u3", mvp.Key);
        Assert.Equal(2, mvp.Count());
    }

    // ── Score registration ────────────────────────────────────────────────────

    [Fact]
    public async Task SaveScore_SetsRegisteredByUserIdAndAt()
    {
        using var db = TestDataFactory.CreateDbContext();

        var group = new Group { Name = "Grupo Teste", InviteCode = "abc123" };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var ev = new Event { GroupId = group.Id, StartsAt = DateTime.UtcNow.AddDays(-1) };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        // Mirrors SaveScore logic
        var before = DateTime.UtcNow;
        var dbEv = await db.Events.FindAsync(ev.Id);
        Assert.NotNull(dbEv);

        dbEv.ScoreTeamA              = 3;
        dbEv.ScoreTeamB              = 1;
        dbEv.ScoreRegisteredByUserId = "admin-1";
        dbEv.ScoreRegisteredAt       = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Events.FindAsync(ev.Id);
        Assert.Equal(3,         saved!.ScoreTeamA);
        Assert.Equal(1,         saved.ScoreTeamB);
        Assert.Equal("admin-1", saved.ScoreRegisteredByUserId);
        Assert.True(saved.ScoreRegisteredAt >= before);
    }
}
