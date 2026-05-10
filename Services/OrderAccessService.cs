using Confirmai.Models;

namespace Confirmai.Services;

public class OrderAccessService
{
    public bool CanAccess(OrderModel? order, string? userId, bool isAdmin)
    {
        if (order is null)
            return false;

        if (isAdmin)
            return true;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        return order.BuyerId == userId || order.SellerId == userId;
    }
}


