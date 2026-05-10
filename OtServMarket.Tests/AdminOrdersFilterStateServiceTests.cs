using Confirmai.Services;
using Confirmai.Tests.TestDoubles;
using System.Globalization;

namespace Confirmai.Tests;

public class AdminOrdersFilterStateServiceTests
{
    [Fact]
    public async Task LoadAsync_WithStoredValues_ParsesState()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminOrdersStorageKeys.IdFilter, "12");
        js.Set(AdminOrdersStorageKeys.ProductFilter, "produto");
        js.Set(AdminOrdersStorageKeys.BuyerFilter, "buyer");
        js.Set(AdminOrdersStorageKeys.SellerFilter, "seller");
        js.Set(AdminOrdersStorageKeys.MinAmountFilter, "0.02");
        js.Set(AdminOrdersStorageKeys.MaxAmountFilter, "0.10");
        js.Set(AdminOrdersStorageKeys.StatusFilter, "Pago");
        js.Set(AdminOrdersStorageKeys.DateFilter, new DateTime(2026, 3, 15).ToString("o", CultureInfo.InvariantCulture));
        js.Set(AdminOrdersStorageKeys.Page, "2");

        var service = new AdminOrdersFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal(12, state.Id);
        Assert.Equal("produto", state.Product);
        Assert.Equal("buyer", state.Buyer);
        Assert.Equal("seller", state.Seller);
        Assert.Equal(0.02m, state.MinAmount);
        Assert.Equal(0.10m, state.MaxAmount);
        Assert.Equal("Pago", state.Status);
        Assert.Equal(new DateTime(2026, 3, 15), state.Date);
        Assert.Equal(2, state.Page);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidTypedValues_UsesNullFallbacks()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminOrdersStorageKeys.IdFilter, "-1");
        js.Set(AdminOrdersStorageKeys.MinAmountFilter, "invalid");
        js.Set(AdminOrdersStorageKeys.MaxAmountFilter, "invalid");
        js.Set(AdminOrdersStorageKeys.DateFilter, "invalid");
        js.Set(AdminOrdersStorageKeys.Page, "0");

        var service = new AdminOrdersFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Null(state.Id);
        Assert.Null(state.MinAmount);
        Assert.Null(state.MaxAmount);
        Assert.Null(state.Date);
        Assert.Null(state.Page);
    }

    [Fact]
    public async Task SaveAsync_PersistsValuesAndRemovesNullables()
    {
        var js = new FakeLocalStorageJsRuntime();

        // Preload optional keys to verify SaveAsync clears stale state.
        js.Set(AdminOrdersStorageKeys.IdFilter, "20");
        js.Set(AdminOrdersStorageKeys.MinAmountFilter, "0.03");
        js.Set(AdminOrdersStorageKeys.DateFilter, new DateTime(2026, 3, 10).ToString("o", CultureInfo.InvariantCulture));

        var service = new AdminOrdersFilterStateService(js);

        await service.SaveAsync(new AdminOrdersFilterState
        {
            Id = null,
            Product = "x",
            Buyer = "b",
            Seller = "s",
            MinAmount = null,
            MaxAmount = 0.50m,
            Status = "Pendente",
            Date = null,
            Page = null
        });

        Assert.Null(js.Get(AdminOrdersStorageKeys.IdFilter));
        Assert.Equal("x", js.Get(AdminOrdersStorageKeys.ProductFilter));
        Assert.Equal("b", js.Get(AdminOrdersStorageKeys.BuyerFilter));
        Assert.Equal("s", js.Get(AdminOrdersStorageKeys.SellerFilter));
        Assert.Null(js.Get(AdminOrdersStorageKeys.MinAmountFilter));
        Assert.Equal("0.50", js.Get(AdminOrdersStorageKeys.MaxAmountFilter));
        Assert.Equal("Pendente", js.Get(AdminOrdersStorageKeys.StatusFilter));
        Assert.Null(js.Get(AdminOrdersStorageKeys.DateFilter));
        Assert.Equal("1", js.Get(AdminOrdersStorageKeys.Page));
    }

    [Fact]
    public async Task LoadAsync_WhenJsInteropThrows_ReturnsDefaultState()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };

        var service = new AdminOrdersFilterStateService(js);
        var state = await service.LoadAsync();

        Assert.Null(state.Id);
        Assert.Equal(string.Empty, state.Product);
        Assert.Equal(string.Empty, state.Buyer);
        Assert.Equal(string.Empty, state.Seller);
        Assert.Null(state.MinAmount);
        Assert.Null(state.MaxAmount);
        Assert.Equal(string.Empty, state.Status);
        Assert.Null(state.Date);
        Assert.Null(state.Page);
    }

    [Fact]
    public async Task SaveAsync_WhenJsInteropThrows_DoesNotPropagateException()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };

        var service = new AdminOrdersFilterStateService(js);

        await service.SaveAsync(new AdminOrdersFilterState
        {
            Id = 1,
            Product = "prod",
            Buyer = "buyer",
            Seller = "seller",
            MinAmount = 0.01m,
            MaxAmount = 0.02m,
            Status = "Pago",
            Date = new DateTime(2026, 3, 15),
            Page = 2
        });
    }
}


