using Microsoft.JSInterop;

namespace Confirmai.Services;

public sealed class AdminPaymentsFilterState
{
    public string UserId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? Date { get; init; }
    public int? Page { get; init; }
}

public class AdminPaymentsFilterStateService
{
    private readonly IJSRuntime _js;

    public AdminPaymentsFilterStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<AdminPaymentsFilterState> LoadAsync()
    {
        try
        {
            var userId = await LocalStorageStateHelpers.GetStringAsync(_js, AdminPaymentsStorageKeys.UserFilter);
            var productId = await LocalStorageStateHelpers.GetStringAsync(_js, AdminPaymentsStorageKeys.ProductFilter);
            var status = await LocalStorageStateHelpers.GetStringAsync(_js, AdminPaymentsStorageKeys.StatusFilter);
            var minAmount = await LocalStorageStateHelpers.GetDecimalAsync(_js, AdminPaymentsStorageKeys.MinAmountFilter);
            var maxAmount = await LocalStorageStateHelpers.GetDecimalAsync(_js, AdminPaymentsStorageKeys.MaxAmountFilter);
            var date = await LocalStorageStateHelpers.GetDateAsync(_js, AdminPaymentsStorageKeys.DateFilter);
            var page = await LocalStorageStateHelpers.GetPositiveIntAsync(_js, AdminPaymentsStorageKeys.Page);

            return new AdminPaymentsFilterState
            {
                UserId = userId,
                ProductId = productId,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                Status = status,
                Date = date,
                Page = page
            };
        }
        catch
        {
            return new AdminPaymentsFilterState();
        }
    }

    public async Task SaveAsync(AdminPaymentsFilterState state)
    {
        try
        {
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminPaymentsStorageKeys.UserFilter, state.UserId);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminPaymentsStorageKeys.ProductFilter, state.ProductId);
            await LocalStorageStateHelpers.SetStringAsync(_js, AdminPaymentsStorageKeys.StatusFilter, state.Status);
            await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(_js, AdminPaymentsStorageKeys.MinAmountFilter, state.MinAmount);
            await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(_js, AdminPaymentsStorageKeys.MaxAmountFilter, state.MaxAmount);
            await LocalStorageStateHelpers.SetOrRemoveDateAsync(_js, AdminPaymentsStorageKeys.DateFilter, state.Date);
            await LocalStorageStateHelpers.SetPageAsync(_js, AdminPaymentsStorageKeys.Page, state.Page);
        }
        catch
        {
            // No-op: ignore storage failures.
        }
    }
}

