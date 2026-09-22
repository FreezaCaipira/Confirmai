using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Convention test (C36-D Fase 5): every CSS file that has been migrated
/// to the design-token system must stay migrated — no linear-gradient and
/// no legacy variable families (--ci-*, --futsal-*, --poker-*,
/// --parchment-*, --gold-*, --green-*, --red-*).
/// The MigratedFiles list only grows: when a phase migrates a file, add it
/// here; never remove an entry.
/// Sole exception: .home-hero rules may keep the accent gradient
/// (documented exception in docs/design-system.md §1 regra 5, C36-D Fase 3).
/// </summary>
public class CssNoLegacyVarsTests
{
    private static readonly string[] MigratedFiles =
    [
        // Shared system
        "wwwroot/css/components.css",
        "wwwroot/css/tokens.css",
        "wwwroot/css/shell.css",
        // Home (migrated in C36-B; hero gradient is Fase 3's exception)
        "Pages/Index.razor.css",
        // Group screens (C36-D Fases 1, 4, 7, 8)
        "Pages/Groups/Detail.razor.css",
        "Pages/Groups/Features.razor.css",
        "Pages/Groups/Index.razor.css",
        "Pages/Groups/Join.razor.css",
        "Pages/Groups/Partidas.razor.css",
        "Pages/Groups/Ranking.razor.css",
        "Shared/Components/Groups/GroupDetailEvents.razor.css",
        "Shared/Components/Groups/GroupDetailMembers.razor.css",
        "Shared/Components/Groups/GroupEntryPanel.razor.css",
        "Shared/Components/Groups/GroupHeader.razor.css",
        "Shared/Components/Groups/GroupMetrics.razor.css",
        "Shared/Components/Groups/NoGroupsHint.razor.css",
        "Shared/Components/Groups/RankingTable.razor.css",
        "Shared/Components/Groups/RankingViewSelector.razor.css",
        // Mailbox (C36-D Fase 5)
        "Pages/Mailbox.razor.css",
        "Pages/Components/MailboxConversationList.razor.css",
        "Pages/Components/MailboxFilters.razor.css",
        "Pages/Components/MailboxThreadPane.razor.css",
    ];

    private static readonly Regex LegacyVarRegex = new(
        @"var\(--(ci|futsal|poker|parchment|gold|green|red)-",
        RegexOptions.Compiled);

    private static readonly Regex HomeHeroRuleRegex = new(
        @"\.home-hero[^{}]*\{[^{}]*\}",
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
    public void MigratedFiles_AllExist()
    {
        var root = RepoRoot().FullName;
        var missing = MigratedFiles.Where(f => !File.Exists(Path.Combine(root, f))).ToList();
        Assert.True(missing.Count == 0,
            "MigratedFiles entries that no longer exist (fix the path; the list only grows):\n"
            + string.Join("\n", missing));
    }

    [Fact]
    public void MigratedFiles_HaveNoLegacyVars()
    {
        var root = RepoRoot().FullName;
        var violations = new List<string>();
        foreach (var file in MigratedFiles)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path)) continue;
            if (LegacyVarRegex.IsMatch(File.ReadAllText(path)))
                violations.Add(file);
        }
        Assert.True(violations.Count == 0,
            "Legacy variable families in migrated files:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void MigratedFiles_HaveNoGradients_ExceptHomeHero()
    {
        var root = RepoRoot().FullName;
        var violations = new List<string>();
        foreach (var file in MigratedFiles)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path)) continue;
            // .home-hero is the documented gradient exception: strip its
            // rules before checking for linear-gradient.
            var css = HomeHeroRuleRegex.Replace(File.ReadAllText(path), string.Empty);
            if (css.Contains("linear-gradient", StringComparison.Ordinal))
                violations.Add(file);
        }
        Assert.True(violations.Count == 0,
            "linear-gradient outside .home-hero in migrated files:\n" + string.Join("\n", violations));
    }
}
