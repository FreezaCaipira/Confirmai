using System.Net.Http.Json;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.VenueManager;

public partial class VenueEdit : IAsyncDisposable
{
    [Parameter] public int Id { get; set; }

    private Venue            model           = new();
    private bool             isLoading       = true;
    private bool             isSaving        = false;
    private bool             accessDenied    = false;
    private string           saveError       = string.Empty;
    private List<string>     cityOptions     = new();

    private static readonly string[] BrazilianStates =
        ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS",
         "MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];

    private record IbgeMunicipio(string Nome);

    private DotNetObjectReference<VenueEdit>? _objRef;
    private bool _acInit = false;

    private bool IsNew => Id == 0;

    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IConfiguration Config { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;

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

    protected override async Task OnInitializedAsync()
    {
        if (!IsNew)
        {
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = auth.User.IsInRole("admin");

            await using var db = await DbFactory.CreateDbContextAsync();
            var found = await db.Venues.FindAsync(Id);

            if (found is null)
            {
                isLoading = false;
                return;
            }

            // Venue managers can only edit venues assigned to them
            if (!isAdmin && found.VenueAdminUserId != userId)
            {
                accessDenied = true;
                isLoading    = false;
                return;
            }

            model = found;
        }

        await LoadCitiesAsync();
        isLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_acInit && !isLoading && !accessDenied)
        {
            _acInit = true;
            var apiKey = Config["Google:MapsApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.Contains("SET_VIA"))
            {
                _objRef ??= DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("venueAutocomplete.init", apiKey, _objRef, "venue-name-input");
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

    private async Task Save()
    {
        isSaving  = true;
        saveError = string.Empty;

        try
        {
            var auth   = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = auth.User.IsInRole("admin");

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
                // Re-check ownership before saving
                var existing = await db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == Id);
                if (existing is not null && !isAdmin && existing.VenueAdminUserId != userId)
                {
                    saveError = "Você não tem permissão para editar esta quadra.";
                    isSaving  = false;
                    return;
                }

                db.Venues.Update(model);
                await LogService.AuditAsync("venue.updated", "Venue", model.Id.ToString(), $"Quadra editada: {model.Name}", userId);
            }

            await db.SaveChangesAsync();
            NavigationManager.NavigateTo("/venue-manager/venues");
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
