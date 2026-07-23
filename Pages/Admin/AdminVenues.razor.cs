using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Confirmai.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.Admin;

public partial class AdminVenues
{
    private const int PageSize = 20;

    private List<Venue> all      = new();
    private List<Venue> filtered = new();
    private bool        isLoading = true;
    private int         currentPage = 1;
    private int?        confirmDeleteId;
    private string      deleteError = string.Empty;

    private string  filterName  = string.Empty;
    private string  filterType  = string.Empty;
    private string  filterState = string.Empty;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        all = await db.Venues
            .OrderBy(v => v.StateCode)
            .ThenBy(v => v.City)
            .ThenBy(v => v.Name)
            .ToListAsync();
        ApplyFilter();
        isLoading = false;
    }

    private void ApplyFilter()
    {
        var q = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filterName))
            q = q.Where(v => v.Name.Contains(filterName, StringComparison.OrdinalIgnoreCase)
                           || v.City.Contains(filterName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filterType) && int.TryParse(filterType, out var typeInt))
            q = q.Where(v => (int)v.Type == typeInt);

        if (filterState == "active")   q = q.Where(v => v.IsActive);
        if (filterState == "inactive") q = q.Where(v => !v.IsActive);

        filtered = q.ToList();
        currentPage = 1;
    }

    private void ClearFilters()
    {
        filterName  = string.Empty;
        filterType  = string.Empty;
        filterState = string.Empty;
        ApplyFilter();
    }

    private async Task ToggleActive(Venue venue)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var entity = await db.Venues.FindAsync(venue.Id);
        if (entity is null) return;
        entity.IsActive = !entity.IsActive;
        await db.SaveChangesAsync();
        venue.IsActive = entity.IsActive;
        var evtType = entity.IsActive ? "venue.activated" : "venue.deactivated";
        var label   = entity.IsActive ? "ativada" : "desativada";
        await LogService.AuditAsync(evtType, "Venue", entity.Id.ToString(),
            $"Quadra {label}: {entity.Name}");
    }

    private void PrevPage() { if (currentPage > 1) currentPage--; }
    private void NextPage() { if (currentPage < TotalPages) currentPage++; }

    private async Task DeleteVenue(int id)
    {
        confirmDeleteId = null;
        deleteError     = string.Empty;

        await using var db = await DbFactory.CreateDbContextAsync();
        var venue = await db.Venues
            .Include(v => v.Events)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venue is null) return;

        if (venue.Events.Count > 0)
        {
            deleteError = $"\"{venue.Name}\" possui {venue.Events.Count} evento(s) vinculado(s) e não pode ser removida.";
            return;
        }

        db.Venues.Remove(venue);
        await db.SaveChangesAsync();
        await LogService.AuditAsync("venue.deleted", "Venue", id.ToString(), $"Quadra removida: {venue.Name}");

        all.RemoveAll(v => v.Id == id);
        ApplyFilter();
    }

    private static string VenueTypeLabel(VenueType t) => t switch
    {
        VenueType.Quadra  => "Quadra",
        VenueType.Society => "Society",
        VenueType.Campo   => "Campo",
        _                 => t.ToString()
    };
}
