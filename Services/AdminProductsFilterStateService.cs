using Microsoft.JSInterop;

namespace Confirmai.Services;

public sealed class AdminProductsFilterState
{
    public int? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? Page { get; init; }
}

public class AdminProductsFilterStateService
{
    private readonly IJSRuntime _js;

    public AdminProductsFilterStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<AdminProductsFilterState> LoadAsync()
    {
        try
        {
            var id = await LocalStorageStateHelpers.GetNonNegativeIntAsync(_js, AdminProductsStorageKeys.IdFilter);
            var name = await LocalStorageStateHelpers.GetStringAsync(_js, AdminProductsStorageKeys.NameFilter);
            var userId = await LocalStorageStateHelpers.GetStringAsync(_js, AdminProductsStorageKeys.UserFilter);
            var minPrice = await LocalStorageStateHelpers.GetDecimalAsync(_js, AdminProductsStorageKeys.MinPriceFilter);
            var maxPrice = await LocalStorageStateHelpers.GetDecimalAsync(_js, AdminProductsStorageKeys.MaxPriceFilter);
            var page = await LocalStorageStateHelpers.GetPositiveIntAsync(_js, AdminProductsStorageKeys.Page);

            return new AdminProductsFilterState
            {
                Id = id,
                Name = name,
                UserId = userId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Page = page
            };
        }
        catch
        {
            return new AdminProductsFilterState();
        }
    }

    public async Task SaveAsync(AdminProductsFilterState state)
    {
        try
        {
            await LocalStorageStateHelpers.SetOrRemoveIntAsync(_js, AdminProductsStorageKeys.IdFilter, state.Id);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminProductsStorageKeys.NameFilter, state.Name);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminProductsStorageKeys.UserFilter, state.UserId);
            await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(_js, AdminProductsStorageKeys.MinPriceFilter, state.MinPrice);
            await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(_js, AdminProductsStorageKeys.MaxPriceFilter, state.MaxPrice);
            await LocalStorageStateHelpers.SetPageAsync(_js, AdminProductsStorageKeys.Page, state.Page);
        }
        catch
        {
            // No-op: ignore storage failures.
        }
    }
}

