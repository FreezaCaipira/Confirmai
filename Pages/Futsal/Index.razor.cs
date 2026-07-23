using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Confirmai.Pages.Futsal;

public partial class Index
{
    [SupplyParameterFromQuery(Name = "cidade")] public string? City   { get; set; }
    [SupplyParameterFromQuery(Name = "estado")] public string? Estado { get; set; }

    private string? currentUserId;

    [Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
