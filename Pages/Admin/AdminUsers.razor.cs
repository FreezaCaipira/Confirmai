using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Confirmai.Shared.Components.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class AdminUsers
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private AdminUsersFilterStateService AdminUsersFilterStateService { get; set; } = default!;
    [Inject] private AdminUsersQueryService AdminUsersQuery { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    [CascadingParameter] public Toast? ToastRef { get; set; }

    private Dictionary<string, string> userRoles = new();
    private List<ApplicationUser> currentPageUsers = new();
    private bool filtersLoaded;
    private bool showRestoredFiltersNotice;
    private int totalUsers;
    private int currentPage = 1;
    private int totalPages = 1;
    private const int PageSize = 20;

    private string filterUserName = "";
    private string filterEmail = "";
    private string filterRole = "";
    private string filterStatus = "";

    protected override async Task OnInitializedAsync() => await LoadUsersPageAsync();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || filtersLoaded) return;
        await LoadFilterStateFromStorageAsync();
        filtersLoaded = true;
        currentPage = 1;
        await LoadUsersPageAsync();
    }

    private async Task LoadUsersPageAsync()
    {
        var data = await AdminUsersQuery.LoadPageAsync(filterUserName, filterEmail, filterRole, filterStatus, currentPage, PageSize);
        userRoles = data.UserRoles;
        currentPageUsers = data.Users;
        totalUsers = data.TotalUsers;
        totalPages = data.TotalPages;
        if (currentPage > totalPages) currentPage = totalPages;
    }

    private async Task ApplyFiltersAndPersist()
    {
        showRestoredFiltersNotice = false;
        currentPage = 1;
        await LoadUsersPageAsync();
        await PersistFilterStateAsync();
    }

    private async Task GoToPrevPage() { if (currentPage > 1) { currentPage--; await LoadUsersPageAsync(); } }
    private async Task GoToNextPage() { if (currentPage < totalPages) { currentPage++; await LoadUsersPageAsync(); } }

    private async Task LockUser(string userId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", T["AdminUsers.ConfirmLock"])) return;
        var user = await UserManager.FindByIdAsync(userId);
        if (user == null) { ToastRef?.Show(T["AdminUsers.NotFoundToast"], "error"); return; }
        await UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await LogService.LogAsync(string.Format(T["AdminUsers.LogLocked"], userId), source: "Admin", level: "Warning", userId: userId);
        ToastRef?.Show(T["AdminUsers.LockedToast"], "warning");
        await LoadUsersPageAsync();
    }

    private async Task UnlockUser(string userId)
    {
        var user = await UserManager.FindByIdAsync(userId);
        if (user == null) { ToastRef?.Show(T["AdminUsers.NotFoundToast"], "error"); return; }
        await UserManager.SetLockoutEndDateAsync(user, null);
        await LogService.LogAsync(string.Format(T["AdminUsers.LogUnlocked"], userId), source: "Admin", level: "Info", userId: userId);
        ToastRef?.Show(T["AdminUsers.UnlockedToast"], "success");
        await LoadUsersPageAsync();
    }

    private Task EditUser(string userId) { NavigationManager.NavigateTo($"/admin/users/edit/{userId}"); return Task.CompletedTask; }
    private Task ViewUser(string userId) { NavigationManager.NavigateTo($"/admin/users/view/{userId}"); return Task.CompletedTask; }

    private async Task DeleteUser(string userId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", T["AdminUsers.ConfirmDelete"])) return;
        var user = await UserManager.FindByIdAsync(userId);
        if (user == null) { ToastRef?.Show(T["AdminUsers.NotFoundToast"], "error"); return; }
        var result = await UserManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await LogService.AuditAsync(AuditEvents.UserDeleted, AuditEntities.User, userId,
                string.Format(T["AdminUsers.LogDeleted"], userId), source: AdminAuditSources.Identity, level: "Warning");
            ToastRef?.Show(T["AdminUsers.DeletedToast"], "info");
            await LoadUsersPageAsync();
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            ToastRef?.Show($"{T["AdminUsers.DeleteErrorToast"]}: {errors}", "error");
        }
    }

    private async Task ClearFilters()
    {
        filterUserName = filterEmail = filterRole = filterStatus = "";
        showRestoredFiltersNotice = false;
        await PersistFilterStateAsync();
    }

    private async Task LoadFilterStateFromStorageAsync()
    {
        var state = await AdminUsersFilterStateService.LoadAsync();
        filterUserName = state.UserName;
        filterEmail = state.Email;
        filterRole = state.Role;
        filterStatus = state.Status;
        showRestoredFiltersNotice = !string.IsNullOrWhiteSpace(filterUserName) || !string.IsNullOrWhiteSpace(filterEmail) || !string.IsNullOrWhiteSpace(filterRole) || !string.IsNullOrWhiteSpace(filterStatus);
    }

    private async Task PersistFilterStateAsync() =>
        await AdminUsersFilterStateService.SaveAsync(new AdminUsersFilterState { UserName = filterUserName, Email = filterEmail, Role = filterRole, Status = filterStatus });

    private void DismissRestoredNotice() => showRestoredFiltersNotice = false;

    private async Task HandleApplyFilters((string Email, string Role, string Status) filters)
    {
        filterEmail = filters.Email; filterRole = filters.Role; filterStatus = filters.Status;
        await ApplyFiltersAndPersist();
    }

    private async Task HandleClearFilters((string Email, string Role, string Status) filters)
    {
        filterEmail = filterRole = filterStatus = "";
        showRestoredFiltersNotice = false;
        currentPage = 1;
        await LoadUsersPageAsync();
        await PersistFilterStateAsync();
    }
}
