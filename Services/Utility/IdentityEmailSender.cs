using Confirmai.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Confirmai.Services.Utility;

public class IdentityEmailSender : IEmailSender
{
    private readonly ILogger<IdentityEmailSender> _logger;
    private readonly EmailOptions _options;
    private readonly IWebHostEnvironment _environment;

    public IdentityEmailSender(
        ILogger<IdentityEmailSender> logger,
        IOptions<EmailOptions> options,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _options = options.Value;
        _environment = environment;
    }

    /// <summary>
    /// True when running in the Development environment. Exposed so other
    /// components (e.g. ForgotPasswordConfirmation) can check without
    /// injecting IWebHostEnvironment separately.
    /// </summary>
    public bool IsDevelopment => _environment.IsDevelopment();

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await SendEmailAsync(email, subject, htmlMessage, textMessage: null);
    }

    /// <summary>
    /// Sends an email with both HTML and plain-text alternatives.
    /// The text alternative (AlternateViews) ensures spam filters and
    /// accessibility-first clients can read the message.
    /// </summary>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage, string? textMessage)
    {
        if (!IsSmtpEnabled())
        {
            await PersistFallbackEmailAsync(email, subject, htmlMessage, textMessage);
            _logger.LogInformation("Identity email (fallback log) -> To: {Email} | Subject: {Subject} | Body: {Body}", email, subject, htmlMessage);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject
        };
        message.To.Add(email);

        if (!string.IsNullOrWhiteSpace(textMessage))
        {
            // Plain-text alternate view (preferred by spam filters + a11y clients)
            var textView = AlternateView.CreateAlternateViewFromString(
                textMessage,
                Encoding.UTF8,
                "text/plain");
            message.AlternateViews.Add(textView);

            var htmlView = AlternateView.CreateAlternateViewFromString(
                htmlMessage,
                Encoding.UTF8,
                "text/html");
            message.AlternateViews.Add(htmlView);
        }
        else
        {
            message.Body = htmlMessage;
            message.IsBodyHtml = true;
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(message);
        _logger.LogInformation("Identity email sent via SMTP to {Email}.", email);
    }

    private bool IsSmtpEnabled()
    {
        return _options.Enabled
            && !string.IsNullOrWhiteSpace(_options.Host)
            && !string.IsNullOrWhiteSpace(_options.FromEmail)
            && !string.IsNullOrWhiteSpace(_options.Username)
            && !string.IsNullOrWhiteSpace(_options.Password)
            && _options.Port > 0;
    }

    /// <summary>
    /// Fallback emails carry live confirmation and password-reset links, so
    /// outside Development they must never land under the web root, which is
    /// served publicly by the static files middleware.
    /// </summary>
    public string GetFallbackDirectory()
    {
        if (_environment.IsDevelopment())
        {
            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? AppContext.BaseDirectory
                : _environment.WebRootPath;

            return Path.Combine(webRoot, "uploads", "dev-emails");
        }

        var contentRoot = string.IsNullOrWhiteSpace(_environment.ContentRootPath)
            ? AppContext.BaseDirectory
            : _environment.ContentRootPath;

        return Path.Combine(contentRoot, "App_Data", "fallback-emails");
    }

    private async Task PersistFallbackEmailAsync(string email, string subject, string htmlMessage, string? textMessage)
    {
        try
        {
            var fallbackDirectory = GetFallbackDirectory();
            Directory.CreateDirectory(fallbackDirectory);

            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.html";
            var filePath = Path.Combine(fallbackDirectory, fileName);
            var textFilePath = Path.Combine(fallbackDirectory, Path.GetFileNameWithoutExtension(fileName) + ".txt");

            var body = new StringBuilder();
            body.AppendLine("<html><body style='font-family:Segoe UI,Arial,sans-serif'>");
            body.AppendLine("<h2>Identity fallback email</h2>");
            body.AppendLine($"<p><b>To:</b> {WebUtility.HtmlEncode(email)}</p>");
            body.AppendLine($"<p><b>Subject:</b> {WebUtility.HtmlEncode(subject)}</p>");
            body.AppendLine($"<p><b>Generated (UTC):</b> {DateTime.UtcNow:O}</p>");
            body.AppendLine("<hr />");
            body.AppendLine(htmlMessage);
            body.AppendLine("</body></html>");

            await File.WriteAllTextAsync(filePath, body.ToString(), Encoding.UTF8);
            var textBody = new StringBuilder();
            textBody.AppendLine("Identity fallback email");
            textBody.AppendLine($"To: {email}");
            textBody.AppendLine($"Subject: {subject}");
            textBody.AppendLine($"Generated (UTC): {DateTime.UtcNow:O}");
            textBody.AppendLine("----------------------------------------");
            textBody.AppendLine(textMessage ?? htmlMessage);
            await File.WriteAllTextAsync(textFilePath, textBody.ToString(), Encoding.UTF8);
            _logger.LogInformation("Identity fallback email persisted at {FallbackPath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist fallback identity email.");
        }
    }
}
