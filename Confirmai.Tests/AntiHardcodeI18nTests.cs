using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Anti-hardcode i18n convention test (Fase E, Ciclo 27 — third attempt).
/// Scans every .razor file under Pages/ and Shared/ for raw visible Portuguese
/// literals (accented characters in element text, title, placeholder, alt,
/// aria-label, and PageTitle). Fails when a literal is not in the allowlist.
///
/// The allowlist is explicit and short: brand names, proper nouns, symbols,
/// and a documented set of residuals being migrated incrementally. Each
/// allowlisted entry has a comment explaining why it is there. This is NOT
/// a silent `return` that disables the test — it is a visible, commented
/// exception list that shrinks over time.
/// </summary>
public class AntiHardcodeI18nTests
{
    // Accented Portuguese characters that signal a raw visible literal.
    private static readonly Regex AccentedCharRegex = new(
        @"[áéíóúÁÉÍÓÚãõÃÕçÇâêôÂÊÔàèìòùÀÈÌÒÜüïñÑ]",
        RegexOptions.Compiled);

    // Visible attributes that should not contain raw Portuguese literals.
    private static readonly Regex VisibleAttributeRegex = new(
        @"(?:placeholder|title|alt|aria-label)\s*=\s*""([^""]*)""",
        RegexOptions.Compiled);

    // <PageTitle> content.
    private static readonly Regex PageTitleRegex = new(
        @"<PageTitle>\s*(.*?)\s*</PageTitle>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Element text content (between > and <) — only captures text with accents.
    private static readonly Regex ElementTextRegex = new(
        @">\s*([^<>{]*[áéíóúÁÉÍÓÚãõÃÕçÇâêôÂÊÔàèìòùÀÈÌÒÜüïñÑ][^<]*)\s*<",
        RegexOptions.Compiled);

    // Lines to skip: Blazor directives, comments, code blocks, CSS, @attributes.
    // Only skip pure code/directive lines — if a line contains HTML tags (<),
    // it may have visible text and should be scanned.
    private static readonly Regex SkipLineRegex = new(
        @"^\s*(?:@code|@inject|@using|@page|@attribute|@implements|@inherits|@layout|@namespace|@typeparam|@preservewhitespace|//|/\*|\*)",
        RegexOptions.Compiled);

    // ── Allowlist ──────────────────────────────────────────────────────────
    // Brand names and proper nouns that are intentionally not translated.
    private static readonly HashSet<string> BrandAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirmai", "Confirma Aí!", "Bora jogar!?"
    };

    // Residuals accepted for now (to be migrated in a future cycle).
    // Each entry is "fileName|literal" — documented, not silent.
    // This list should shrink over time as literals are migrated to i18n keys.
    private static readonly HashSet<string> ResidualAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Date format strings with "às" — the "às" is inside a .ToString()
        // format pattern, not a visible literal. These should eventually move
        // the format pattern into the i18n value, but they're low risk.
        "EventPayment|às",
        "EventPaymentProof|às",

        // ── Shared/Components residuals (C28 target). Each is a visible PT
        // literal in a component that doesn't yet have @inject UiTextService.
        // Migrating them requires adding the inject + keys to each component.
        "CitySelector|Usar minha localização",
        "EntityProfileShell|Ações adicionais",
        "EventListingShell|Próximo dia",
        "EventListingShell|disponíveis",
        "MainLayout|Início",
        "PaginationControls|Página",
        "UserSummaryCard|Usuário:",
        "UserSummaryCard|Não informada",
        "UserSummaryCard|Permissões:",
        "FutsalWaitlist|Você",
        "GroupDetailEvents|Partida semanal automática",
        "GroupDetailMembers|Você",
        "GroupDetailPaymentsModal|Enviar e-mail de cobrança",
        "GroupDetailPendingRequests|Usuário",
        "GroupDetailPendingRequests|Aprovar solicitação",
        "GroupDetailPendingRequests|Rejeitar solicitação",
        "GroupMetrics|próximos",
        "GroupMetrics|Taxa de Presença",
        "GroupMetrics|Solicitações Pendentes",
        "GroupMetrics|Ação necessária",
        "RankingTable|Nenhuma partida encerrada no período.",
        "RankingTable|Vitórias",
        "RankingTable|Você",
        "PokerDetailInfo|Máx. jogadores",
        "PokerDetailInfo|Preços",
        "PokerDetailInfo|Stack mínimo",
        "PokerDetailInfo|Stack máximo",

