using Confirmai.Services.Admin;

namespace Confirmai.Tests;

public class AdminPaymentsStorageKeysTests
{
    [Fact]
    public void UserFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.user", AdminPaymentsStorageKeys.UserFilter);
    }

    [Fact]
    public void ProductFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.product", AdminPaymentsStorageKeys.ProductFilter);
    }

    [Fact]
    public void MinAmountFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.minAmount", AdminPaymentsStorageKeys.MinAmountFilter);
    }

    [Fact]
    public void MaxAmountFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.maxAmount", AdminPaymentsStorageKeys.MaxAmountFilter);
    }

    [Fact]
    public void StatusFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.status", AdminPaymentsStorageKeys.StatusFilter);
    }

    [Fact]
    public void DateFilter_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.date", AdminPaymentsStorageKeys.DateFilter);
    }

    [Fact]
    public void Page_ConstantValue()
    {
        // Act & Assert
        Assert.Equal("admin.payments.page", AdminPaymentsStorageKeys.Page);
    }
}
