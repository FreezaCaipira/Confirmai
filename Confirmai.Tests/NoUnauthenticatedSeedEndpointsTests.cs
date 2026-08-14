using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Convention test: Program.cs must not expose seed/debug endpoints that write
/// to the database outside a development-only guard. Ciclo 27 shipped
/// /api/seed-fee-test to main with no authorization and no environment check,
/// letting anonymous callers stamp fees and create approved settlements.
/// </summary>
public class NoUnauthenticatedSeedEndpointsTests
{
    private static string ReadProgramSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Program.cs")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "Program.cs"));
    }

    [Fact]
    public void Program_DoesNotMapSeedOrDebugEndpoints_AtRootLevel()
    {
        var source = ReadProgramSource();

        // Endpoints mapped at root level (no leading indentation) are always
        // registered — the dev-only ones live inside an if block and are indented.
        var rootMappings = Regex.Matches(source, @"^app\.Map\w+\(""([^""]+)""", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .Where(route => route.Contains("seed", StringComparison.OrdinalIgnoreCase)
                || route.Contains("debug", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(rootMappings.Count == 0,
            "Seed/debug endpoints must be inside the development-only block: "
            + string.Join(", ", rootMappings));
    }
}
