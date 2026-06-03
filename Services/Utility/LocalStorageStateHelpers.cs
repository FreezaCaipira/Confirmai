using Microsoft.JSInterop;
using System.Globalization;

namespace Confirmai.Services.Utility;

internal static class LocalStorageStateHelpers
{
    public static async Task<string> GetStringAsync(IJSRuntime js, string key)
        => await js.InvokeAsync<string?>("localStorage.getItem", key) ?? string.Empty;

    public static async Task<int?> GetPositiveIntAsync(IJSRuntime js, string key)
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : null;
    }

    public static async Task<int?> GetNonNegativeIntAsync(IJSRuntime js, string key)
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return int.TryParse(raw, out var parsed) && parsed >= 0 ? parsed : null;
    }

    public static async Task<decimal?> GetDecimalAsync(IJSRuntime js, string key)
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public static async Task<DateTime?> GetDateAsync(IJSRuntime js, string key)
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed.Date
            : null;
    }

    public static async Task<TEnum?> GetEnumAsync<TEnum>(IJSRuntime js, string key)
        where TEnum : struct
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return Enum.TryParse<TEnum>(raw, out var parsed) ? parsed : null;
    }

    public static async Task<bool?> GetNullableBoolAsync(IJSRuntime js, string key)
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return bool.TryParse(raw, out var parsed) ? parsed : null;
    }

    public static Task SetStringAsync(IJSRuntime js, string key, string value)
        => js.InvokeVoidAsync("localStorage.setItem", key, value ?? string.Empty).AsTask();

    public static Task SetPageAsync(IJSRuntime js, string key, int? page)
        => js.InvokeVoidAsync("localStorage.setItem", key, (page ?? 1).ToString(CultureInfo.InvariantCulture)).AsTask();

    public static Task SetOrRemoveIntAsync(IJSRuntime js, string key, int? value)
        => value.HasValue
            ? js.InvokeVoidAsync("localStorage.setItem", key, value.Value.ToString(CultureInfo.InvariantCulture)).AsTask()
            : js.InvokeVoidAsync("localStorage.removeItem", key).AsTask();

    public static Task SetOrRemoveDecimalAsync(IJSRuntime js, string key, decimal? value)
        => value.HasValue
            ? js.InvokeVoidAsync("localStorage.setItem", key, value.Value.ToString(CultureInfo.InvariantCulture)).AsTask()
            : js.InvokeVoidAsync("localStorage.removeItem", key).AsTask();

    public static Task SetOrRemoveDateAsync(IJSRuntime js, string key, DateTime? value)
        => value.HasValue
            ? js.InvokeVoidAsync("localStorage.setItem", key, value.Value.ToString("o", CultureInfo.InvariantCulture)).AsTask()
            : js.InvokeVoidAsync("localStorage.removeItem", key).AsTask();
}


