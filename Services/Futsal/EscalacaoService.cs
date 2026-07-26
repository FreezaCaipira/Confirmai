using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Pages.Components;
using Confirmai.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Futsal;

public sealed record EscalacaoLoadResult(
    Event? Event,
    bool IsAdmin,
    bool IsPastEvent,
    bool IsMember,
    List<PostMatchVote> AllVotes,
    PostMatchVote? MyVote,
    string? ScoreRegisteredByName,
    string TeamAName,
    string TeamBName);

public class EscalacaoService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EscalacaoService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<EscalacaoLoadResult> LoadAsync(int eventId, string? currentUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var ev = await db.Events
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
            .Include(e => e.Confirmations)
                .ThenInclude(c => c.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Sport == Sport.Futsal);

        var isAdmin = EventAccess.IsAdmin(ev, currentUserId);

        bool isPastEvent = false;
        bool isMember = false;
        var allVotes = new List<PostMatchVote>();
        PostMatchVote? myVote = null;
        string? scoreRegisteredByName = null;
        string teamAName = "Time A";
        string teamBName = "Time B";

        if (ev is not null)
        {
            isPastEvent = ev.StartsAt.AddMinutes(ev.DurationMinutes ?? 120) < DateTime.UtcNow;
            isMember = currentUserId is not null &&
                       ev.Group.Members.Any(m => m.UserId == currentUserId);

            allVotes = await db.PostMatchVotes
                .Where(v => v.EventId == eventId)
                .ToListAsync();
            myVote = currentUserId is not null
                ? allVotes.FirstOrDefault(v => v.VoterUserId == currentUserId)
                : null;

            if (ev.ScoreRegisteredByUserId is not null)
            {
                scoreRegisteredByName = await db.Users
                    .Where(u => u.Id == ev.ScoreRegisteredByUserId)
                    .Select(u => u.FullName ?? u.Email)
                    .FirstOrDefaultAsync();
            }

            teamAName = ev.TeamAName ?? "Time A";
            teamBName = ev.TeamBName ?? "Time B";
        }

        return new EscalacaoLoadResult(
            ev, isAdmin, isPastEvent, isMember,
            allVotes, myVote, scoreRegisteredByName, teamAName, teamBName);
    }

    public async Task SaveTeamNamesAsync(int eventId, string teamAName, string teamBName)
    {
        var nameA = string.IsNullOrWhiteSpace(teamAName) ? "Time A" : teamAName.Trim();
        var nameB = string.IsNullOrWhiteSpace(teamBName) ? "Time B" : teamBName.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbEv = await db.Events.FindAsync(eventId);
        if (dbEv is null) return;
        dbEv.TeamAName = nameA == "Time A" ? null : nameA;
        dbEv.TeamBName = nameB == "Time B" ? null : nameB;
        await db.SaveChangesAsync();
    }

    public async Task ConfirmLineupAsync(int eventId, Dictionary<string, int?> userIdToTeam)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var confs = await db.EventConfirmations
            .Where(c => c.EventId == eventId)
            .ToListAsync();

        foreach (var c in confs)
        {
            if (userIdToTeam.TryGetValue(c.UserId, out var teamId))
                c.TeamId = teamId;
        }

        var dbEv = await db.Events.FindAsync(eventId);
        if (dbEv is not null)
            dbEv.LineupConfirmedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task ResetLineupAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var confs = await db.EventConfirmations
            .Where(c => c.EventId == eventId)
            .ToListAsync();
        foreach (var c in confs)
            c.TeamId = null;

        var dbEv = await db.Events.FindAsync(eventId);
        if (dbEv is not null)
            dbEv.LineupConfirmedAt = null;

        await db.SaveChangesAsync();
    }

    public async Task CastVoteAsync(int eventId, string voterUserId, string votedForUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.PostMatchVotes
            .FirstOrDefaultAsync(v => v.EventId == eventId && v.VoterUserId == voterUserId);
        if (existing is not null)
        {
            existing.VotedForUserId = votedForUserId;
            existing.VotedAt = DateTime.UtcNow;
        }
        else
        {
            db.PostMatchVotes.Add(new PostMatchVote
            {
                EventId = eventId,
                VoterUserId = voterUserId,
                VotedForUserId = votedForUserId,
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task SaveScoreAsync(int eventId, int scoreA, int scoreB, string? userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbEv = await db.Events.FindAsync(eventId);
        if (dbEv is not null)
        {
            dbEv.ScoreTeamA = scoreA;
            dbEv.ScoreTeamB = scoreB;
            dbEv.ScoreRegisteredByUserId = userId;
            dbEv.ScoreRegisteredAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
