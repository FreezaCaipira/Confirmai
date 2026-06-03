using Confirmai.Services.Utility;
using Microsoft.JSInterop;

namespace Confirmai.Services.Admin;

public sealed class AdminUsersFilterState
{
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int? Page { get; init; }
}

public class AdminUsersFilterStateService
{
    private readonly IJSRuntime _js;

    public AdminUsersFilterStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<AdminUsersFilterState> LoadAsync()
    {
        try
        {
            var userName = await LocalStorageStateHelpers.GetStringAsync(_js, AdminUsersStorageKeys.UserNameFilter);
            var email = await LocalStorageStateHelpers.GetStringAsync(_js, AdminUsersStorageKeys.EmailFilter);
            var role = await LocalStorageStateHelpers.GetStringAsync(_js, AdminUsersStorageKeys.RoleFilter);
            var status = await LocalStorageStateHelpers.GetStringAsync(_js, AdminUsersStorageKeys.StatusFilter);
            var page = await LocalStorageStateHelpers.GetPositiveIntAsync(_js, AdminUsersStorageKeys.Page);

            return new AdminUsersFilterState
            {
                UserName = userName,
                Email = email,
                Role = role,
                Status = status,
                Page = page
            };
        }
        catch
        {
            return new AdminUsersFilterState();
        }
    }

    public async Task SaveAsync(AdminUsersFilterState state)
    {
        try
        {
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminUsersStorageKeys.UserNameFilter, state.UserName);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminUsersStorageKeys.EmailFilter, state.Email);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminUsersStorageKeys.RoleFilter, state.Role);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminUsersStorageKeys.StatusFilter, state.Status);
            await LocalStorageStateHelpers.SetPageAsync(_js, AdminUsersStorageKeys.Page, state.Page);
        }
        catch
        {
            // No-op: ignore storage failures.
        }
    }
}

