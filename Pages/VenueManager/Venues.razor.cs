using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Pages.VenueManager;

public partial class Venues
{
    private List<Venue> venues = new();
    private bool   isLoading     = true;
    private int?   confirmDeleteId;
    private string deleteError   = string.Empty;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            isLoading = false;
            return;
        }

        await using var db = await DbFactory.CreateDbContextAsync();

        // Admins see all venues; venue_managers see only their assigned venues
        var isAdmin = auth.User.IsInRole("admin");
        var query = db.Venues.AsNoTracking().AsQueryable();
        if (!isAdmin)
            query = query.Where(v => v.VenueAdminUserId == userId);

        venues = await query.OrderBy(v => v.Name).ToListAsync();
        isLoading = false;
    }

    private static string VenueTypeLabel(VenueType t) => t switch
    {
        VenueType.Quadra  => "Quadra",
        VenueType.Society => "Society",
        VenueType.Campo   => "Campo",
        _                 => t.ToString()
    };

    private async Task DeleteVenue(int id)
    {
        confirmDeleteId = null;
        deleteError     = string.Empty;

        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = auth.User.IsInRole("admin");

        await using var db = await DbFactory.CreateDbContextAsync();

        var venue = await db.Venues
            .Include(v => v.Events)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venue is null) return;

        if (!isAdmin && venue.VenueAdminUserId != userId)
        {
            deleteError = T["VenueManager.DeleteAccessDenied"];
            return;
        }

        if (venue.Events.Count > 0)
        {
            deleteError = $"\"{venue.Name}\" possui {venue.Events.Count} evento(s) vinculado(s) e não pode ser removida.";
            return;
        }

        db.Venues.Remove(venue);
        await db.SaveChangesAsync();
        await LogService.AuditAsync("venue.deleted", "Venue", id.ToString(), $"Quadra removida: {venue.Name}", userId);

        venues.RemoveAll(v => v.Id == id);
    }
}
