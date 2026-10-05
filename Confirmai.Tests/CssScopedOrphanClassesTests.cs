using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C37 Fase 0 — Blazor scoped CSS only styles the markup of its own component
/// (the b-xxxx attribute). A class used in X.razor but defined ONLY in some
/// other component's X.razor.css renders completely unstyled — that is what
/// produced the "plain HTML list" findings in prod.
///
/// An orphan is a class used in <c>class="..."</c> of a .razor file that is
/// defined neither in a global stylesheet (wwwroot/css/*.css) nor in that
/// file's own .razor.css, but IS defined in a different component's
/// .razor.css. Classes defined nowhere are ignored (JS hooks, dynamic).
///
/// The allowlist below may only SHRINK: it contains the specific orphan
/// classes currently tolerated per file (legacy flows at the end of C37).
/// Adding a new orphan fails; fixing one without removing the entry fails too.
/// </summary>
public class CssScopedOrphanClassesTests
{
    // ── Allowlist (only shrinks). Path -> exact orphan classes tolerated. ────
    // Legacy flows out of the product (marketplace, futsal/schedule, admin
    // tables) keep their orphans until those pages are removed (C32 Fase F).
    private static readonly IReadOnlyDictionary<string, string[]> AllowedOrphans =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // === In-flow orphans — zerados ao longo do C37 (fases 3-5) ===
            ["Shared/Components/Poker/PokerDetailInfo.razor"] = new[]
            {
                "cash-includes-block", "cash-includes-label",
                "cash-includes-value", "homegame-unlocked-notice",
                "poker-detail-item", "poker-detail-label", "poker-detail-value",
                "poker-detail-value--gtd", "poker-detail-value--price",
                "poker-details-grid"
            },
            ["Pages/Futsal/Components/RecurrenceScheduler.razor"] = new[]
            {
                "weekday-chip", "weekday-grid"
            },
            ["Pages/Poker/Edit.razor"] = new[]
            {
                "homegame-notice", "modality-chip", "modality-grid"
            },
            ["Pages/Futsal/Components/DetailEventHeader.razor"] = new[]
            {
                "detail-header--futsal"
            },

