using Confirmai.Services.Utility;
using Microsoft.JSInterop;

namespace Confirmai.Services.Admin;

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

public class AdminPaymentsFilterStateService : AdminFilterStateServiceBase<AdminPaymentsFilterState>
{
    public AdminPaymentsFilterStateService(IJSRuntime js) : base(js) { }

    protected override async Task<AdminPaymentsFilterState> LoadCoreAsync()
    {
        var userId = await LocalStorageStateHelpers.GetStringAsync(Js, AdminPaymentsStorageKeys.UserFilter);
        var productId = await LocalStorageStateHelpers.GetStringAsync(Js, AdminPaymentsStorageKeys.ProductFilter);
        var status = await LocalStorageStateHelpers.GetStringAsync(Js, AdminPaymentsStorageKeys.StatusFilter);
        var minAmount = await LocalStorageStateHelpers.GetDecimalAsync(Js, AdminPaymentsStorageKeys.MinAmountFilter);
        var maxAmount = await LocalStorageStateHelpers.GetDecimalAsync(Js, AdminPaymentsStorageKeys.MaxAmountFilter);
        var date = await LocalStorageStateHelpers.GetDateAsync(Js, AdminPaymentsStorageKeys.DateFilter);
        var page = await LocalStorageStateHelpers.GetPositiveIntAsync(Js, AdminPaymentsStorageKeys.Page);

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

    protected override async Task SaveCoreAsync(AdminPaymentsFilterState state)
    {
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminPaymentsStorageKeys.UserFilter, state.UserId);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminPaymentsStorageKeys.ProductFilter, state.ProductId);
        await LocalStorageStateHelpers.SetStringAsync(Js, AdminPaymentsStorageKeys.StatusFilter, state.Status);
        await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(Js, AdminPaymentsStorageKeys.MinAmountFilter, state.MinAmount);
        await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(Js, AdminPaymentsStorageKeys.MaxAmountFilter, state.MaxAmount);
        await LocalStorageStateHelpers.SetOrRemoveDateAsync(Js, AdminPaymentsStorageKeys.DateFilter, state.Date);
        await LocalStorageStateHelpers.SetPageAsync(Js, AdminPaymentsStorageKeys.Page, state.Page);
    }
}
