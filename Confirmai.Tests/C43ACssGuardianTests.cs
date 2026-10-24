namespace Confirmai.Tests;

/// <summary>C43-A: guardioes CSS — nav-cta, seg-item ativo, btn-whatsapp, invite-field, my-conf-actions.</summary>

public class C43ACssGuardianTests
{
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

    private static string Css(string rel) =>
        File.ReadAllText(Path.Combine(RepoRoot().FullName, rel.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void ShellNavCta_ComesAfterGeneralLinkRule_AndDeclaresAccentFg()
    {
        var css = Css("wwwroot/css/shell.css");
        var general = css.IndexOf("body .oldsite-top-nav .oldsite-top-nav-link", StringComparison.Ordinal);
        var cta = css.IndexOf("body .oldsite-top-nav .shell-nav-cta {", StringComparison.Ordinal);
        Assert.True(general >= 0 && cta > general, "CTA rule must come after the general nav-link rule");
        var block = css.Substring(cta, css.IndexOf('}', cta) - cta);
        Assert.Contains("--accent-fg", block);
    }

    [Fact]
    public void SegItemActive_IsFilledAccent_NotSoft()
    {
        var css = Css("wwwroot/css/components.css");
        var i = css.IndexOf(".seg-item--active {", StringComparison.Ordinal);
        Assert.True(i >= 0);
        var block = css.Substring(i, css.IndexOf('}', i) - i);
        Assert.Contains("background: var(--accent);", block);
        Assert.Contains("color: var(--accent-fg);", block);
        Assert.DoesNotContain("accent-soft", block);
    }

    [Fact]
    public void WhatsappButton_UsesBrandTokens_WithDarkText()
    {
        var css = Css("wwwroot/css/components.css");
        var i = css.IndexOf(".btn-whatsapp {", StringComparison.Ordinal);
        Assert.True(i >= 0);
        var block = css.Substring(i, css.IndexOf('}', i) - i);
        Assert.Contains("background: var(--whatsapp);", block);
        Assert.Contains("color: var(--whatsapp-fg);", block);
    }

    [Fact]
    public void InviteField_IsGrid_CopyButtonCannotOverlap()
    {
        var css = Css("wwwroot/css/events.css");
        var i = css.IndexOf(".invite-field {", StringComparison.Ordinal);
        Assert.True(i >= 0);
        var block = css.Substring(i, css.IndexOf('}', i) - i);
        Assert.Contains("grid-template-columns", block);
        Assert.Contains("minmax(0,1fr) auto", block);
        Assert.Contains("min-height: 44px", block);
    }

    [Fact]
    public void MyConfActions_DoesNotStretchButtons()
    {
        var css = Css("Shared/Components/ConfirmationCard.razor.css");
        var i = css.IndexOf(".my-conf-actions {", StringComparison.Ordinal);
        Assert.True(i >= 0);
        var block = css.Substring(i, css.IndexOf('}', i) - i);
        Assert.Contains("align-items: flex-end", block);
    }
}
