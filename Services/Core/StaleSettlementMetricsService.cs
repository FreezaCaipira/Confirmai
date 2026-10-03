using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Core;

/// <summary>
/// C32 Fase C — periodically counts platform-fee settlements stuck in
/// EmAnalise longer than Ops:StaleSettlementDays (default 3) and publishes
/// the count to the confirmai_fee_settlements_pending_stale gauge.
/// </summary>
public sealed class StaleSettlementMetricsService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OperationalMetrics _metrics;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StaleSettlementMetricsService> _logger;

    public StaleSettlementMetricsService(
        IServiceScopeFactory scopeFactory,
        OperationalMetrics metrics,
        IConfiguration configuration,
        ILogger<StaleSettlementMetricsService> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StaleSettlementMetricsService: erro ao contar repasses parados.");
            }

            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }

    /// <summary>Public so tests can drive a single refresh deterministically.</summary>
    public async Task RefreshOnceAsync(CancellationToken ct = default)
    {
        var staleDays = _configuration.GetValue("Ops:StaleSettlementDays", 3);
        var cutoff = DateTime.UtcNow.AddDays(-staleDays);

        using var scope = _scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync(ct);
        var count = await db.PlatformFeeSettlements
            .CountAsync(s => s.Status == PlatformFeeSettlementStatus.EmAnalise
                && s.SubmittedAt < cutoff, ct);

        _metrics.RecordStaleFeeSettlements(count);
    }
}
