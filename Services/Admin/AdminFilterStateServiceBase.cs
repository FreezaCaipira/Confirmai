using Microsoft.JSInterop;

namespace Confirmai.Services.Admin;

/// <summary>
/// Generic base class for admin filter state services that persist/restore
/// filter state to/from browser localStorage.
/// Eliminates the duplicated try/catch + Load/Save pattern across
/// AdminLogsFilterStateService, AdminUsersFilterStateService, and AdminPaymentsFilterStateService.
/// </summary>
/// <typeparam name="TState">The filter state record/class type.</typeparam>
public abstract class AdminFilterStateServiceBase<TState> where TState : new()
{
    protected readonly IJSRuntime Js;

    protected AdminFilterStateServiceBase(IJSRuntime js)
    {
        Js = js;
    }

    /// <summary>
    /// Loads filter state from localStorage.
    /// Returns a default instance if storage access fails.
    /// </summary>
    public async Task<TState> LoadAsync()
    {
        try
        {
            return await LoadCoreAsync();
        }
        catch
        {
            return new TState();
        }
    }

    /// <summary>
    /// Saves filter state to localStorage.
    /// Silently ignores storage failures.
    /// </summary>
    public async Task SaveAsync(TState state)
    {
        try
        {
            await SaveCoreAsync(state);
        }
        catch
        {
            // No-op: ignore storage failures.
        }
    }

    /// <summary>Override to implement the actual load logic (reads from localStorage).</summary>
    protected abstract Task<TState> LoadCoreAsync();

    /// <summary>Override to implement the actual save logic (writes to localStorage).</summary>
    protected abstract Task SaveCoreAsync(TState state);
}
