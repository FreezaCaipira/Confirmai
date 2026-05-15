using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

public class DashboardMetricsSnapshot
{
    public int UsersCount { get; set; }
    public int QuoteQueriesCount { get; set; }
}

public class DashboardMetricsService
{
    private readonly AppDbContext _db;

    public DashboardMetricsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardMetricsSnapshot> GetSnapshotAsync()
    {
        var usersCount = await _db.Users.CountAsync();

        var quoteQueriesCount = await _db.Logs.CountAsync(log =>
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

