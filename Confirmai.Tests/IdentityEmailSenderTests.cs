using Confirmai.Configuration;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Payment;
using Confirmai.Services.Events;
using Confirmai.Services.User;
using Confirmai.Services.Core;
using Confirmai.Services.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

public class IdentityEmailSenderTests
{
    [Fact]
    public async Task SendEmailAsync_PersistsFallbackFiles_WhenSmtpIsDisabled()
    {
        var root = Path.Combine(Path.GetTempPath(), "Confirmai-email-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sender = CreateSender(root, new EmailOptions
            {
                Enabled = false,
                FromEmail = "noreply@test.local",
                Username = "user",
                Password = "pass",
                Host = "smtp.test.local",
                Port = 587
            });

            await sender.SendEmailAsync("to@test.local", "Subject A", "<b>Hello</b>");

            var outputDir = Path.Combine(root, "uploads", "dev-emails");
            Assert.True(Directory.Exists(outputDir));

            var htmlFiles = Directory.GetFiles(outputDir, "*.html", SearchOption.TopDirectoryOnly);
            var textFiles = Directory.GetFiles(outputDir, "*.txt", SearchOption.TopDirectoryOnly);

            Assert.NotEmpty(htmlFiles);
            Assert.NotEmpty(textFiles);

            var htmlBody = await File.ReadAllTextAsync(htmlFiles[0]);
            var txtBody = await File.ReadAllTextAsync(textFiles[0]);

            Assert.Contains("to@test.local", htmlBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Subject A", htmlBody, StringComparison.Ordinal);
            Assert.Contains("to@test.local", txtBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Subject A", txtBody, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SendEmailAsync_FallsBack_WhenSmtpConfigurationIsIncomplete()
    {
        var root = Path.Combine(Path.GetTempPath(), "Confirmai-email-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sender = CreateSender(root, new EmailOptions
            {
                Enabled = true,
                Host = "",
                FromEmail = "",
                Username = "",
                Password = "",
                Port = 0
            });

            await sender.SendEmailAsync("to@test.local", "Subject B", "Body B");

            var outputDir = Path.Combine(root, "uploads", "dev-emails");
            Assert.True(Directory.Exists(outputDir));
            Assert.NotEmpty(Directory.GetFiles(outputDir, "*.html", SearchOption.TopDirectoryOnly));
            Assert.NotEmpty(Directory.GetFiles(outputDir, "*.txt", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static IdentityEmailSender CreateSender(string webRootPath, EmailOptions options)
    {
        var environment = new TestWebHostEnvironment
        {
            WebRootPath = webRootPath,
            ContentRootPath = webRootPath
        };

        return new IdentityEmailSender(
            NullLogger<IdentityEmailSender>.Instance,
            Options.Create(options),
            environment);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Confirmai.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}


