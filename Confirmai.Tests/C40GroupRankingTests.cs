using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Groups;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Tests;

/// <summary>
/// C40 F2 — ranking com pontos: so futsal, partida iniciada e COM placar;
/// jogador so conta jogo escalado (TeamId 0/1); Pts = 3V + 1E;
/// ordem Pts &gt; V &gt; Destaques &gt; Nome com posicao compartilhada no empate.
/// </summary>
public class C40GroupRankingTests
{
    private static GroupRanking.Entry E(
        string user, string? team = "A", int? scoreA = 1, int? scoreB = 0,
        string name = "", DateTime? startsAt = null)
        => new(
            user,
            name is "" ? user : name,
            startsAt ?? new DateTime(2026, 10, 1, 21, 0, 0, DateTimeKind.Utc),
            team is null ? null : team == "A" ? 0 : 1,
            scoreA, scoreB);

    private static readonly IReadOnlyDictionary<string, int> NoHighlights =
        new Dictionary<string, int>();

    [Fact]
    public void Build_OnlyScoredAndFielded_CountAsGames()
    {
        var entries = new[]
        {
            E("u1"),                          // escalado, com placar -> conta
            E("u1", team: null),              // reserva -> nao conta jogo
            E("u1", scoreA: null, scoreB: null), // partida sem placar -> nao conta
            E("u2", team: "B"),
        };

        var rows = GroupRanking.Build(entries, NoHighlights);

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows.First(r => r.UserId == "u1").GamesPlayed);
    }

    [Fact]
    public void Build_Points_Are3PerWin1PerDraw()
    {
        var entries = new[]
        {
            E("u1", "A", 3, 1), // vitoria
            E("u1", "A", 2, 2), // empate
            E("u1", "A", 0, 4), // derrota
            E("u2", "B", 3, 1), // vitoria de u2 (time B perdeu) -> derrota
        };

        var rows = GroupRanking.Build(entries, NoHighlights);
        var u1 = rows.First(r => r.UserId == "u1");
        var u2 = rows.First(r => r.UserId == "u2");

        Assert.Equal((3, 1, 1, 1, 4), (u1.GamesPlayed, u1.Wins, u1.Draws, u1.Losses, u1.Points));
        Assert.Equal((1, 0, 0, 1, 0), (u2.GamesPlayed, u2.Wins, u2.Draws, u2.Losses, u2.Points));
    }

    [Fact]
    public void Build_OrdersByPoints_ThenWins_ThenHighlights_ThenName()
    {
        var entries = new[]
        {
            E("u-carlos", "A", 1, 0, name: "Carlos"),          // 3 pts
            E("u-ana", "A", 2, 0, name: "Ana"),                // 3 pts, mesma rodada?
            E("u-bruno", "B", 0, 0, name: "Bruno"),            // 1 pt (empate)
        };
        var highlights = new Dictionary<string, int> { ["u-carlos"] = 2 };

        var rows = GroupRanking.Build(entries, highlights);

        // ana e carlos: 3 pts / 1 V / 0 destaques vs 2 destaques -> carlos na frente
        Assert.Equal("u-carlos", rows[0].UserId);
        Assert.Equal("u-ana", rows[1].UserId);
        Assert.Equal("u-bruno", rows[2].UserId);
    }

    [Fact]
    public void Build_SharedPosition_OnFullTie()
    {
        var entries = new[]
        {
            E("u1", "A", 1, 0), E("u2", "B", 1, 0), E("u3", "A", 2, 1), E("u4", "B", 2, 1),
        };
        // u1 e u3 venceram (3 pts, 1V); u2 e u4 perderam (0 pts)
        var rows = GroupRanking.Build(entries, NoHighlights);

        Assert.Equal(1, rows[0].Position);
        Assert.Equal(1, rows[1].Position);   // empate total com rows[0]
        Assert.Equal(3, rows[2].Position);   // posicao salta (1,1,3,3)
        Assert.Equal(3, rows[3].Position);
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmpty()
        => Assert.Empty(GroupRanking.Build(Array.Empty<GroupRanking.Entry>(), NoHighlights));

    [Fact]
    public void BuildWhatsAppText_FormatsPodium()
    {
        var entries = new[]
        {
            E("u1", "A", 2, 0, name: "Ana"),
            E("u1", "A", 1, 0, name: "Ana"),
            E("u2", "B", 0, 0, name: "Beto"),
        };
        var rows = GroupRanking.Build(entries, NoHighlights);

        var text = GroupRanking.BuildWhatsAppText(rows, "Racha do Zé");

        Assert.Contains("*Ranking — Racha do Zé*", text);
        Assert.Contains("1. Ana — 6 pts (2J 2V 0E)", text);
        Assert.Contains("2. Beto — 1 pts (1J 0V 1E)", text);
    }

    // ── service (EF): filtra poker / sem placar / futuro / inativo ──────────

    [Fact]
    public async Task LoadAsync_OnlyFutsalScoredFielded_PastEvents()
    {
        var ctx = TestDataFactory.CreateDbContextWithFactory();
        var group = new Group { Name = "G", Sport = Sport.Futsal };
        ctx.db.Groups.Add(group);
        var futsalScored = new Event
        {
            Group = group, Sport = Sport.Futsal, IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1), ScoreTeamA = 2, ScoreTeamB = 1,
        };
        var futsalNoScore = new Event
        {
            Group = group, Sport = Sport.Futsal, IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
        };
        var futsalFuture = new Event
        {
            Group = group, Sport = Sport.Futsal, IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(1), ScoreTeamA = 1, ScoreTeamB = 0,
        };
        var poker = new Event
        {
            Group = group, Sport = Sport.Poker, IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1), ScoreTeamA = 5, ScoreTeamB = 3,
        };
        ctx.db.Events.AddRange(futsalScored, futsalNoScore, futsalFuture, poker);
        ctx.db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1" });
        ctx.db.SaveChanges();
        foreach (var (ev, team) in new[] { (futsalScored, 0), (futsalNoScore, 0), (futsalFuture, 0), (poker, 0) })
            ctx.db.EventConfirmations.Add(new EventConfirmation { Event = ev, UserId = "u1", TeamId = team });
        // reserva em partida com placar -> fora
        ctx.db.EventConfirmations.Add(new EventConfirmation { Event = futsalScored, UserId = "u1", TeamId = null });
        ctx.db.SaveChanges();

        var svc = new GroupRankingService(ctx.factory);
        var data = await svc.LoadAsync(group.Id, DateTime.UtcNow);

        Assert.Single(data.Entries); // so a confirmacao escalada da partida com placar
        Assert.Equal(futsalScored.Id, data.Entries[0].EventId);
    }
}
