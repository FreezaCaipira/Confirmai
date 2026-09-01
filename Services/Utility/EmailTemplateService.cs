using System.Text;
using System.Web;
using Confirmai.Services.Core;

namespace Confirmai.Services.Utility;

/// <summary>
/// Renders transactional Identity emails (confirm email, reset password) as
/// a complete HTML envelope with inline CSS (email clients strip &lt;style&gt;)
/// and a plain-text alternative for AlternateViews.
///
/// Extracted in C30-B Fase 5 so the template is testable without SMTP.
/// </summary>
public sealed class EmailTemplateService
{
    private readonly UiTextService _t;

    public EmailTemplateService(UiTextService t)
    {
        _t = t;
    }

    /// <summary>
    /// Renders a transactional email as a full HTML document.
    /// All CSS is inline (email clients strip &lt;style&gt; and &lt;link&gt;).
    /// Uses &lt;table&gt; for layout (Outlook doesn't support flex/grid).
    /// </summary>
    public string RenderHtml(string title, string? preheader, IEnumerable<string> paragraphs, string? ctaText, string? ctaUrl)
    {
        var sb = new StringBuilder(2048);

        // ── Outer shell: 100% width background, centered 600px table ──
        sb.Append("<!DOCTYPE html><html lang=\"pt-br\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.Append("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">");
        sb.Append("<title>").Append(HttpUtility.HtmlEncode(title)).Append("</title>");
        sb.Append("</head><body style=\"margin:0;padding:0;background:#0d1825;font-family:Segoe UI,Arial,sans-serif;color:#f1f5f9;\">");

        // Preheader (hidden preview text shown by Gmail/Outlook)
        if (!string.IsNullOrWhiteSpace(preheader))
        {
            sb.Append("<div style=\"display:none;max-height:0;overflow:hidden;opacity:0;\">");
            sb.Append(HttpUtility.HtmlEncode(preheader));
            sb.Append("</div>");
        }

        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#0d1825;\">");
        sb.Append("<tr><td align=\"center\" style=\"padding:24px 12px;\">");

        // ── 600px card ──
        sb.Append("<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;width:100%;background:#111927;border:1px solid #1b3d6c;border-radius:8px;\">");

        // Header band
        sb.Append("<tr><td style=\"padding:24px 32px 16px;border-bottom:1px solid #1b3d6c;\">");
        sb.Append("<h1 style=\"margin:0;font-size:20px;font-weight:700;color:#f1f5f9;\">");
        sb.Append(HttpUtility.HtmlEncode(title));
        sb.Append("</h1></td></tr>");

        // Body paragraphs
        sb.Append("<tr><td style=\"padding:24px 32px;\">");
        foreach (var p in paragraphs)
        {
            sb.Append("<p style=\"margin:0 0 16px;font-size:15px;line-height:1.6;color:#deeeff;\">");
            sb.Append(p); // paragraphs may contain inline HTML (e.g. links)
            sb.Append("</p>");
        }

        // CTA button
        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:8px 0 16px;\">");
            sb.Append("<tr><td style=\"border-radius:6px;background:#4f9cf8;\">");
            sb.Append("<a href=\"").Append(HttpUtility.HtmlAttributeEncode(ctaUrl)).Append("\" ");
            sb.Append("style=\"display:inline-block;padding:12px 28px;font-size:15px;font-weight:600;");
            sb.Append("color:#ffffff;text-decoration:none;border-radius:6px;background:#4f9cf8;\">");
            sb.Append(HttpUtility.HtmlEncode(ctaText));
            sb.Append("</a></td></tr></table>");

            // Fallback link (image-blocking clients)
            sb.Append("<p style=\"margin:0 0 16px;font-size:13px;color:#8aacc8;\">");
            sb.Append(HttpUtility.HtmlEncode(ctaText)).Append(": <a href=\"");
            sb.Append(HttpUtility.HtmlAttributeEncode(ctaUrl)).Append("\" style=\"color:#7ab6ff;word-break:break-all;\">");
            sb.Append(HttpUtility.HtmlEncode(ctaUrl)).Append("</a></p>");
        }

        sb.Append("</td></tr>");

        // Footer
        sb.Append("<tr><td style=\"padding:16px 32px 24px;border-top:1px solid #1b3d6c;\">");
        sb.Append("<p style=\"margin:0;font-size:12px;color:#6082a0;line-height:1.5;\">");
        sb.Append(HttpUtility.HtmlEncode(_t["Identity.Email.Footer"])).Append("</p>");
        sb.Append("</td></tr>");

        sb.Append("</table></td></tr></table></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders a plain-text version of the email for AlternateViews.
    /// Strips all HTML tags, keeps the link URLs visible.
    /// </summary>
    public string RenderText(string title, IEnumerable<string> paragraphs, string? ctaText, string? ctaUrl)
    {
        var sb = new StringBuilder(512);
        sb.AppendLine(title);
        sb.AppendLine(new string('=', Math.Min(title.Length, 60)));
        sb.AppendLine();

        foreach (var p in paragraphs)
        {
            // Strip HTML tags for plain text
            var text = StripHtml(p);
            sb.AppendLine(text);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            sb.AppendLine(ctaText);
            sb.AppendLine(ctaUrl);
        }

        sb.AppendLine();
        sb.AppendLine(_t["Identity.Email.Footer"]);
        return sb.ToString();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var sb = new StringBuilder(html.Length);
        var inside = false;
        foreach (var c in html)
        {
            if (c == '<') { inside = true; continue; }
            if (c == '>') { inside = false; continue; }
            if (!inside) sb.Append(c);
        }
        return sb.ToString().Trim();
    }
}
