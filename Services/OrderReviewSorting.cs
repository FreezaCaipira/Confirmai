using Confirmai.Models;

namespace Confirmai.Services;

public enum OrderReviewSortColumn
{
    Buyer,
    Product,
    Amount
}

public static class OrderReviewSorting
{
    public static IOrderedEnumerable<OrderModel> Apply(
        IEnumerable<OrderModel> source,
        OrderReviewSortColumn sortColumn,
        bool sortAscending)
    {
        return (sortColumn, sortAscending) switch
        {
            (OrderReviewSortColumn.Buyer, true) => source
                .OrderBy(o => o.Buyer?.UserName ?? string.Empty)
                .ThenByDescending(o => o.Amount)
                .ThenBy(o => o.Id),

            (OrderReviewSortColumn.Buyer, false) => source
                .OrderByDescending(o => o.Buyer?.UserName ?? string.Empty)
                .ThenByDescending(o => o.Amount)
                .ThenBy(o => o.Id),

            (OrderReviewSortColumn.Product, true) => source
                .OrderBy(o => o.Product?.Name ?? string.Empty)
                .ThenBy(o => o.Buyer?.UserName ?? string.Empty)
                .ThenByDescending(o => o.Amount)
                .ThenBy(o => o.Id),

            (OrderReviewSortColumn.Product, false) => source
                .OrderByDescending(o => o.Product?.Name ?? string.Empty)
                .ThenBy(o => o.Buyer?.UserName ?? string.Empty)
                .ThenByDescending(o => o.Amount)
                .ThenBy(o => o.Id),

            (OrderReviewSortColumn.Amount, true) => source
                .OrderBy(o => o.Amount)
                .ThenBy(o => o.Buyer?.UserName ?? string.Empty)
                .ThenBy(o => o.Product?.Name ?? string.Empty)
                .ThenBy(o => o.Id),

            _ => source
                .OrderByDescending(o => o.Amount)
                .ThenBy(o => o.Buyer?.UserName ?? string.Empty)
                .ThenBy(o => o.Product?.Name ?? string.Empty)
                .ThenBy(o => o.Id)
        };
    }
}


