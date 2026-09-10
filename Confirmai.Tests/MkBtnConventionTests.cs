using System.Text.RegularExpressions;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Convention test (C30-C Fase 3): ensures the .mk-btn base selector is
/// defined only once — in buttons.css (global). Scoped .razor.css files may
/// override individual properties (min-height, border-radius, etc.) but
/// MUST NOT redefine the full base (display, align-items, justify-content,
/// gap, padding, border, background, color, font-size, font-weight, cursor,
/// text-decoration, transition — all at once).
/// </summary>
public class MkBtnConventionTests
{
    private static readonly Regex MkBtnBaseRegex = new(
        @"\.mk-btn\s*\{[^}]*display:\s*inline-flex",
        RegexOptions.Compiled | RegexOptions.Singleline);

    [Fact]
    public void MkBtnBase_IsDefinedOnlyInButtonsCss_NotInScopedCss()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.csproj").Any() && Directory.Exists(Path.Combine(dir.FullName, "Pages")))
                break;
            dir = dir.Parent;
        }
        Assert.NotNull(dir);

        // Verify buttons.css has the base definition.
        var buttonsCss = Path.Combine(dir.FullName, "wwwroot", "css", "buttons.css");
        Assert.True(File.Exists(buttonsCss), "buttons.css not found");
        var buttonsContent = File.ReadAllText(buttonsCss);
        Assert.True(MkBtnBaseRegex.IsMatch(buttonsContent),
            ".mk-btn base (display: inline-flex) must be defined in buttons.css");

        // Scan all .razor.css files for full base redefinitions.
        var violations = new List<string>();
        var razorCssFiles = Directory.GetFiles(dir.FullName, "*.razor.css", SearchOption.AllDirectories);

        foreach (var file in razorCssFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = file.Replace('\\', '/')[..dir.FullName.Length].TrimStart('/');

            // Check for full base redefinition (contains display: inline-flex
            // inside a .mk-btn { } block).
            if (MkBtnBaseRegex.IsMatch(content))
            {
                violations.Add(relativePath);
            }
        }

        Assert.True(violations.Count == 0,
            ".mk-btn base (display: inline-flex) must not be redefined in scoped CSS. " +
            $"Found in {violations.Count} file(s):\n" +
            string.Join("\n", violations));
    }
}
