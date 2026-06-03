using Confirmai.Services.Admin;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Tests.TestDoubles;
using System.Globalization;

namespace Confirmai.Tests;

public class AdminPaymentsFilterStateServiceTests
{
    [Fact]
    public async Task LoadAsync_WithStoredValues_ParsesState()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminPaymentsStorageKeys.UserFilter, "user-1");
        js.Set(AdminPaymentsStorageKeys.ProductFilter, "product-a");
        js.Set(AdminPaymentsStorageKeys.MinAmountFilter, "0.01");
        js.Set(AdminPaymentsStorageKeys.MaxAmountFilter, "0.05");
        js.Set(AdminPaymentsStorageKeys.StatusFilter, "Pago");
        js.Set(AdminPaymentsStorageKeys.DateFilter, new DateTime(2026, 3, 15).ToString("o", CultureInfo.InvariantCulture));
        js.Set(AdminPaymentsStorageKeys.Page, "5");

        var service = new AdminPaymentsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal("user-1", state.UserId);
        Assert.Equal("product-a", state.ProductId);
        Assert.Equal(0.01m, state.MinAmount);
        Assert.Equal(0.05m, state.MaxAmount);
        Assert.Equal("Pago", state.Status);
        Assert.Equal(new DateTime(2026, 3, 15), state.Date);
        Assert.Equal(5, state.Page);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidTypedValues_UsesNullFallbacks()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminPaymentsStorageKeys.MinAmountFilter, "invalid");
        js.Set(AdminPaymentsStorageKeys.MaxAmountFilter, "invalid");
        js.Set(AdminPaymentsStorageKeys.DateFilter, "invalid");
        js.Set(AdminPaymentsStorageKeys.Page, "0");

        var service = new AdminPaymentsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Null(state.MinAmount);
        Assert.Null(state.MaxAmount);
        Assert.Null(state.Date);
        Assert.Null(state.Page);
    }

    [Fact]
    public async Task SaveAsync_PersistsValuesAndPageFallback()
    {
        var js = new FakeLocalStorageJsRuntime();

        // Preload optional keys to verify SaveAsync removes stale filters.
        js.Set(AdminPaymentsStorageKeys.MinAmountFilter, "0.01");
        js.Set(AdminPaymentsStorageKeys.DateFilter, new DateTime(2026, 3, 10).ToString("o", CultureInfo.InvariantCulture));

        var service = new AdminPaymentsFilterStateService(js);

        await service.SaveAsync(new AdminPaymentsFilterState
        {
            UserId = "user-2",
            ProductId = "product-b",
            MinAmount = null,
            MaxAmount = 0.9m,
            Status = "Pendente",
            Date = null,
            Page = null
        });

        Assert.Equal("user-2", js.Get(AdminPaymentsStorageKeys.UserFilter));
        Assert.Equal("product-b", js.Get(AdminPaymentsStorageKeys.ProductFilter));
        Assert.Null(js.Get(AdminPaymentsStorageKeys.MinAmountFilter));
        Assert.Equal("0.9", js.Get(AdminPaymentsStorageKeys.MaxAmountFilter));
        Assert.Equal("Pendente", js.Get(AdminPaymentsStorageKeys.StatusFilter));
        Assert.Null(js.Get(AdminPaymentsStorageKeys.DateFilter));
        Assert.Equal("1", js.Get(AdminPaymentsStorageKeys.Page));
    }

    [Fact]
    public async Task LoadAsync_WhenJsInteropThrows_ReturnsDefaultState()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };

        var service = new AdminPaymentsFilterStateService(js);
        var state = await service.LoadAsync();

        Assert.Equal(string.Empty, state.UserId);
        Assert.Equal(string.Empty, state.ProductId);
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

        var service = new AdminPaymentsFilterStateService(js);

        await service.SaveAsync(new AdminPaymentsFilterState
        {
            UserId = "u",
            ProductId = "p",
            MinAmount = 1,
            MaxAmount = 2,
            Status = "Pago",
            Date = new DateTime(2026, 3, 15),
            Page = 2
        });
    }
}


