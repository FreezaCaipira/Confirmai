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

public class AdminUsersFilterStateService : AdminFilterStateServiceBase<AdminUsersFilterState>
{
    public AdminUsersFilterStateService(IJSRuntime js) : base(js) { }

    protected override async Task<AdminUsersFilterState> LoadCoreAsync()
    {
        var userName = await LocalStorageStateHelpers.GetStringAsync(Js, AdminUsersStorageKeys.UserNameFilter);
        var email = await LocalStorageStateHelpers.GetStringAsync(Js, AdminUsersStorageKeys.EmailFilter);
        var role = await LocalStorageStateHelpers.GetStringAsync(Js, AdminUsersStorageKeys.RoleFilter);
        var status = await LocalStorageStateHelpers.GetStringAsync(Js, AdminUsersStorageKeys.StatusFilter);
        var page = await LocalStorageStateHelpers.GetPositiveIntAsync(Js, AdminUsersStorageKeys.Page);

        return new AdminUsersFilterState
        {
            UserName = userName,
            Email = email,
            Role = role,
            Status = status,
            Page = page
        };
    }

    protected override async Task SaveCoreAsync(AdminUsersFilterState state)
    {
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminUsersStorageKeys.UserNameFilter, state.UserName);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminUsersStorageKeys.EmailFilter, state.Email);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminUsersStorageKeys.RoleFilter, state.Role);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminUsersStorageKeys.StatusFilter, state.Status);
        await LocalStorageStateHelpers.SetPageAsync(Js, AdminUsersStorageKeys.Page, state.Page);
    }
}
