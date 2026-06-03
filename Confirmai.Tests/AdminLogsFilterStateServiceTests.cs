using Confirmai.Services.Admin;
using Confirmai.Services;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Confirmai.Tests.TestDoubles;
using System.Globalization;

namespace Confirmai.Tests;

public class AdminLogsFilterStateServiceTests
{
    [Fact]
    public async Task LoadAsync_WithStoredValues_ParsesState()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminLogsStorageKeys.GlobalSearch, "term");
        js.Set(AdminLogsStorageKeys.UserFilter, "user-1");
        js.Set(AdminLogsStorageKeys.SourceFilter, AdminAuditSources.SecurityPolicy);
        js.Set(AdminLogsStorageKeys.MessageFilter, "msg");
        js.Set(AdminLogsStorageKeys.LevelFilter, AdminAuditLevels.Success);
        js.Set(AdminLogsStorageKeys.StartDateFilter, new DateTime(2026, 3, 1).ToString("o", CultureInfo.InvariantCulture));
        js.Set(AdminLogsStorageKeys.EndDateFilter, new DateTime(2026, 3, 15).ToString("o", CultureInfo.InvariantCulture));
        js.Set(AdminLogsStorageKeys.Page, "3");
        js.Set(AdminLogsStorageKeys.QuickRangePreset, AdminLogsQuickRangePreset.Last30Days.ToString());
        js.Set(AdminLogsStorageKeys.AuditQuickFilter, AdminLogsAuditQuickFilter.SecurityPolicy.ToString());
        js.Set(AdminLogsStorageKeys.SortColumn, AdminLogSortColumn.Source.ToString());
        js.Set(AdminLogsStorageKeys.SortAscending, bool.TrueString);

        var service = new AdminLogsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal("term", state.GlobalSearch);
        Assert.Equal("user-1", state.UserId);
        Assert.Equal(AdminAuditSources.SecurityPolicy, state.Source);
        Assert.Equal(AdminAuditLevels.Success, state.Level);
        Assert.Equal(new DateTime(2026, 3, 1), state.StartDate);
        Assert.Equal(new DateTime(2026, 3, 15), state.EndDate);
        Assert.Equal(3, state.Page);
        Assert.Equal(AdminLogsQuickRangePreset.Last30Days, state.QuickRangePreset);
        Assert.Equal(AdminLogsAuditQuickFilter.SecurityPolicy, state.AuditQuickFilter);
        Assert.Equal(AdminLogSortColumn.Source, state.SortColumn);
        Assert.True(state.SortAscending);
    }

    [Fact]
    public async Task SaveAsync_WritesValuesAndRemovesMissingDates()
    {
        var js = new FakeLocalStorageJsRuntime();

        // Preload date keys to verify SaveAsync removes stale StartDate and overwrites EndDate.
        js.Set(AdminLogsStorageKeys.StartDateFilter, new DateTime(2026, 2, 1).ToString("o", CultureInfo.InvariantCulture));
        js.Set(AdminLogsStorageKeys.EndDateFilter, new DateTime(2026, 2, 2).ToString("o", CultureInfo.InvariantCulture));

        var service = new AdminLogsFilterStateService(js);

        await service.SaveAsync(new AdminLogsFilterState
        {
            GlobalSearch = "abc",
            UserId = "user-2",
            Source = "Webhook",
            Message = "hello",
            Level = AdminAuditLevels.Refused,
            StartDate = null,
            EndDate = new DateTime(2026, 3, 20),
            Page = 2,
            QuickRangePreset = AdminLogsQuickRangePreset.Custom,
            AuditQuickFilter = AdminLogsAuditQuickFilter.All,
            SortColumn = AdminLogSortColumn.Timestamp,
            SortAscending = false
        });

        Assert.Equal("abc", js.Get(AdminLogsStorageKeys.GlobalSearch));
        Assert.Equal("user-2", js.Get(AdminLogsStorageKeys.UserFilter));
        Assert.Equal("Webhook", js.Get(AdminLogsStorageKeys.SourceFilter));
        Assert.Equal(AdminAuditLevels.Refused, js.Get(AdminLogsStorageKeys.LevelFilter));
        Assert.Null(js.Get(AdminLogsStorageKeys.StartDateFilter));
        Assert.NotNull(js.Get(AdminLogsStorageKeys.EndDateFilter));
        Assert.Equal("2", js.Get(AdminLogsStorageKeys.Page));
        Assert.Equal(AdminLogsQuickRangePreset.Custom.ToString(), js.Get(AdminLogsStorageKeys.QuickRangePreset));
    }

    [Fact]
    public async Task LoadAsync_WithInvalidStoredValues_UsesDefaultsForTypedFields()
    {
        var js = new FakeLocalStorageJsRuntime();
        js.Set(AdminLogsStorageKeys.StartDateFilter, "not-a-date");
        js.Set(AdminLogsStorageKeys.EndDateFilter, "also-invalid");
        js.Set(AdminLogsStorageKeys.Page, "-10");
        js.Set(AdminLogsStorageKeys.QuickRangePreset, "InvalidPreset");
        js.Set(AdminLogsStorageKeys.AuditQuickFilter, "InvalidAudit");
        js.Set(AdminLogsStorageKeys.SortColumn, "InvalidSort");
        js.Set(AdminLogsStorageKeys.SortAscending, "not-bool");

        var service = new AdminLogsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Null(state.StartDate);
        Assert.Null(state.EndDate);
        Assert.Null(state.Page);
        Assert.Null(state.QuickRangePreset);
        Assert.Null(state.AuditQuickFilter);
        Assert.Null(state.SortColumn);
        Assert.Null(state.SortAscending);
    }

    [Fact]
    public async Task SaveAsync_WhenPageIsNull_PersistsPageOne()
    {
        var js = new FakeLocalStorageJsRuntime();
        var service = new AdminLogsFilterStateService(js);

        await service.SaveAsync(new AdminLogsFilterState
        {
            GlobalSearch = string.Empty,
            UserId = string.Empty,
            Source = string.Empty,
            Message = string.Empty,
            Level = string.Empty,
            Page = null
        });

        Assert.Equal("1", js.Get(AdminLogsStorageKeys.Page));
    }

    [Fact]
    public async Task LoadAsync_WhenJsInteropThrows_ReturnsDefaultState()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };
        var service = new AdminLogsFilterStateService(js);

        var state = await service.LoadAsync();

        Assert.Equal(string.Empty, state.GlobalSearch);
        Assert.Equal(string.Empty, state.UserId);
        Assert.Equal(string.Empty, state.Source);
        Assert.Equal(string.Empty, state.Message);
        Assert.Equal(string.Empty, state.Level);
        Assert.Null(state.StartDate);
        Assert.Null(state.EndDate);
        Assert.Null(state.Page);
        Assert.Null(state.QuickRangePreset);
        Assert.Null(state.AuditQuickFilter);
        Assert.Null(state.SortColumn);
        Assert.Null(state.SortAscending);
    }

    [Fact]
    public async Task SaveAsync_WhenJsInteropThrows_DoesNotPropagateException()
    {
        var js = new FakeLocalStorageJsRuntime
        {
            ThrowOnInvoke = true
        };
        var service = new AdminLogsFilterStateService(js);

        await service.SaveAsync(new AdminLogsFilterState
        {
            GlobalSearch = "x",
            Page = 3
        });
    }

    [Fact]
    public async Task SaveAndLoadAsync_PreservesPaymentPanelStaleAuditQuickFilter()
    {
        var js = new FakeLocalStorageJsRuntime();
        var service = new AdminLogsFilterStateService(js);

        await service.SaveAsync(new AdminLogsFilterState
        {
            GlobalSearch = "stale",
            AuditQuickFilter = AdminLogsAuditQuickFilter.PaymentPanelStale,
            QuickRangePreset = AdminLogsQuickRangePreset.Last7Days,
            Page = 1
        });

        var loaded = await service.LoadAsync();

        Assert.Equal("stale", loaded.GlobalSearch);
        Assert.Equal(AdminLogsAuditQuickFilter.PaymentPanelStale, loaded.AuditQuickFilter);
        Assert.Equal(AdminLogsQuickRangePreset.Last7Days, loaded.QuickRangePreset);
        Assert.Equal(1, loaded.Page);
    }
}


