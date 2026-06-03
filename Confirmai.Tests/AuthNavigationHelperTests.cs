using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class AuthNavigationHelperTests
{
    [Fact]
    public void BuildLoginUrl_EncodesReturnUrl()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("/payments?status=paid");

        Assert.StartsWith("/Identity/Account/Login?returnUrl=", url);
        Assert.Contains(Uri.EscapeDataString("/payments?status=paid"), url);
    }

    [Fact]
    public void BuildLoginUrl_PrependsSlash_WhenMissing()
    {
        var url = AuthNavigationHelper.BuildLoginUrl("payments");

        Assert.Contains(Uri.EscapeDataString("/payments"), url);
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