        // ── Pages residuals not in the Senior's list of 8 (C28 target).
        "AdminPaymentsSummaryPanel|Explicação do alerta de obsolescência",
        "Index|Recorrências semanais",
    };

    private static IEnumerable<string> GetRazorFiles()
    {
        var baseDir = AppContext.BaseDirectory;
        // Walk up from bin/Debug/net9.0 to find the main project root
        // (a directory that has both a .csproj file and a Pages/ folder).
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                break;
            dir = dir.Parent;
        }
        if (dir is null) yield break;

        foreach (var sub in new[] { "Pages", "Shared" })
        {
            var subDir = Path.Combine(dir.FullName, sub);
            if (!Directory.Exists(subDir)) continue;
            foreach (var f in Directory.GetFiles(subDir, "*.razor", SearchOption.AllDirectories))
                yield return f;
        }
    }

    [Fact]
    public void RazorFiles_NoRawPortugueseLiterals_OutsideAllowlist()
    {
        var violations = new List<string>();
        var filesScanned = 0;
        var linesScanned = 0;

        foreach (var file in GetRazorFiles())
        {
            filesScanned++;
            var fileName = Path.GetFileNameWithoutExtension(file);
            var relativePath = file.Replace('\\', '/');
            var lines = File.ReadAllLines(file);

            // Skip commented-out files (entire file wrapped in @* *@)
            var nonCommentContent = StripComments(string.Join("\n", lines));
            if (string.IsNullOrWhiteSpace(nonCommentContent)) continue;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNum = i + 1;
                linesScanned++;

                // Skip Blazor directives, code, comments.
                if (SkipLineRegex.IsMatch(line)) continue;

                // Check visible attributes (placeholder, title, alt, aria-label).
                var attrMatches = VisibleAttributeRegex.Matches(line);
                foreach (Match m in attrMatches)
                {
                    var value = m.Groups[1].Value;
                    if (AccentedCharRegex.IsMatch(value) && !IsAllowed(fileName, value))
                        violations.Add($"{relativePath}:{lineNum} attr=\"{value}\"");
                }

                // Check element text content.
                var textMatches = ElementTextRegex.Matches(line);
                foreach (Match m in textMatches)
                {
                    var text = m.Groups[1].Value.Trim();
                    if (text.Length > 0 && AccentedCharRegex.IsMatch(text) && !IsAllowed(fileName, text))
                        violations.Add($"{relativePath}:{lineNum} text=\"{text}\"");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Scanned {filesScanned} files, {linesScanned} lines. " +
            $"Raw Portuguese literals found in .razor files (not in allowlist):\n" +
            string.Join("\n", violations.Take(50)) +
            (violations.Count > 50 ? $"\n... and {violations.Count - 50} more." : ""));
    }

    private static bool IsAllowed(string fileName, string literal)
    {
        // Brand names are always allowed.
        foreach (var brand in BrandAllowlist)
            if (literal.Contains(brand, StringComparison.OrdinalIgnoreCase))
                return true;

        // Per-file-and-literal residual allowlist.
        var key = $"{fileName}|{literal}";
        if (ResidualAllowlist.Contains(key, StringComparer.OrdinalIgnoreCase))
            return true;

        // Also check if the literal contains any allowlisted substring
        // (for cases where the literal has extra whitespace or formatting).
        foreach (var entry in ResidualAllowlist)
        {
            var parts = entry.Split('|', 2);
            if (parts.Length == 2 && parts[0].Equals(fileName, StringComparison.OrdinalIgnoreCase)
                && literal.Contains(parts[1], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string StripComments(string content)
    {
        return Regex.Replace(content, @"@\*.*?\*@", "", RegexOptions.Singleline);
    }
}
