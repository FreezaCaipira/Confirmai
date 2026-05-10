using Microsoft.EntityFrameworkCore;
using Confirmai.Data;

namespace Confirmai.Services
{
    /// <summary>
    /// Background service that purges used or expired GameLoginTokens once per day
    /// to prevent the table from growing unbounded.
    /// </summary>
    public class GameLoginTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GameLoginTokenCleanupService> _logger;

        public GameLoginTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<GameLoginTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once at startup, then every 24 h.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao limpar GameLoginTokens expirados.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CleanupAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTime.UtcNow;
            var tokens = await db.GameLoginTokens
                .Where(t => t.IsUsed || t.ExpiresAtUtc < cutoff)
                .ToListAsync(ct);

            if (tokens.Count > 0)
            {
                db.GameLoginTokens.RemoveRange(tokens);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("GameLoginTokenCleanup: {Count} token(s) removido(s).", tokens.Count);
            }
        }
    }
}
