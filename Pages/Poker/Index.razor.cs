using System.Security.Claims;
using Confirmai.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Confirmai.Pages.Poker;

public partial class Index : IAsyncDisposable
{
    [SupplyParameterFromQuery(Name = "cidade")] public string? City   { get; set; }
    [SupplyParameterFromQuery(Name = "estado")] public string? Estado { get; set; }

    private string? currentUserId;
    private bool showTypeMenu = false;

    private CancellationTokenSource? _menuFocusOutCts;
    private Task? _menuFocusOutTask = null;

    [Inject] private AuthenticationStateProvider AuthProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthProvider.GetAuthenticationStateAsync();
        currentUserId = auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task HandleMenuFocusOut()
    {
        _menuFocusOutCts?.Cancel();
        _menuFocusOutCts = new CancellationTokenSource();
        var ct = _menuFocusOutCts.Token;

        try
        {
            await Task.Delay(150, ct);
            if (!ct.IsCancellationRequested)
            {
                showTypeMenu = false;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        _menuFocusOutCts?.Cancel();

        if (_menuFocusOutTask is not null)
        {
            try { await _menuFocusOutTask; }
            catch (OperationCanceledException) { }
        }

        _menuFocusOutCts?.Dispose();
    }
}
