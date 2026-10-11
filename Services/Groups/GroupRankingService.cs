using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Groups;

/// <summary>
/// C40 F2 — carrega os inputs do ranking (so futsal, partida iniciada e com
/// placar, jogador escalado) e os vencedores de destaque por partida. O
/// calculo em si e puro em <see cref="GroupRanking.Build"/>.
/// </summary>
public sealed class GroupRankingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public GroupRankingService(IDbContextFactory<AppDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public record RankingData(
        List<GroupRanking.Entry> Entries,
        List<(int EventId, DateTime StartsAt, string WinnerUserId)> MvpWinners);

    public async Task<RankingData> LoadAsync(int groupId, DateTime utcNow)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entries = await db.EventConfirmations
            .Where(c => c.Event.GroupId == groupId
                     && c.Event.Sport == Sport.Futsal
                     && c.Event.IsActive
                     && c.Event.StartsAt < utcNow
                     && c.Event.ScoreTeamA != null
                     && c.Event.ScoreTeamB != null
                     && (c.TeamId == 0 || c.TeamId == 1)
                     && c.User != null)
            .Select(c => new GroupRanking.Entry(
                c.UserId,
                c.User!.FullName ?? c.User.UserName ?? c.UserId,
                c.Event.StartsAt,
                c.TeamId,
                c.Event.ScoreTeamA,
                c.Event.ScoreTeamB,
                c.EventId))
            .ToListAsync();

        // destaques (MVP por partida) seguem a mesma janela: futsal, ativo, passado
        var votes = await db.PostMatchVotes
            .Where(v => v.Event.GroupId == groupId
                     && v.Event.Sport == Sport.Futsal
                     && v.Event.IsActive
                     && v.Event.StartsAt < utcNow)
            .Select(v => new { v.EventId, v.VotedForUserId, v.Event.StartsAt })
            .ToListAsync();

        var mvpWinners = votes
            .GroupBy(v => v.EventId)
            .Select(g => (
                EventId: g.Key,
                StartsAt: g.First().StartsAt,
                WinnerUserId: g.GroupBy(v => v.VotedForUserId)
                               .OrderByDescending(vg => vg.Count())
                               .First().Key))
            .ToList();

        return new RankingData(entries, mvpWinners);
    }
}
