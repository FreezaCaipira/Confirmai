using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Convention test (C36-D Fase 1): one shared button system lives in
/// wwwroot/css/components.css, loaded after shell.css and before the
/// scoped bundle in _Host.cshtml. Group screens must not use the old
/// button families (detail-back-link, btn-view-groups, groups-create-btn,
/// detail-admin-btn--*), and components.css itself must stay flat —
/// no gradients, no legacy --ci-* aliases.
/// </summary>
public class CssComponentsTests
{
    private static readonly string[] BannedGroupClasses =
    [
        "detail-back-link", "btn-view-groups", "groups-create-btn",
        "detail-admin-btn", "invite-copy-btn", "invite-block-btn",
        "events-expand-btn", "event-access-btn"
    ];

    private static readonly Regex LegacyVarRegex = new(
        @"var\(--(ci|futsal|poker|parchment|gold|green|red)-",
        RegexOptions.Compiled);

    private static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                return dir;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public void ComponentsCss_LoadsAfterShellCss_BeforeScopedBundle()
    {
        var host = File.ReadAllText(Path.Combine(RepoRoot().FullName, "Pages", "_Host.cshtml"));
        var shell = host.IndexOf("shell.css", StringComparison.Ordinal);
        var components = host.IndexOf("components.css", StringComparison.Ordinal);
        var scoped = host.IndexOf("Confirmai.styles.css", StringComparison.Ordinal);

        Assert.True(shell >= 0, "shell.css not referenced in _Host.cshtml");
        Assert.True(components > shell, "components.css must load after shell.css");
        Assert.True(scoped > components, "components.css must load before Confirmai.styles.css");
    }

    [Fact]
    public void ComponentsCss_DefinesTheFourVariants_AndSmallSize()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot().FullName, "wwwroot", "css", "components.css"));
        foreach (var selector in new[] { ".btn", ".btn-primary", ".btn-secondary", ".btn-ghost", ".btn-sm" })
        {
            Assert.True(Regex.IsMatch(css, Regex.Escape(selector) + @"\s*[\{,\.]"),
                $"components.css must define {selector}");
        }
    }

    [Fact]
    public void ComponentsCss_HasNoGradients_OrLegacyVars()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot().FullName, "wwwroot", "css", "components.css"));
        Assert.DoesNotContain("linear-gradient", css);
        Assert.False(LegacyVarRegex.IsMatch(css),
            "components.css must use only the new token names (no --ci-*, --green-*, etc.)");
    }

    [Fact]
    public void GroupPages_DoNotUseOldButtonFamilies()
    {
        var root = RepoRoot().FullName;
        var violations = new List<string>();
        foreach (var dir in new[] { Path.Combine(root, "Pages", "Groups"), Path.Combine(root, "Shared", "Components", "Groups") })
        {
            foreach (var file in Directory.GetFiles(dir, "*.razor", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                foreach (var cls in BannedGroupClasses)
                {
                    if (content.Contains(cls, StringComparison.Ordinal))
                        violations.Add($"{Path.GetFileName(file)}: {cls}");
                }
            }
        }
        Assert.True(violations.Count == 0,
            "Old button families found in group screens:\n" + string.Join("\n", violations));
    }
}
