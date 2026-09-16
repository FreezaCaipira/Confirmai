using System.Net.Http.Json;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Shared;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class AdminVenueEdit : IAsyncDisposable
{
    [Parameter] public int Id { get; set; }

    private Venue            model           = new();
    private bool             isLoading       = true;
    private bool             isSaving        = false;
    private string           saveError       = string.Empty;
    private List<string>     cityOptions     = new();

    private static readonly string[] BrazilianStates =
        ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
         "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IConfiguration Config { get; set; } = default!;

    private async Task LoadCitiesAsync()
    {
        if (string.IsNullOrWhiteSpace(model.StateCode)) return;
        try
        {
            var http   = HttpClientFactory.CreateClient();
            var result = await http.GetFromJsonAsync<IbgeMunicipio[]>(
                $"https://servicodados.ibge.gov.br/api/v1/localidades/estados/{model.StateCode}/municipios?orderBy=nome");
            cityOptions = result?.Select(m => m.Nome).ToList() ?? new();
        }
        catch { cityOptions = new(); }
    }

    private record IbgeMunicipio(string Nome);

    // Places autocomplete
    private DotNetObjectReference<AdminVenueEdit>? _objRef;
    private bool _acInit = false;

    // Venue admin assignment
    private ApplicationUser? venueAdmin;
    private ApplicationUser? searchedUser;
    private string           adminSearchEmail = string.Empty;
    private string           adminSearchError = string.Empty;
    private bool             isAssigning      = false;

    private bool IsNew => Id == 0;

    protected override async Task OnInitializedAsync()
    {
        if (!IsNew)
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            var found = await db.Venues
                .Include(v => v.VenueAdmin)
                .FirstOrDefaultAsync(v => v.Id == Id);
            if (found is not null)
            {
                model      = found;
                venueAdmin = found.VenueAdmin;
            }
        }
        await LoadCitiesAsync();
        isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_acInit && !isLoading)
        {
            _acInit = true;
            var apiKey = Config["Google:MapsApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("SET_VIA"))
            {
                _objRef ??= DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("venueAutocomplete.init", apiKey, _objRef, "admin-venue-name-input");
            }
        }
    }

    [JSInvokable]
    public async Task PlaceSelected(string name, string street, string city, string state)
    {
        model.Name      = name;
        model.Address   = street;
        model.City      = city;
        model.StateCode = state.Length > 2 ? state[..2] : state.ToUpperInvariant();
        await LoadCitiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_objRef is not null)
        {
            try { await JS.InvokeVoidAsync("venueAutocomplete.dispose"); } catch { }
            _objRef.Dispose();
        }
    }

    private async Task SearchUser()
    {
        adminSearchError = string.Empty;
        searchedUser     = null;
        var email = adminSearchEmail.Trim();
        if (string.IsNullOrWhiteSpace(email)) return;

        var user = await UserManager.FindByEmailAsync(email);
        if (user is null)
            adminSearchError = T["AdminVenues.UserNotFoundByEmail"];
        else
            searchedUser = user;
    }

    private async Task HandleAdminSearchKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await SearchUser();
    }

    private async Task AssignVenueAdmin()
    {
        if (searchedUser is null) return;
        isAssigning = true;

        await using var db = await DbFactory.CreateDbContextAsync();
        var venue = await db.Venues.FindAsync(Id);
        if (venue is null) { isAssigning = false; return; }

        venue.VenueAdminUserId = searchedUser.Id;
        await db.SaveChangesAsync();

        // Ensure the user has the venue_manager role
        if (!await UserManager.IsInRoleAsync(searchedUser, "venue_manager"))
            await UserManager.AddToRoleAsync(searchedUser, "venue_manager");

        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LogService.AuditAsync("venue.admin_assigned", "Venue", Id.ToString(),
            $"Admin da quadra atribuído: {searchedUser.Email}", userId);

        venueAdmin       = searchedUser;
        searchedUser     = null;
        adminSearchEmail = string.Empty;
        isAssigning      = false;
    }

    private async Task RemoveVenueAdmin()
    {
        if (venueAdmin is null) return;
        isAssigning = true;

        await using var db = await DbFactory.CreateDbContextAsync();
        var venue = await db.Venues.FindAsync(Id);
        if (venue is null) { isAssigning = false; return; }

        var removedUser = venueAdmin;
        venue.VenueAdminUserId = null;
        await db.SaveChangesAsync();

        // Remove venue_manager role only if user has no other venues assigned
        var hasOtherVenues = await db.Venues
            .AnyAsync(v => v.VenueAdminUserId == removedUser.Id);
        if (!hasOtherVenues)
            await UserManager.RemoveFromRoleAsync(removedUser, "venue_manager");

        var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await LogService.AuditAsync("venue.admin_removed", "Venue", Id.ToString(),
            $"Admin da quadra removido: {removedUser.Email}", userId);

        venueAdmin  = null;
        isAssigning = false;
    }

    private async Task Save()
    {
        isSaving  = true;
        saveError = string.Empty;

        try
        {
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var db = await DbFactory.CreateDbContextAsync();

            if (IsNew)
            {
                model.CreatedByUserId = userId;
                model.CreatedAt       = DateTime.UtcNow;
                db.Venues.Add(model);
                await LogService.AuditAsync("venue.created", "Venue", null, $"Nova quadra criada: {model.Name}", userId);
            }
            else
            {
                db.Venues.Update(model);
                await LogService.AuditAsync("venue.updated", "Venue", model.Id.ToString(), $"Quadra editada: {model.Name}", userId);
            }

            await db.SaveChangesAsync();
            NavigationManager.NavigateTo("/admin/venues");
        }
        catch (Exception ex)
        {
            saveError = $"Erro ao salvar: {ex.Message}";
            isSaving  = false;
        }
    }

    private static string VenueTypeLabel(VenueType t) => t switch
    {
        VenueType.Quadra  => "Quadra",
        VenueType.Society => "Society",
        VenueType.Campo   => "Campo",
        _                 => t.ToString()
    };
}
