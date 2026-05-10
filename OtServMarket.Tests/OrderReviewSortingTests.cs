using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class OrderReviewSortingTests
{
    [Fact]
    public void Apply_WhenSortingByAmountDescending_UsesBuyerThenProductThenIdAsTieBreakers()
    {
        var orders = new[]
        {
            Order(1, "zoe", "Notebook", 2.00m),
            Order(2, "ana", "Camera", 2.00m),
            Order(3, "ana", "Alicate", 2.00m),
            Order(4, "mike", "Drone", 3.00m),
            Order(5, null, null, 2.00m)
        };

        var sorted = OrderReviewSorting.Apply(orders, OrderReviewSortColumn.Amount, sortAscending: false)
            .Select(o => o.Id)
            .ToArray();

        Assert.Equal(new[] { 4, 5, 3, 2, 1 }, sorted);
    }

    [Fact]
    public void Apply_WhenSortingByBuyerAscending_UsesAmountDescendingThenIdAsTieBreakers()
    {
        var orders = new[]
        {
            Order(1, "ana", "Notebook", 1.00m),
            Order(2, "ana", "Notebook", 2.00m),
            Order(3, "bruno", "Camera", 3.00m),
            Order(4, "ana", "Notebook", 2.00m),
            Order(5, null, "Alicate", 4.00m)
        };

        var sorted = OrderReviewSorting.Apply(orders, OrderReviewSortColumn.Buyer, sortAscending: true)
            .Select(o => o.Id)
            .ToArray();

        Assert.Equal(new[] { 5, 2, 4, 1, 3 }, sorted);
    }

    [Fact]
    public void Apply_WhenSortingByProductDescending_UsesBuyerThenAmountDescendingThenIdAsTieBreakers()
    {
        var orders = new[]
        {
            Order(1, "carlos", "Tablet", 1.00m),
            Order(2, "ana", "Tablet", 2.00m),
            Order(3, "bruno", "Tablet", 2.00m),
            Order(4, "ana", "Alicate", 3.00m),
            Order(5, "ana", "Tablet", 2.00m)
        };

        var sorted = OrderReviewSorting.Apply(orders, OrderReviewSortColumn.Product, sortAscending: false)
            .Select(o => o.Id)
            .ToArray();

        Assert.Equal(new[] { 2, 5, 3, 1, 4 }, sorted);
    }

    private static OrderModel Order(int id, string? buyerName, string? productName, decimal amount)
    {
        return new OrderModel
        {
            Id = id,
            Buyer = buyerName is null ? null : new ApplicationUser { UserName = buyerName },
            Product = productName is null ? null : new Product { Name = productName },
            Amount = amount
        };
    }
}