            // === Legado fora do fluxo (ate a C32 Fase F remover as telas) ===
            ["Pages/Payment/PaymentCheckoutPanel.razor"] = new[]
            {
                "no-gateways-hint", "payment-inline-feedback",
                "payment-inline-feedback-warning", "tibia-pay-action-wrap",
                "tibia-pay-address", "tibia-pay-address-wrap", "tibia-pay-btn",
                "tibia-pay-card", "tibia-pay-card-body", "tibia-pay-card-checkout",
                "tibia-pay-card-head", "tibia-pay-copy-btn", "tibia-pay-field",
                "tibia-pay-form-grid", "tibia-pay-label",
                "tibia-pay-no-intermediary-warning", "tibia-pay-pill",
                "tibia-pay-pill-ok", "tibia-pay-private-key",
                "tibia-pay-private-key-value", "tibia-pay-qrcode",
                "tibia-pay-select", "tibia-pay-status-block",
                "tibia-pay-status-main", "tibia-pay-status-row",
                "tibia-pay-switch", "tibia-pay-switch-block",
                "tibia-pay-switch-lever", "tibia-pay-switch-text",
                "tibia-pay-waiting-message", "tibia-pay-waiting-title"
            },
            ["Pages/Payment/PaymentProductSummary.razor"] = new[]
            {
                "tibia-pay-card", "tibia-pay-card-body", "tibia-pay-card-head",
                "tibia-pay-item-card", "tibia-pay-item-media",
                "tibia-pay-item-row", "tibia-pay-item-row-price",
                "tibia-pay-item-row-total", "tibia-pay-item-subcard",
                "tibia-pay-label", "tibia-pay-product-image",
                "tibia-pay-seller-highlight", "tibia-pay-seller-inline",
                "tibia-pay-seller-link-wrap", "tibia-pay-value",
                "tibia-pay-value-price", "tibia-pay-value-total"
            },
            ["Pages/Futsal/Schedule/Edit.razor"] = new[]
            {
                "create-event-card-top"
            },
            ["Pages/Admin/AdminAuditTimeline.razor"] = new[]
            {
                "logs-empty-state"
            },
            ["Pages/Admin/AdminVenues.razor"] = new[]
            {
                "admin-actions-row"
            },
            ["Pages/Admin/Components/AdminPaymentsTable.razor"] = new[]
            {
                "admin-actions-row"
            },
            ["Shared/Components/Admin/AdminLogsTable.razor"] = new[]
            {
                "log-table-container", "logs-empty-state", "logs-timeline-link"
            },
            ["Shared/Components/Admin/AdminUsersTable.razor"] = new[]
            {
                "admin-actions-row"
            },
        };

    private static readonly Regex CommentRegex = new(@"/\*.*?\*/",
        RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CssClassRegex = new(@"\.([a-z][\w-]*)",
        RegexOptions.Compiled);
    private static readonly Regex ClassAttrRegex = new(@"\bclass\s*=\s*""([^""]*)""",
        RegexOptions.Compiled);
    private static readonly Regex RazorExprRegex = new(@"@\(.*?\)|@\w+",
        RegexOptions.Compiled);
    private static readonly Regex ValidClassRegex = new(@"^[a-z][\w-]+$",
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

    private static IEnumerable<string> Enumerate(string root, string ext)
    {
        var sep = Path.DirectorySeparatorChar;
        string[] excluded = [$"{sep}bin{sep}", $"{sep}obj{sep}", $"{sep}lib{sep}",
            $"{sep}node_modules{sep}", $"{sep}Confirmai.Tests{sep}", $"{sep}Migrations{sep}"];
        return Directory.EnumerateFiles(root, ext, SearchOption.AllDirectories)
            .Where(f => !excluded.Any(f.Contains));
    }

    private static HashSet<string> ExtractCssClasses(string css)
    {
        var text = CommentRegex.Replace(css, " ");
        return CssClassRegex.Matches(text)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> ExtractRazorClasses(string razor)
    {
        var classes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in ClassAttrRegex.Matches(razor))
        {
            var value = RazorExprRegex.Replace(m.Groups[1].Value, " ");
            foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (ValidClassRegex.IsMatch(token))
                    classes.Add(token);
        }
        return classes;
    }

    /// <summary>Orphan classes per .razor file: file path (relative) -> classes.</summary>
    internal static Dictionary<string, List<string>> FindOrphans(string root)
    {
        var globalClasses = new HashSet<string>(StringComparer.Ordinal);
        var cssDir = Path.Combine(root, "wwwroot", "css");
        foreach (var file in Directory.EnumerateFiles(cssDir, "*.css"))
            globalClasses.UnionWith(ExtractCssClasses(File.ReadAllText(file)));

        // path (relative, forward slashes) -> classes defined in that scoped css
        var scopedByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Enumerate(root, "*.razor.css"))
        {
            var razorPath = file[..^".css".Length]; // strip .css -> the .razor path
            scopedByFile[Path.GetRelativePath(root, razorPath).Replace('\\', '/')] =
                ExtractCssClasses(File.ReadAllText(file));
        }

        var orphans = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var razorFile in Enumerate(root, "*.razor"))
        {
            var rel = Path.GetRelativePath(root, razorFile).Replace('\\', '/');
            scopedByFile.TryGetValue(rel, out var ownClasses);

            var used = ExtractRazorClasses(File.ReadAllText(razorFile));
            var fileOrphans = used
                .Where(c => !globalClasses.Contains(c)
                            && !(ownClasses?.Contains(c) ?? false)
                            && scopedByFile.Values.Any(v => v.Contains(c)))
                .OrderBy(c => c)
                .ToList();

            if (fileOrphans.Count > 0)
                orphans[rel] = fileOrphans;
        }
        return orphans;
    }

    [Fact]
    public void NoRazorFile_UsesClassesDefinedOnlyInAnotherComponentsCss()
    {
        var root = RepoRoot().FullName;
        var orphans = FindOrphans(root);

        var violations = new List<string>();
        foreach (var (file, classes) in orphans)
        {
            if (!AllowedOrphans.TryGetValue(file, out var allowed))
            {
                violations.Add($"{file}: {string.Join(", ", classes)} (sem entrada na allowlist)");
                continue;
            }
            var unexpected = classes.Except(allowed, StringComparer.Ordinal).ToList();
            if (unexpected.Count > 0)
                violations.Add($"{file}: orfas novas nao permitidas: {string.Join(", ", unexpected)}");
        }

        // Allowlist only shrinks: every listed class must still be an orphan.
        foreach (var (file, allowed) in AllowedOrphans)
        {
            var current = orphans.TryGetValue(file, out var o) ? o : new List<string>();
            var stale = allowed.Except(current, StringComparer.Ordinal).ToList();
            if (stale.Count > 0)
                violations.Add($"{file}: entradas obsoletas na allowlist (remover): {string.Join(", ", stale)}");
        }

        Assert.True(violations.Count == 0,
            "Classes orfas de CSS scoped (estilizadas so no CSS de outro componente):\n"
            + string.Join("\n", violations));
    }
}
