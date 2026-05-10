using Microsoft.JSInterop;

namespace Confirmai.Tests.TestDoubles;

internal sealed class FakeLocalStorageJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string?> _storage = new();

    public bool ThrowOnInvoke { get; set; }

    public string? Get(string key)
        => _storage.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string? value)
        => _storage[key] = value;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (ThrowOnInvoke)
        {
            throw new InvalidOperationException("Simulated JS failure");
        }

        if (identifier == "localStorage.getItem")
        {
            var key = args?[0]?.ToString() ?? string.Empty;
            _storage.TryGetValue(key, out var value);
            return new ValueTask<TValue>((TValue)(object?)value!);
        }

        if (identifier == "localStorage.setItem")
        {
            var key = args?[0]?.ToString() ?? string.Empty;
            var value = args?.Length > 1 ? args[1]?.ToString() : null;
            _storage[key] = value;
            return new ValueTask<TValue>(default(TValue)!);
        }

        if (identifier == "localStorage.removeItem")
        {
            var key = args?[0]?.ToString() ?? string.Empty;
            _storage.Remove(key);
            return new ValueTask<TValue>(default(TValue)!);
        }

        throw new NotSupportedException($"Unsupported JS identifier: {identifier}");
    }
}


