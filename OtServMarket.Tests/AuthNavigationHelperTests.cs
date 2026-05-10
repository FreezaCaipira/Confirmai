using Confirmai.Services;

namespace Confirmai.Tests;

public class AuthNavigationHelperTests
{
    [Fact]
    public void BuildLoginUrl_EncodesReturnUrl()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("/orders?status=paid");

        Assert.StartsWith("/Identity/Account/Login?returnUrl=", url);
        Assert.Contains(Uri.EscapeDataString("/orders?status=paid"), url);
    }

    [Fact]
    public void BuildLoginUrl_PrependsSlash_WhenMissing()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("orders");

        Assert.Contains(Uri.EscapeDataString("/orders"), url);
    }

    [Fact]
    public void BuildLoginUrl_DefaultsToRoot_WhenEmpty()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("");

        Assert.Contains(Uri.EscapeDataString("/"), url);
    }

    [Fact]
    public void BuildLoginUrl_DefaultsToRoot_WhenWhitespace()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("   ");

        Assert.Contains(Uri.EscapeDataString("/"), url);
    }

    [Fact]
    public void BuildLoginUrl_PreservesAbsoluteLocalPath()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("/admin/users");

        Assert.Contains(Uri.EscapeDataString("/admin/users"), url);
    }
}
