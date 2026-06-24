using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

/// <summary>
/// Service para operações relacionadas a confirmações de eventos do usuário.
/// </summary>
public class UserConfirmationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UserConfirmationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Carrega todas as confirmações de um usuário, separadas por status.
    /// </summary>
    public async Task<(List<EventConfirmation> Upcoming, List<EventConfirmation> Past, List<EventConfirmation> Cancelled)> 
        GetUserConfirmationsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return (new(), new(), new());

        await using var db = await _dbFactory.CreateDbContextAsync();
        var confs = await db.EventConfirmations
            .Include(c => c.Event).ThenInclude(e => e.Group)
            .Include(c => c.Event).ThenInclude(e => e.Confirmations)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Event.StartsAt)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var upcoming = confs.Where(c => c.Event.StartsAt >= now && c.Event.IsActive).ToList();
        var past = confs.Where(c => c.Event.StartsAt < now && c.Event.IsActive)
                       .OrderByDescending(c => c.Event.StartsAt).ToList();
        var cancelled = confs.Where(c => !c.Event.IsActive)
                            .OrderByDescending(c => c.Event.StartsAt).ToList();

        return (upcoming, past, cancelled);
    }

    /// <summary>
    /// Filtra uma lista de confirmações por esporte.
    /// </summary>
    public List<EventConfirmation> FilterBySport(List<EventConfirmation> confirmations, Sport? sport)
    {
        if (!sport.HasValue)
            return confirmations;

        return confirmations.Where(c => c.Event.Sport == sport.Value).ToList();
    }

    /// <summary>
    /// Filtra uma lista de confirmações por período de tempo.
    /// </summary>
    public List<EventConfirmation> FilterByPeriod(List<EventConfirmation> confirmations, string period)
    {
        if (period == "all")
            return confirmations;

        var days = period == "7days" ? 7 : 30;
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return confirmations.Where(c => c.Event.StartsAt >= cutoff).ToList();
    }

    /// <summary>
    /// Aplica múltiplos filtros a uma lista de confirmações.
    /// </summary>
    public List<EventConfirmation> ApplyFilters(List<EventConfirmation> confirmations, Sport? sport, string period)
    {
        var filtered = FilterBySport(confirmations, sport);
        filtered = FilterByPeriod(filtered, period);
        return filtered;
    }
}
