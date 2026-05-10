using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class OrderAccessServiceTests
{
    private readonly OrderAccessService _service = new();

    [Fact]
    public void CanAccess_ReturnsFalse_WhenOrderIsNull()
    {
        var canAccess = _service.CanAccess(order: null, userId: "user-1", isAdmin: false);

        Assert.False(canAccess);
    }

    [Fact]
    public void CanAccess_ReturnsTrue_ForAdmin()
    {
        var order = new OrderModel { BuyerId = "buyer-1", SellerId = "seller-1" };

        var canAccess = _service.CanAccess(order, userId: "random-user", isAdmin: true);

        Assert.True(canAccess);
    }

    [Fact]
    public void CanAccess_ReturnsTrue_ForBuyer()
    {
        var order = new OrderModel { BuyerId = "buyer-1", SellerId = "seller-1" };

        var canAccess = _service.CanAccess(order, userId: "buyer-1", isAdmin: false);

        Assert.True(canAccess);
    }

    [Fact]
    public void CanAccess_ReturnsTrue_ForSeller()
    {
        var order = new OrderModel { BuyerId = "buyer-1", SellerId = "seller-1" };

        var canAccess = _service.CanAccess(order, userId: "seller-1", isAdmin: false);

        Assert.True(canAccess);
    }

    [Fact]
    public void CanAccess_ReturnsFalse_ForUnrelatedUser()
    {
        var order = new OrderModel { BuyerId = "buyer-1", SellerId = "seller-1" };

        var canAccess = _service.CanAccess(order, userId: "other-user", isAdmin: false);

        Assert.False(canAccess);
    }

    [Fact]
    public void CanAccess_ReturnsFalse_WhenUserIdIsMissing()
    {
        var order = new OrderModel { BuyerId = "buyer-1", SellerId = "seller-1" };

        var canAccess = _service.CanAccess(order, userId: null, isAdmin: false);

        Assert.False(canAccess);
    }
}


