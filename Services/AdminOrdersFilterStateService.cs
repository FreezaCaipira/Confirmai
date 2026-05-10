using Microsoft.JSInterop;

namespace Confirmai.Services;

public sealed class AdminOrdersFilterState
{
    public int? Id { get; init; }
    public string Product { get; init; } = string.Empty;
    public string Buyer { get; init; } = string.Empty;
    public string Seller { get; init; } = string.Empty;
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? Date { get; init; }
    public int? Page { get; init; }
}

public class AdminOrdersFilterStateService
{
    private readonly IJSRuntime _js;

    public AdminOrdersFilterStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<AdminOrdersFilterState> LoadAsync()
    {
        try
        {
            var id = await LocalStorageStateHelpers.GetNonNegativeIntAsync(_js, AdminOrdersStorageKeys.IdFilter);
            var product = await LocalStorageStateHelpers.GetStringAsync(_js, AdminOrdersStorageKeys.ProductFilter);
            var buyer = await LocalStorageStateHelpers.GetStringAsync(_js, AdminOrdersStorageKeys.BuyerFilter);
            var seller = await LocalStorageStateHelpers.GetStringAsync(_js, AdminOrdersStorageKeys.SellerFilter);
            var status = await LocalStorageStateHelpers.GetStringAsync(_js, AdminOrdersStorageKeys.StatusFilter);
            var minAmount = await LocalStorageStateHelpers.GetDecimalAsync(_js, AdminOrdersStorageKeys.MinAmountFilter);
            var maxAmount = await LocalStorageStateHelpers.GetDecimalAsync(_js, AdminOrdersStorageKeys.MaxAmountFilter);
            var date = await LocalStorageStateHelpers.GetDateAsync(_js, AdminOrdersStorageKeys.DateFilter);
            var page = await LocalStorageStateHelpers.GetPositiveIntAsync(_js, AdminOrdersStorageKeys.Page);

            return new AdminOrdersFilterState
            {
                Id = id,
                Product = product,
                Buyer = buyer,
                Seller = seller,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                Status = status,
                Date = date,
                Page = page
            };
        }
        catch
        {
            return new AdminOrdersFilterState();
        }
    }

    public async Task SaveAsync(AdminOrdersFilterState state)
    {
        try
        {
            await LocalStorageStateHelpers.SetOrRemoveIntAsync(_js, AdminOrdersStorageKeys.IdFilter, state.Id);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminOrdersStorageKeys.ProductFilter, state.Product);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminOrdersStorageKeys.BuyerFilter, state.Buyer);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminOrdersStorageKeys.SellerFilter, state.Seller);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminOrdersStorageKeys.StatusFilter, state.Status);
            await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(_js, AdminOrdersStorageKeys.MinAmountFilter, state.MinAmount);
            await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(_js, AdminOrdersStorageKeys.MaxAmountFilter, state.MaxAmount);
            await LocalStorageStateHelpers.SetOrRemoveDateAsync(_js, AdminOrdersStorageKeys.DateFilter, state.Date);
            await LocalStorageStateHelpers.SetPageAsync(_js, AdminOrdersStorageKeys.Page, state.Page);
        }
        catch
        {
            // No-op: ignore storage failures.
        }
    }
}

