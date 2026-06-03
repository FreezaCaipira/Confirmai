using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Tests.TestDoubles;
using System.Globalization;

namespace Confirmai.Tests;

public class LocalStorageStateHelpersTests
{
    [Fact]
    public async Task GetStringAsync_WhenMissing_ReturnsEmptyString()
    {
        var js = new FakeLocalStorageJsRuntime();

        var value = await LocalStorageStateHelpers.GetStringAsync(js, "missing-key");

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public async Task GetPositiveIntAsync_And_GetNonNegativeIntAsync_ParseWithExpectedGuards()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set("page", "3");
        js.Set("id", "0");
        js.Set("bad", "-1");

        var page = await LocalStorageStateHelpers.GetPositiveIntAsync(js, "page");
        var id = await LocalStorageStateHelpers.GetNonNegativeIntAsync(js, "id");
        var badPositive = await LocalStorageStateHelpers.GetPositiveIntAsync(js, "bad");

        Assert.Equal(3, page);
        Assert.Equal(0, id);
        Assert.Null(badPositive);
    }

    [Fact]
    public async Task GetDecimalAsync_WhenInvalid_ReturnsNull()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set("amount", "0.50");
        js.Set("invalid", "abc");

        var amount = await LocalStorageStateHelpers.GetDecimalAsync(js, "amount");
        var invalid = await LocalStorageStateHelpers.GetDecimalAsync(js, "invalid");

        Assert.Equal(0.50m, amount);
        Assert.Null(invalid);
    }

    [Fact]
    public async Task GetDateAsync_WhenRoundtripValueProvided_ReturnsDateOnly()
    {
        var js = new FakeLocalStorageJsRuntime();
        var dateTime = new DateTime(2026, 3, 15, 23, 12, 1, DateTimeKind.Utc);
        js.Set("date", dateTime.ToString("o", CultureInfo.InvariantCulture));

        var date = await LocalStorageStateHelpers.GetDateAsync(js, "date");

        Assert.Equal(new DateTime(2026, 3, 15), date);
    }

    [Fact]
    public async Task GetEnumAsync_And_GetNullableBoolAsync_ParseAndFallbackCorrectly()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set("quick", AdminLogsQuickRangePreset.Last7Days.ToString());
        js.Set("quick-bad", "unknown");
        js.Set("flag", "true");
        js.Set("flag-bad", "not-bool");

        var quick = await LocalStorageStateHelpers.GetEnumAsync<AdminLogsQuickRangePreset>(js, "quick");
        var quickBad = await LocalStorageStateHelpers.GetEnumAsync<AdminLogsQuickRangePreset>(js, "quick-bad");
        var flag = await LocalStorageStateHelpers.GetNullableBoolAsync(js, "flag");
        var flagBad = await LocalStorageStateHelpers.GetNullableBoolAsync(js, "flag-bad");

        Assert.Equal(AdminLogsQuickRangePreset.Last7Days, quick);
        Assert.Null(quickBad);
        Assert.True(flag);
        Assert.Null(flagBad);
    }

    [Fact]
    public async Task SetOrRemoveHelpers_WriteExpectedStorageValues()
    {
        var js = new FakeLocalStorageJsRuntime();

        await LocalStorageStateHelpers.SetStringAsync(js, "name", "john");
        await LocalStorageStateHelpers.SetOrRemoveIntAsync(js, "id", 10);
        await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(js, "amount", 1.25m);
        await LocalStorageStateHelpers.SetOrRemoveDateAsync(js, "date", new DateTime(2026, 3, 15));
        await LocalStorageStateHelpers.SetPageAsync(js, "page", null);

        Assert.Equal("john", js.Get("name"));
        Assert.Equal("10", js.Get("id"));
        Assert.Equal("1.25", js.Get("amount"));
        Assert.NotNull(js.Get("date"));
        Assert.Equal("1", js.Get("page"));
    }

    [Fact]
    public async Task SetOrRemoveHelpers_WhenNull_RemoveKeys()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set("id", "1");
        js.Set("amount", "0.10");
        js.Set("date", new DateTime(2026, 3, 15).ToString("o", CultureInfo.InvariantCulture));

        await LocalStorageStateHelpers.SetOrRemoveIntAsync(js, "id", null);
        await LocalStorageStateHelpers.SetOrRemoveDecimalAsync(js, "amount", null);
        await LocalStorageStateHelpers.SetOrRemoveDateAsync(js, "date", null);

        Assert.Null(js.Get("id"));
        Assert.Null(js.Get("amount"));
        Assert.Null(js.Get("date"));
    }

    [Fact]
    public async Task GetHelpers_WhenJsThrows_PropagateExceptionToCaller()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => LocalStorageStateHelpers.GetStringAsync(js, "x"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => LocalStorageStateHelpers.GetPositiveIntAsync(js, "x"));
    }
}


