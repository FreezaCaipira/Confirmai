using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Futsal;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

public class EscalacaoServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
        => TestDbContextFactory.CreateInMemoryFactory($"esc-{Guid.NewGuid()}");

    private static async Task<(IDbContextFactory<AppDbContext> factory, EscalacaoService svc, int eventId, int groupId)> SetupWithEventAsync()
    {
        var factory = CreateFactory();
        var svc = new EscalacaoService(factory);

        await using var db = factory.CreateDbContext();
        var group = new Group { Name = "Test Group" };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var ev = new Event
        {
            GroupId = group.Id,
            Sport = Sport.Futsal,
            StartsAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 120,
            MaxPlayers = 10,
            Price = 0m
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return (factory, svc, ev.Id, group.Id);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullEvent_WhenNotFound()
    {
        var factory = CreateFactory();
        var svc = new EscalacaoService(factory);

        var result = await svc.LoadAsync(999, "user-1");

        Assert.Null(result.Event);
        Assert.Equal("Time A", result.TeamAName);
        Assert.Equal("Time B", result.TeamBName);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEvent_WhenFound()
    {
        var (factory, svc, eventId, groupId) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "user-1", UserName = "P1", FullName = "Player One" });
        db.GroupMembers.Add(new GroupMember { UserId = "user-1", GroupId = groupId, Role = GroupMemberRole.Member });
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "user-1", ConfirmedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await svc.LoadAsync(eventId, "user-1");

        Assert.NotNull(result.Event);
        Assert.True(result.IsMember);
        Assert.False(result.IsPastEvent);
    }

    [Fact]
    public async Task LoadAsync_IsPastEvent_True_WhenEventEnded()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var ev = await db.Events.FindAsync(eventId);
        ev!.StartsAt = DateTime.UtcNow.AddDays(-2);
        await db.SaveChangesAsync();

        var result = await svc.LoadAsync(eventId, null);

        Assert.True(result.IsPastEvent);
    }

    [Fact]
    public async Task SaveTeamNamesAsync_PersistsCustomNames()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();

        await svc.SaveTeamNamesAsync(eventId, "Amarelo", "Azul");

        await using var db = factory.CreateDbContext();
        var dbEv = await db.Events.FindAsync(eventId);
        Assert.Equal("Amarelo", dbEv!.TeamAName);
        Assert.Equal("Azul", dbEv.TeamBName);
    }

    [Fact]
    public async Task SaveTeamNamesAsync_NullsOutDefaultNames()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        var dbEv = await db.Events.FindAsync(eventId);
        dbEv!.TeamAName = "Custom";
        dbEv.TeamBName = "Custom2";
        await db.SaveChangesAsync();

        await svc.SaveTeamNamesAsync(eventId, "Time A", "Time B");

        await using var db2 = factory.CreateDbContext();
        var result = await db2.Events.FindAsync(eventId);
        Assert.Null(result!.TeamAName);
        Assert.Null(result.TeamBName);
    }

    [Fact]
    public async Task ConfirmLineupAsync_SetsTeamIdsAndConfirmedAt()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "P1" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "P2" });
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "u1", ConfirmedAt = DateTime.UtcNow });
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "u2", ConfirmedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var map = new Dictionary<string, int?> { ["u1"] = 0, ["u2"] = 1 };
        await svc.ConfirmLineupAsync(eventId, map);

        await using var db2 = factory.CreateDbContext();
        var confs = await db2.EventConfirmations.Where(c => c.EventId == eventId).ToListAsync();
        Assert.Equal(0, confs.First(c => c.UserId == "u1").TeamId);
        Assert.Equal(1, confs.First(c => c.UserId == "u2").TeamId);
        var dbEv = await db2.Events.FindAsync(eventId);
        Assert.NotNull(dbEv!.LineupConfirmedAt);
    }

    [Fact]
    public async Task ResetLineupAsync_ClearsTeamIdsAndConfirmedAt()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "P1" });
        db.EventConfirmations.Add(new EventConfirmation { EventId = eventId, UserId = "u1", TeamId = 0, ConfirmedAt = DateTime.UtcNow });
        var ev = await db.Events.FindAsync(eventId);
        ev!.LineupConfirmedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await svc.ResetLineupAsync(eventId);

        await using var db2 = factory.CreateDbContext();
        var conf = await db2.EventConfirmations.FirstAsync(c => c.EventId == eventId);
        Assert.Null(conf.TeamId);
        var dbEv = await db2.Events.FindAsync(eventId);
        Assert.Null(dbEv!.LineupConfirmedAt);
    }

    [Fact]
    public async Task CastVoteAsync_CreatesNewVote()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();

        await svc.CastVoteAsync(eventId, "voter-1", "voted-1");

        await using var db = factory.CreateDbContext();
        var vote = await db.PostMatchVotes.FirstOrDefaultAsync(v => v.EventId == eventId && v.VoterUserId == "voter-1");
        Assert.NotNull(vote);
        Assert.Equal("voted-1", vote!.VotedForUserId);
    }

    [Fact]
    public async Task CastVoteAsync_UpdatesExistingVote()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();
        await using var db = factory.CreateDbContext();
        db.PostMatchVotes.Add(new PostMatchVote { EventId = eventId, VoterUserId = "voter-1", VotedForUserId = "old" });
        await db.SaveChangesAsync();

        await svc.CastVoteAsync(eventId, "voter-1", "new");

        await using var db2 = factory.CreateDbContext();
        var vote = await db2.PostMatchVotes.FirstAsync(v => v.EventId == eventId && v.VoterUserId == "voter-1");
        Assert.Equal("new", vote.VotedForUserId);
    }

    [Fact]
    public async Task SaveScoreAsync_PersistsScore()
    {
        var (factory, svc, eventId, _) = await SetupWithEventAsync();

        await svc.SaveScoreAsync(eventId, 3, 2, "user-1");

        await using var db = factory.CreateDbContext();
        var dbEv = await db.Events.FindAsync(eventId);
        Assert.Equal(3, dbEv!.ScoreTeamA);
        Assert.Equal(2, dbEv.ScoreTeamB);
        Assert.Equal("user-1", dbEv.ScoreRegisteredByUserId);
    }

    [Fact]
    public async Task SaveScoreAsync_DoesNothing_WhenEventNotFound()
    {
        var factory = CreateFactory();
        var svc = new EscalacaoService(factory);
        await svc.SaveScoreAsync(999, 3, 2, "user-1");
    }
}
