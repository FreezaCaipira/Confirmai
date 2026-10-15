using System.Reflection;

namespace Confirmai.Services.Core;

/// <summary>
/// C42-A: single source for the app version shown in the header and reported
/// to OTel. Reads AssemblyInformationalVersion and strips the "+sha" suffix
/// SourceLink may append.
/// </summary>
public static class AppVersion
{
    public static readonly string Current = Load();

    private static string Load()
    {
        var attr = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = attr?.InformationalVersion ?? "0.0.0";
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
