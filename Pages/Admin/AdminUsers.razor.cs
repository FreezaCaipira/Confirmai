using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Shared;
using Confirmai.Shared.Components;
using Confirmai.Shared.Components.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class AdminUsers
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private LogService LogService { get; set; } = default!;
    [Inject] private AdminUsersFilterStateService AdminUsersFilterStateService { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;

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

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersPageAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || filtersLoaded)
        {
            return;
        }

        await LoadFilterStateFromStorageAsync();
        filtersLoaded = true;
        currentPage = 1;
        await LoadUsersPageAsync();
    }

    private async Task LoadUsersPageAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();

        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filterUserName))
        {
            query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(filterUserName.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filterEmail))
        {
            query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(filterEmail.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filterRole))
        {
            var userIdsWithRole = await db.UserRoles
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name != null && x.Name.ToLower().Contains(filterRole.ToLower()))
                .Select(x => x.UserId)
                .ToListAsync();
            query = query.Where(u => userIdsWithRole.Contains(u.Id));
        }

        if (filterStatus == "active")
        {
            query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now);
        }
        else if (filterStatus == "blocked")
        {
            query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now);
        }

        totalUsers = await query.CountAsync();
        totalPages = Math.Max(1, (int)Math.Ceiling((double)totalUsers / PageSize));

        if (currentPage > totalPages)
            currentPage = totalPages;

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var roleMap = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => users.Select(u => u.Id).Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(g => g.Key, g => string.Join(", ", g.Select(x => x.Name)));

        userRoles.Clear();
        foreach (var user in users)
        {
            userRoles[user.Id] = roleMap.TryGetValue(user.Id, out var roles) ? roles : "-";
        }

        currentPageUsers = users;
    }

    private async Task ApplyFiltersAndPersist()
    {
        showRestoredFiltersNotice = false;
        currentPage = 1;
        await LoadUsersPageAsync();
        await PersistFilterStateAsync();
    }

    private async Task GoToPrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            await LoadUsersPageAsync();
        }
    }

    private async Task GoToNextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            await LoadUsersPageAsync();
        }
    }

    private async Task LockUser(string userId)
    {
        var confirm = await JS.InvokeAsync<bool>("confirm", T["AdminUsers.ConfirmLock"]);
        if (!confirm)
            return;

        var user = await UserManager.FindByIdAsync(userId);
        if (user != null)
        {
            await UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await LogService.LogAsync(string.Format(T["AdminUsers.LogLocked"], userId),
                                        source: "Admin",
                                        level: "Warning",
                                        userId: userId);
            ToastRef?.Show(T["AdminUsers.LockedToast"], "warning");
            await LoadUsersPageAsync();
        }
        else
        {
            ToastRef?.Show(T["AdminUsers.NotFoundToast"], "error");
        }
    }

    private async Task UnlockUser(string userId)
    {
        var user = await UserManager.FindByIdAsync(userId);
        if (user != null)
        {
            await UserManager.SetLockoutEndDateAsync(user, null);
            await LogService.LogAsync(string.Format(T["AdminUsers.LogUnlocked"], userId),
                                        source: "Admin",
                                        level: "Info",
                                        userId: userId);
            ToastRef?.Show(T["AdminUsers.UnlockedToast"], "success");
            await LoadUsersPageAsync();
        }
        else
        {
            ToastRef?.Show(T["AdminUsers.NotFoundToast"], "error");
        }
    }

    private Task EditUser(string userId)
    {
        NavigationManager.NavigateTo($"/admin/users/edit/{userId}");
        return Task.CompletedTask;
    }

    private Task ViewUser(string userId)
    {
        NavigationManager.NavigateTo($"/admin/users/view/{userId}");
        return Task.CompletedTask;
    }

    private async Task DeleteUser(string userId)
    {
        var confirm = await JS.InvokeAsync<bool>("confirm", T["AdminUsers.ConfirmDelete"]);
        if (!confirm)
            return;

        var user = await UserManager.FindByIdAsync(userId);
        if (user != null)
        {
            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await LogService.AuditAsync(
                    AuditEvents.UserDeleted,
                    AuditEntities.User,
                    userId,
                    string.Format(T["AdminUsers.LogDeleted"], userId),
                    source: AdminAuditSources.Identity,
                    level: "Warning");
                ToastRef?.Show(T["AdminUsers.DeletedToast"], "info");
                await LoadUsersPageAsync();
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                ToastRef?.Show($"{T["AdminUsers.DeleteErrorToast"]}: {errors}", "error");
            }
        }
        else
        {
            ToastRef?.Show(T["AdminUsers.NotFoundToast"], "error");
        }
    }

    private async Task ClearFilters()
    {
        filterUserName = "";
        filterEmail = "";
        filterRole = "";
        filterStatus = "";
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

        showRestoredFiltersNotice =
            !string.IsNullOrWhiteSpace(filterUserName)
            || !string.IsNullOrWhiteSpace(filterEmail)
            || !string.IsNullOrWhiteSpace(filterRole)
            || !string.IsNullOrWhiteSpace(filterStatus);
    }

    private async Task PersistFilterStateAsync()
    {
        await AdminUsersFilterStateService.SaveAsync(new AdminUsersFilterState
        {
            UserName = filterUserName,
            Email = filterEmail,
            Role = filterRole,
            Status = filterStatus
        });
    }

    private void DismissRestoredNotice()
    {
        showRestoredFiltersNotice = false;
    }

    private async Task HandleApplyFilters((string Email, string Role, string Status) filters)
    {
        filterEmail = filters.Email;
        filterRole = filters.Role;
        filterStatus = filters.Status;
        await ApplyFiltersAndPersist();
    }

    private async Task HandleClearFilters((string Email, string Role, string Status) filters)
    {
        filterEmail = "";
        filterRole = "";
        filterStatus = "";
        showRestoredFiltersNotice = false;
        currentPage = 1;
        await LoadUsersPageAsync();
        await PersistFilterStateAsync();
    }
}
