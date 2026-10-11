using System.Text;

namespace Confirmai.Services.Groups;

/// <summary>
/// C40 F2 — calculo puro do ranking do grupo (testavel sem banco).
/// Conta apenas futsal com placar registrado e jogador escalado (TeamId 0/1);
/// Pts = 3 x vitorias + 1 x empates; ordem Pts &gt; V &gt; Destaques &gt; Nome;
/// posicao compartilhada quando a linha inteira empata (1, 1, 3).
/// Sem temporada/premiacao — decisao do Robson (11/10/2026).
/// </summary>
public static class GroupRanking
{
    /// <summary>Uma confirmacao numa partida com placar (input por jogador/jogo).</summary>
    public record Entry(
        string UserId,
        string UserName,
        DateTime StartsAt,
        int? TeamId,
        int? ScoreTeamA,
        int? ScoreTeamB,
        int EventId = 0);

    /// <summary>Linha pronta da tabela. Position ja vem compartilhada no empate.</summary>
    public record Row(
        string UserId,
        string UserName,
        int Position,
        int GamesPlayed,
        int Wins,
        int Draws,
        int Losses,
        int Points,
        int Highlights);

    public static List<Row> Build(
        IEnumerable<Entry> entries, IReadOnlyDictionary<string, int> highlightCounts)
    {
        var rows = entries
            // reserva/sem time e partida sem placar nao contam jogo
            .Where(e => e.TeamId is 0 or 1 && e.ScoreTeamA.HasValue && e.ScoreTeamB.HasValue)
            .GroupBy(e => e.UserId)
            .Select(g =>
            {
                var wins = g.Count(e =>
                    (e.TeamId == 0 && e.ScoreTeamA > e.ScoreTeamB) ||
                    (e.TeamId == 1 && e.ScoreTeamB > e.ScoreTeamA));
                var draws = g.Count(e => e.ScoreTeamA == e.ScoreTeamB);
                return new Row(
                    g.Key,
                    g.First().UserName,
                    Position: 0,
                    GamesPlayed: g.Count(),
                    Wins: wins,
                    Draws: draws,
                    Losses: g.Count() - wins - draws,
                    Points: wins * 3 + draws,
                    Highlights: highlightCounts.GetValueOrDefault(g.Key, 0));
            })
            .OrderByDescending(r => r.Points)
            .ThenByDescending(r => r.Wins)
            .ThenByDescending(r => r.Highlights)
            .ThenBy(r => r.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // posicao compartilhada: mesmo Pts + V + Destaques = mesma posicao
        var position = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            if (i == 0
                || rows[i].Points != rows[i - 1].Points
                || rows[i].Wins != rows[i - 1].Wins
                || rows[i].Highlights != rows[i - 1].Highlights)
            {
                position = i + 1;
            }
            rows[i] = rows[i] with { Position = position };
        }
        return rows;
    }

    /// <summary>Tabela em texto para copiar/colar no WhatsApp do grupo.</summary>
    public static string BuildWhatsAppText(IReadOnlyList<Row> rows, string groupName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"*Ranking — {groupName}*");
        foreach (var r in rows)
            sb.AppendLine($"{r.Position}. {r.UserName} — {r.Points} pts ({r.GamesPlayed}J {r.Wins}V {r.Draws}E)");
        return sb.ToString().TrimEnd();
    }
}
