using System.Security.Cryptography.X509Certificates;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Confirmai.Services.Core;

/// <summary>
/// Monitora expiração de certificados mTLS (EfiBank Pix, etc.).
/// Emite alertas 30, 14 e 7 dias antes da expiração.
/// </summary>
public class CertificateHealthCheckService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly ILogger<CertificateHealthCheckService> _logger;
    private Timer _timer;

    public CertificateHealthCheckService(
        IConfiguration config,
        ILogger<CertificateHealthCheckService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CertificateHealthCheckService iniciado");

        // Verifica a cada 12h (começa após 1min)
        _timer = new Timer(
            async _ => await CheckCertificatesAsync(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromHours(12)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CertificateHealthCheckService parado");
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task CheckCertificatesAsync()
    {
        try
        {
            // Verificar certificado EfiBank mTLS
            await CheckEfiBankCertificateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar certificados");
        }
    }

    private async Task CheckEfiBankCertificateAsync()
    {
        var certPath = _config["EfiBank:ClientCertificatePath"];
        if (string.IsNullOrEmpty(certPath))
        {
            _logger.LogWarning("EfiBank:ClientCertificatePath não configurado");
            return;
        }

        try
        {
            if (!File.Exists(certPath))
            {
                _logger.LogError("❌ Certificado EfiBank não encontrado em {Path}", certPath);
                return;
            }

            var password = _config["EfiBank:CertificatePassword"];
            var cert = new X509Certificate2(certPath, password ?? string.Empty);

            var now = DateTime.Now;
            var expiryDate = cert.NotAfter;
            var daysUntilExpiry = (expiryDate - now).Days;

            // Determinar nível de alerta
            var alertLevel = daysUntilExpiry switch
            {
                <= 0 => ("🔴 EXPIRADO", "critical"),
                <= 7 => ("🔴 CRÍTICO", "critical"),
                <= 14 => ("🟠 URGENTE", "urgent"),
                <= 30 => ("🟡 AVISO", "warning"),
                _ => (null, null)
            };

            if (alertLevel.Item1 != null)
            {
                _logger.LogWarning(
                    "{AlertLevel} Certificado EfiBank expira em {Days} dias ({ExpiryDate:dd/MM/yyyy}). " +
                    "Subject: {Subject}",
                    alertLevel.Item1,
                    daysUntilExpiry,
                    expiryDate,
                    cert.Subject
                );
            }
            else
            {
                _logger.LogInformation(
                    "✅ Certificado EfiBank válido por {Days} dias ({ExpiryDate:dd/MM/yyyy})",
                    daysUntilExpiry,
                    expiryDate
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar certificado EfiBank");
        }
    }
}
