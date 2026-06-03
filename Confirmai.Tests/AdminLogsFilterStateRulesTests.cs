using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;

namespace Confirmai.Tests;

public class AdminLogsFilterStateRulesTests
{
    [Fact]
    public void ShouldShowRestoredNotice_WhenStateIsEmpty_ReturnsFalse()
    {
        var state = new AdminLogsFilterState();

        var result = AdminLogsFilterStateRules.ShouldShowRestoredNotice(state);

        Assert.False(result);
    }

    [Fact]
    public void ShouldShowRestoredNotice_WhenAnyFilterIsPresent_ReturnsTrue()
    {
        var state = new AdminLogsFilterState
        {
            GlobalSearch = "abc"
        };

        var result = AdminLogsFilterStateRules.ShouldShowRestoredNotice(state);

        Assert.True(result);
    }

    [Fact]
    public void ShouldShowRestoredNotice_WhenDateOrPageIsPresent_ReturnsTrue()
    {
        var stateWithDate = new AdminLogsFilterState
        {
            StartDate = new DateTime(2026, 3, 1)
        };

        var stateWithPage = new AdminLogsFilterState
        {
            Page = 2
        };

        Assert.True(AdminLogsFilterStateRules.ShouldShowRestoredNotice(stateWithDate));
        Assert.True(AdminLogsFilterStateRules.ShouldShowRestoredNotice(stateWithPage));
    }
}


