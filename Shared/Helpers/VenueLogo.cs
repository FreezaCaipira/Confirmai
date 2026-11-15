namespace Confirmai.Shared.Helpers;

/// <summary>Logo de quadra: a CSP so libera imagens do proprio site.</summary>
public static class VenueLogo
{
    public static bool IsLocalPath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && path.StartsWith('/')
        && !path.StartsWith("//", StringComparison.Ordinal)
        && !path.Contains('\\')
        && !path.Contains("://", StringComparison.Ordinal);
}
