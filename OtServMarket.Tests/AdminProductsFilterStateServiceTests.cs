using Confirmai.Services;
using Confirmai.Tests.TestDoubles;
using Xunit;

namespace Confirmai.Tests;

public class AdminProductsFilterStateServiceTests
{
    [Fact]
    public async Task LoadAsync_WithStoredValues_ParsesState()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminProductsStorageKeys.IdFilter, "42");
        js.Set(AdminProductsStorageKeys.NameFilter, "Widget");
        js.Set(AdminProductsStorageKeys.UserFilter, "user-1");
        js.Set(AdminProductsStorageKeys.MinPriceFilter, "10.50");
        js.Set(AdminProductsStorageKeys.MaxPriceFilter, "99.99");
        js.Set(AdminProductsStorageKeys.Page, "3");

        var service = new AdminProductsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal(42, state.Id);
        Assert.Equal("Widget", state.Name);
        Assert.Equal("user-1", state.UserId);
        Assert.Equal(10.50m, state.MinPrice);
        Assert.Equal(99.99m, state.MaxPrice);
        Assert.Equal(3, state.Page);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidValues_FallsBackSafely()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminProductsStorageKeys.IdFilter, "bad");
        js.Set(AdminProductsStorageKeys.NameFilter, "");
        js.Set(AdminProductsStorageKeys.UserFilter, "");
        js.Set(AdminProductsStorageKeys.MinPriceFilter, "NaN");
        js.Set(AdminProductsStorageKeys.MaxPriceFilter, "none");
        js.Set(AdminProductsStorageKeys.Page, "-2");

        var service = new AdminProductsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Null(state.Id);
        Assert.Equal(string.Empty, state.Name);
        Assert.Equal(string.Empty, state.UserId);
        Assert.Null(state.MinPrice);
        Assert.Null(state.MaxPrice);
        Assert.Null(state.Page);
    }

    [Fact]
    public async Task SaveAsync_WithValues_WritesAndCleansExpectedKeys()
    {
        var js = new FakeLocalStorageJsRuntime();
        var service = new AdminProductsFilterStateService(js);

        await service.SaveAsync(new AdminProductsFilterState
        {
            Id = 7,
            Name = "Book",
            UserId = "u7",
            MinPrice = 1.25m,
            MaxPrice = null,
            Page = 2
        });

        Assert.Equal("7", js.Get(AdminProductsStorageKeys.IdFilter));
        Assert.Equal("Book", js.Get(AdminProductsStorageKeys.NameFilter));
        Assert.Equal("u7", js.Get(AdminProductsStorageKeys.UserFilter));
        Assert.Equal("1.25", js.Get(AdminProductsStorageKeys.MinPriceFilter));
        Assert.Null(js.Get(AdminProductsStorageKeys.MaxPriceFilter));
        Assert.Equal("2", js.Get(AdminProductsStorageKeys.Page));
    }

    [Fact]
    public async Task SaveAsync_WithJsInteropFailure_DoesNotThrow()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };
        var service = new AdminProductsFilterStateService(js);

        await service.SaveAsync(new AdminProductsFilterState
        {
            Id = 1,
            Name = "n",
            UserId = "u",
            MinPrice = 1,
            MaxPrice = 2,
            Page = 1
        });
    }

    [Fact]
    public async Task SaveAsync_WhenPageIsNull_PersistsPageOne()
    {
        var js = new FakeLocalStorageJsRuntime();

        // Preload keys to verify SaveAsync cleans nullable filters.
        js.Set(AdminProductsStorageKeys.IdFilter, "12");
        js.Set(AdminProductsStorageKeys.MinPriceFilter, "0.10");
        js.Set(AdminProductsStorageKeys.MaxPriceFilter, "0.20");

        var service = new AdminProductsFilterStateService(js);

        await service.SaveAsync(new AdminProductsFilterState
        {
            Id = null,
            Name = string.Empty,
            UserId = string.Empty,
            MinPrice = null,
            MaxPrice = null,
            Page = null
        });

        Assert.Equal("1", js.Get(AdminProductsStorageKeys.Page));
        Assert.Null(js.Get(AdminProductsStorageKeys.IdFilter));
        Assert.Null(js.Get(AdminProductsStorageKeys.MinPriceFilter));
        Assert.Null(js.Get(AdminProductsStorageKeys.MaxPriceFilter));
    }

    [Fact]
    public async Task LoadAsync_WhenJsInteropThrows_ReturnsDefaultState()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };
        var service = new AdminProductsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Null(state.Id);
        Assert.Equal(string.Empty, state.Name);
        Assert.Equal(string.Empty, state.UserId);
        Assert.Null(state.MinPrice);
        Assert.Null(state.MaxPrice);
        Assert.Null(state.Page);
    }
}


