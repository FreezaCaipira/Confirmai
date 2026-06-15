using Microsoft.EntityFrameworkCore;
using Confirmai.Data;

namespace Confirmai.Services.Core;

/// <summary>
/// Daily background service enforcing log retention policy:
///   - Anonymizes IpAddress for logs older than 30 days (GDPR / PII hygiene).
///   - Purges non-financial operational logs older than 90 days.
///   Financial events (payment.* / order.*) are never purged automatically.
/// </summary>
public class LogRetentionService : BackgroundService
{
    // Events that must NEVER be purged by the retention policy (financial audit trail).
    private static readonly string[] FinancialPrefixes = ["payment.", "order."];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogRetentionService> _logger;

    public LogRetentionService(
        IServiceScopeFactory scopeFactory,
        ILogger<LogRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup slightly so it doesn't compete with app boot queries.
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogRetentionService: erro durante execução da política de retenção.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var ipCutoff = now.AddDays(-30);
        var purgeCutoff = now.AddDays(-90);

        // 1. Anonymize IpAddress for logs older than 30 days.
        var toAnonymize = await db.Logs
            .Where(l => l.Timestamp < ipCutoff && l.IpAddress != null)
            .ToListAsync(ct);

        foreach (var log in toAnonymize)
            log.IpAddress = null;

        // 2. Purge non-financial operational logs older than 90 days.
        //    Keep any log whose EventType starts with "payment." or "order.".
        var toPurge = await db.Logs
            .Where(l => l.Timestamp < purgeCutoff
                && (l.EventType == null
                    || (!l.EventType.StartsWith("payment.") && !l.EventType.StartsWith("order."))))
            .ToListAsync(ct);

        db.Logs.RemoveRange(toPurge);

        await db.SaveChangesAsync(ct);

        if (toAnonymize.Count > 0)
            _logger.LogInformation("LogRetentionService: {Count} endereço(s) IP anonimizado(s) (> 30 dias).", toAnonymize.Count);

        if (toPurge.Count > 0)
            _logger.LogInformation("LogRetentionService: {Count} log(s) operacional(is) removido(s) (> 90 dias).", toPurge.Count);
    }
}
