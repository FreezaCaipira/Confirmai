using Confirmai.Services.Core;
using Confirmai.Services.User;
using Confirmai.Services.Utility;
using Xunit;

namespace Confirmai.Tests;

/// <summary>
/// Tests for EmailTemplateService (C30-B Fase 5).
/// Covers: HTML envelope structure, plain-text rendering, CTA button,
/// HTML encoding of title, preheader, footer, empty CTA.
/// </summary>
public class EmailTemplateServiceTests
{
    private readonly EmailTemplateService _service = new(new UiTextService(new LanguagePreferenceService()));

    // ── HTML rendering ──

    [Fact]
    public void RenderHtml_ContainsDoctypeAndHtmlEnvelope()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, null);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html", html);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public void RenderHtml_ContainsTitleInHeadAndBody()
    {
        var html = _service.RenderHtml("Confirme seu email", null, new[] { "Hello" }, null, null);
        Assert.Contains("<title>Confirme seu email</title>", html);
        Assert.Contains("Confirme seu email", html); // also in h1
    }

    [Fact]
    public void RenderHtml_ContainsParagraphs()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Para 1", "Para 2" }, null, null);
        Assert.Contains("Para 1", html);
        Assert.Contains("Para 2", html);
    }

    [Fact]
    public void RenderHtml_ContainsCtaButtonWhenProvided()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, "Confirmar", "https://example.com/confirm");
        Assert.Contains("Confirmar", html);
        Assert.Contains("https://example.com/confirm", html);
        Assert.Contains("<a href=", html);
    }

    [Fact]
    public void RenderHtml_NoCtaButtonWhenUrlIsNull()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, "Confirmar", null);
        Assert.DoesNotContain("<a href=", html);
    }

    [Fact]
    public void RenderHtml_NoCtaButtonWhenTextIsNull()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, "https://example.com");
        Assert.DoesNotContain("<a href=", html);
    }

    [Fact]
    public void RenderHtml_ContainsPreheaderWhenProvided()
    {
        var html = _service.RenderHtml("Test", "Preview text here", new[] { "Hello" }, null, null);
        Assert.Contains("Preview text here", html);
        Assert.Contains("display:none", html); // preheader is hidden
    }

    [Fact]
    public void RenderHtml_NoPreheaderDivWhenNull()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, null);
        // No hidden div with preheader content
        Assert.DoesNotContain("display:none;max-height:0", html);
    }

    [Fact]
    public void RenderHtml_ContainsFooter()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, null);
        // Footer text from Identity.Email.Footer (PT-BR default)
        Assert.Contains("Confirma Ai", html);
    }

    [Fact]
    public void RenderHtml_UsesTableLayout()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, null);
        Assert.Contains("<table", html);
        Assert.Contains("role=\"presentation\"", html);
    }

    [Fact]
    public void RenderHtml_InlineCssNoStyleTag()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, null);
        // Email clients strip <style> tags -- CSS must be inline
        Assert.DoesNotContain("<style", html);
        Assert.Contains("style=\"", html);
    }

    [Fact]
    public void RenderHtml_HtmlEncodesTitle()
    {
        var html = _service.RenderHtml("Test <script>alert(1)</script>", null, new[] { "Hello" }, null, null);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void RenderHtml_MaxWidth600()
    {
        var html = _service.RenderHtml("Test", null, new[] { "Hello" }, null, null);
        Assert.Contains("600", html);
    }

    // ── Plain-text rendering ──

    [Fact]
    public void RenderText_ContainsTitle()
    {
        var text = _service.RenderText("Test Subject", new[] { "Hello" }, null, null);
        Assert.Contains("Test Subject", text);
    }

    [Fact]
    public void RenderText_ContainsParagraphs()
    {
        var text = _service.RenderText("Test", new[] { "Para 1", "Para 2" }, null, null);
        Assert.Contains("Para 1", text);
        Assert.Contains("Para 2", text);
    }

    [Fact]
    public void RenderText_ContainsCtaWhenProvided()
    {
        var text = _service.RenderText("Test", new[] { "Hello" }, "Confirmar", "https://example.com/confirm");
        Assert.Contains("Confirmar", text);
        Assert.Contains("https://example.com/confirm", text);
    }

    [Fact]
    public void RenderText_NoCtaWhenUrlIsNull()
    {
        var text = _service.RenderText("Test", new[] { "Hello" }, "Confirmar", null);
        Assert.DoesNotContain("https://", text);
    }

    [Fact]
    public void RenderText_StripsHtmlTags()
    {
        var text = _service.RenderText("Test", new[] { "Click <a href='x'>here</a>" }, null, null);
        Assert.Contains("Click here", text);
        Assert.DoesNotContain("<a", text);
    }

    [Fact]
    public void RenderText_ContainsFooter()
    {
        var text = _service.RenderText("Test", new[] { "Hello" }, null, null);
        Assert.Contains("Confirma Ai", text);
    }

    [Fact]
    public void RenderText_NoHtmlTags()
    {
        var text = _service.RenderText("Test", new[] { "Hello" }, "CTA", "https://example.com");
        Assert.DoesNotContain("<", text);
    }
}
