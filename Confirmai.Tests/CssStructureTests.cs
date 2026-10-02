using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Every first-party stylesheet must be structurally valid: balanced braces
/// and no declarations outside a rule. A declaration left at the top level
/// (e.g. after deleting only the selector line) is swallowed by the browser
/// into the next rule's selector, silently dropping that rule.
/// </summary>
public class CssStructureTests
{
    private static readonly Regex CommentRegex = new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);

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

    private static IEnumerable<string> FirstPartyCssFiles(string root)
    {
        var sep = Path.DirectorySeparatorChar;
        string[] excluded = [$"{sep}bin{sep}", $"{sep}obj{sep}", $"{sep}lib{sep}", $"{sep}node_modules{sep}", $"{sep}Confirmai.Tests{sep}"];
        return Directory.EnumerateFiles(root, "*.css", SearchOption.AllDirectories)
            .Where(f => !excluded.Any(f.Contains))
            .Where(f => !f.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindStructuralError(string css)
    {
        var text = CommentRegex.Replace(css, " ");
        var depth = 0;
        var segmentStart = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth < 0)
                        return $"unmatched '}}' near: {Snippet(text, i)}";
                    if (depth == 0) segmentStart = i + 1;
                    break;
                case ';' when depth == 0:
                    var segment = text[segmentStart..i].Trim();
                    if (!segment.StartsWith('@'))
                        return $"declaration outside any rule: '{segment}'";
                    segmentStart = i + 1;
                    break;
            }
        }
        return depth != 0 ? "unbalanced braces (missing '}')" : null;
    }

    private static string Snippet(string text, int index)
    {
        var start = Math.Max(0, index - 60);
        return text[start..Math.Min(text.Length, index + 1)].Replace('\n', ' ').Trim();
    }

    [Fact]
    public void FirstPartyCss_IsStructurallyValid()
    {
        var root = RepoRoot().FullName;
        var violations = new List<string>();
        foreach (var file in FirstPartyCssFiles(root))
        {
            var error = FindStructuralError(File.ReadAllText(file));
            if (error is not null)
                violations.Add($"{Path.GetRelativePath(root, file)}: {error}");
        }
        Assert.True(violations.Count == 0, "Invalid CSS structure:\n" + string.Join("\n", violations));
    }

    [Theory]
    [InlineData(".a { color: red; }\n    display: flex;\n}\n.b { color: blue; }")]
    [InlineData(".a { color: red; ")]
    public void FindStructuralError_FlagsBrokenCss(string css)
        => Assert.NotNull(FindStructuralError(css));

    [Fact]
    public void FindStructuralError_AcceptsAtRulesAndNesting()
        => Assert.Null(FindStructuralError(
            "@import url('x.css');\n@media (max-width: 600px) { .a { color: red; } }\n/* .x { */ .b:hover { color: blue; }"));
}
