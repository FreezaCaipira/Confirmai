using Confirmai.Services;
using Confirmai.Tests.TestDoubles;

namespace Confirmai.Tests;

public class AdminUsersFilterStateServiceTests
{
    [Fact]
    public async Task LoadAsync_WithStoredValues_ParsesState()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminUsersStorageKeys.UserNameFilter, "john");
        js.Set(AdminUsersStorageKeys.EmailFilter, "john@test.local");
        js.Set(AdminUsersStorageKeys.RoleFilter, "admin");
        js.Set(AdminUsersStorageKeys.StatusFilter, "Ativo");
        js.Set(AdminUsersStorageKeys.Page, "4");

        var service = new AdminUsersFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal("john", state.UserName);
        Assert.Equal("john@test.local", state.Email);
        Assert.Equal("admin", state.Role);
        Assert.Equal("Ativo", state.Status);
        Assert.Equal(4, state.Page);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidPage_ReturnsNullPage()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminUsersStorageKeys.Page, "0");

        var service = new AdminUsersFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Null(state.Page);
    }

    [Fact]
    public async Task SaveAsync_WritesValuesAndPersistsPageFallback()
    {
        var js = new FakeLocalStorageJsRuntime();
        var service = new AdminUsersFilterStateService(js);

        await service.SaveAsync(new AdminUsersFilterState
        {
            UserName = "ana",
            Email = "ana@test.local",
            Role = "user",
            Status = "Bloqueado",
            Page = null
        });

        Assert.Equal("ana", js.Get(AdminUsersStorageKeys.UserNameFilter));
        Assert.Equal("ana@test.local", js.Get(AdminUsersStorageKeys.EmailFilter));
        Assert.Equal("user", js.Get(AdminUsersStorageKeys.RoleFilter));
        Assert.Equal("Bloqueado", js.Get(AdminUsersStorageKeys.StatusFilter));
        Assert.Equal("1", js.Get(AdminUsersStorageKeys.Page));
    }

    [Fact]
    public async Task LoadAsync_WhenJsInteropThrows_ReturnsDefaultState()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };
        var service = new AdminUsersFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal(string.Empty, state.UserName);
        Assert.Equal(string.Empty, state.Email);
        Assert.Equal(string.Empty, state.Role);
        Assert.Equal(string.Empty, state.Status);
        Assert.Null(state.Page);
    }

    [Fact]
    public async Task SaveAsync_WhenJsInteropThrows_DoesNotPropagateException()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };
        var service = new AdminUsersFilterStateService(js);

        await service.SaveAsync(new AdminUsersFilterState
        {
            UserName = "x",
            Email = "y",
            Role = "z",
            Status = "Ativo",
            Page = 3
        });
    }
}


