using Microsoft.Extensions.Configuration;

namespace Confirmai.Tests;

internal static class TestConfigurationFactory
{
    public static IConfiguration Create(IReadOnlyDictionary<string, string?> entries)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(entries)
            .Build();
    }

    public static IConfiguration Create(params (string Key, string? Value)[] entries)
    {
        var dictionary = entries.ToDictionary(item => item.Key, item => item.Value);
        return Create(dictionary);
    }
}

