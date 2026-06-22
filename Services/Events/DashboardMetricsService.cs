using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Events;

public class DashboardMetricsSnapshot
{
    public int UsersCount { get; set; }
    public int QuoteQueriesCount { get; set; }
}

public class DashboardMetricsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DashboardMetricsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardMetricsSnapshot> GetSnapshotAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        var usersCount = await db.Users.CountAsync();

        var quoteQueriesCount = await db.Logs.CountAsync(log =>
            log.Source == "QuoteQuery" ||
            log.Source == "Quote" ||
            log.Source == "CryptoQuote" ||
            log.Source == "BitcoinQuote");

        return new DashboardMetricsSnapshot
        {
            UsersCount = usersCount,
            QuoteQueriesCount = quoteQueriesCount
        };
    }
}

