using Confirmai.Models;

namespace Confirmai.Services.Groups;

/// <summary>Resumo legível da agenda fixa do grupo (dias, horário, local).</summary>
public sealed record GroupScheduleLine(
    IReadOnlyList<DayOfWeek> Days,
    TimeOnly? CommonTime,
    string? VenueName);

/// <summary>
/// C37 F8 — resume os MatchSchedules ativos de um grupo para exibição nos
/// cards da home. Função pura: recebe os schedules já carregados.
/// </summary>
public static class GroupScheduleSummary
{
    /// <summary>
    /// Dias em ordem de semana começando na segunda (convenção PT-BR).
    /// Retorna null quando não há agenda ativa. O horário só aparece quando
    /// todos os dias ativos dividem o mesmo — agendas mistas exibem só dias.
    /// </summary>
    public static GroupScheduleLine? Build(IEnumerable<MatchSchedule> schedules)
    {
        var active = schedules.Where(s => s.IsActive).ToList();
        if (active.Count == 0) return null;

        var days = active
            .Select(s => s.DayOfWeek)
            .Distinct()
            .OrderBy(d => ((int)d + 6) % 7) // Monday-first
            .ToList();

        var times = active.Select(s => s.TimeOfDay).Distinct().ToList();

        var venues = active
            .Select(s => s.Venue?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        return new GroupScheduleLine(
            days,
            times.Count == 1 ? times[0] : null,
            venues.Count > 0 ? string.Join(" · ", venues) : null);
    }
}
