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

    /// <summary>
    /// C37 Fase 1 (item 1 do Robson): .identity-container button (0,1,1) used
    /// to beat .google-signin-btn (0,1,0) — the Google button inherited the
    /// blue/min-width of "Entrar". The variant must keep the
    /// .identity-container prefix and reset what the generic rule injects.
    /// </summary>
    [Fact]
    public void GoogleSignInButton_SelectorOutranks_GenericIdentityButton()
    {
        var css = File.ReadAllText(Path.Combine(RepoRoot().FullName, "wwwroot", "css", "identity.css"));

        // The rule that defines the Google button's background must carry the
        // .identity-container prefix (specificity 0,2,0 > generic 0,1,1).
        var rule = Regex.Match(css,
            @"([^{}]+)\{[^}]*background:\s*#fff");
        Assert.True(rule.Success, "google-signin-btn background rule not found");
        Assert.Contains(".identity-container", rule.Groups[1].Value);
        Assert.Contains(".google-signin-btn", rule.Groups[1].Value);

        // And it must reset the properties the generic rule injects.
        var block = Regex.Match(css,
            @"\.identity-container\s+\.google-signin-btn\s*\{([^}]*)");
        Assert.True(block.Success);
        foreach (var decl in new[] { "min-width: 0", "margin: 0", "min-height: 40px" })
            Assert.Contains(decl, block.Groups[1].Value);

        // The media query that re-styles .identity-container button must not
        // hit the Google variant.
        Assert.Contains("button:not(.google-signin-btn)", css);
    }

    /// <summary>
    /// C38 Fase 2 (testador externo): .seg-toggle mede ~350px de largura minima
    /// e estoura /grupo/{id}/partidas em 320px — os itens precisam quebrar de
    /// linha, e dados do usuario (nome do grupo, local da partida) ganham
    /// overflow-wrap onde sao renderizados.
    /// </summary>
    [Fact]
    public void SegToggle_Wraps_AndUserData_BreaksAt320px()
    {
        var root = RepoRoot().FullName;
        var components = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "components.css"));

        var segToggle = Regex.Match(components, @"\.seg-toggle\s*\{([^}]*)");
        Assert.True(segToggle.Success, ".seg-toggle rule not found in components.css");
        Assert.Contains("flex-wrap: wrap", segToggle.Groups[1].Value);
        Assert.Contains("max-width: 100%", segToggle.Groups[1].Value);

        var groupHeader = File.ReadAllText(Path.Combine(root, "Shared", "Components", "Groups", "GroupHeader.razor.css"));
        var title = Regex.Match(groupHeader, @"\.group-header-title\s*\{([^}]*)");
        Assert.True(title.Success);
        Assert.Contains("overflow-wrap: anywhere", title.Groups[1].Value);

        var events = File.ReadAllText(Path.Combine(root, "Shared", "Components", "Groups", "GroupDetailEvents.razor.css"));
        var tipMeta = Regex.Match(events, @"\.et-tip-meta\s*\{([^}]*)");
        Assert.True(tipMeta.Success);
        Assert.Contains("overflow-wrap: anywhere", tipMeta.Groups[1].Value);
    }
}
