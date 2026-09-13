using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// C35: reads wwwroot/css/tokens.css and asserts the WCAG contrast ratios
/// the design system promises, so a palette tweak cannot silently drop
/// below AA. Also guards the load order: tokens.css must come before
/// site.css in every host page, otherwise the aliases resolve to nothing.
/// </summary>
public class DesignTokensContrastTests
{
    private static readonly Regex TokenRegex = new(
        @"--([a-z0-9-]+)\s*:\s*(#[0-9a-fA-F]{6})\s*;",
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

    private static Dictionary<string, string> LoadTokens()
    {
        var path = Path.Combine(RepoRoot().FullName, "wwwroot", "css", "tokens.css");
        Assert.True(File.Exists(path), "tokens.css not found");
        var tokens = new Dictionary<string, string>();
        foreach (Match m in TokenRegex.Matches(File.ReadAllText(path)))
            tokens[m.Groups[1].Value] = m.Groups[2].Value;
        return tokens;
    }

    private static double Luminance(string hex)
    {
        double Channel(int i)
        {
            var c = int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }

    private static double Contrast(string fg, string bg)
    {
        var l1 = Luminance(fg);
        var l2 = Luminance(bg);
        var (hi, lo) = l1 > l2 ? (l1, l2) : (l2, l1);
        return (hi + 0.05) / (lo + 0.05);
    }

    [Theory]
    [InlineData("text", "bg", 12.0)]
    [InlineData("text", "surface", 12.0)]
    [InlineData("text", "surface-2", 12.0)]
    [InlineData("text-2", "bg", 7.0)]
    [InlineData("text-2", "surface", 7.0)]
    [InlineData("text-2", "surface-2", 7.0)]
    [InlineData("text-3", "surface", 4.5)]
    [InlineData("accent-fg", "accent", 4.5)]
    [InlineData("accent-text", "bg", 4.5)]
    [InlineData("accent-text", "surface", 4.5)]
    public void Tokens_MeetAaContrast(string fg, string bg, double minimum)
    {
        var tokens = LoadTokens();
        Assert.True(tokens.ContainsKey(fg), $"--{fg} missing");
        Assert.True(tokens.ContainsKey(bg), $"--{bg} missing");

        var ratio = Contrast(tokens[fg], tokens[bg]);
        Assert.True(ratio >= minimum,
            $"--{fg} ({tokens[fg]}) on --{bg} ({tokens[bg]}) = {ratio:F2}:1, expected >= {minimum}:1");
    }

    [Fact]
    public void Tokens_DefineTheSemanticSet()
    {
        var tokens = LoadTokens();
        var required = new[]
        {
            "bg", "surface", "surface-2", "surface-3", "border", "border-2",
            "text", "text-2", "text-3", "accent", "accent-hover", "accent-fg", "accent-text",
            "success", "warning", "danger",
        };
        var missing = required.Where(t => !tokens.ContainsKey(t)).ToList();
        Assert.True(missing.Count == 0, "missing tokens: " + string.Join(", ", missing));
    }

    [Theory]
    [InlineData("Pages/_Host.cshtml")]
    [InlineData("Pages/Error.cshtml")]
    [InlineData("Areas/Identity/Pages/_Layout.cshtml")]
    public void HostPages_LoadTokensBeforeSiteCss(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(RepoRoot().FullName, relativePath));
        var tokensIdx = content.IndexOf("css/tokens.css", StringComparison.Ordinal);
        var siteIdx = content.IndexOf("css/site.css", StringComparison.Ordinal);

        Assert.True(tokensIdx >= 0, $"{relativePath} does not load tokens.css");
        Assert.True(siteIdx >= 0, $"{relativePath} does not load site.css");
        Assert.True(tokensIdx < siteIdx, $"{relativePath} must load tokens.css before site.css");
    }

    [Fact]
    public void SiteCss_CoreVarsAreAliasesOfTokens()
    {
        var site = File.ReadAllText(Path.Combine(RepoRoot().FullName, "wwwroot", "css", "site.css"));
        var expected = new Dictionary<string, string>
        {
            ["--ci-bg"] = "var(--bg)",
            ["--ci-bg-card"] = "var(--surface)",
            ["--ci-accent"] = "var(--accent-text)",
            ["--ci-accent-mid"] = "var(--accent)",
            ["--ci-border"] = "var(--border)",
            ["--ci-text"] = "var(--text)",
            ["--ci-text-muted"] = "var(--text-2)",
        };

        foreach (var (name, value) in expected)
        {
            var m = Regex.Match(site, Regex.Escape(name) + @"\s*:\s*([^;]+);");
            Assert.True(m.Success, $"{name} not defined in site.css");
            Assert.Equal(value, m.Groups[1].Value.Trim());
        }
    }
}
