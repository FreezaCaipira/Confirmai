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

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (!IsSmtpEnabled())
        {
            await PersistFallbackEmailAsync(email, subject, htmlMessage);
            _logger.LogInformation("Identity email (fallback log) -> To: {Email} | Subject: {Subject} | Body: {Body}", email, subject, htmlMessage);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(email);

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

    private async Task PersistFallbackEmailAsync(string email, string subject, string htmlMessage)
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
            textBody.AppendLine(htmlMessage);
            await File.WriteAllTextAsync(textFilePath, textBody.ToString(), Encoding.UTF8);
            _logger.LogInformation("Identity fallback email persisted at {FallbackPath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist fallback identity email.");
        }
    }
}
