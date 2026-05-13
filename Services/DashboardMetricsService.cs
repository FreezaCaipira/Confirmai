using Confirmai.Data;
using Confirmai.Enums;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

public class DashboardMetricsSnapshot
{
    public int UsersCount { get; set; }
    public int PaidSalesCount { get; set; }
    public decimal PaidSalesVolumeBtc { get; set; }
    public int PendingOrdersCount { get; set; }
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

        var paidOrdersQuery = _db.Orders.Where(order => order.IsPaid);
        var paidSalesCount = await paidOrdersQuery.CountAsync();
        var paidSalesVolumeBtc = await paidOrdersQuery.SumAsync(order => (decimal?)order.Amount) ?? 0m;

        var pendingOrdersCount = await _db.Orders.CountAsync(order =>
            !order.FundsReleased &&
            (order.Status == PaymentStatus.AguardandoPagamento ||
             order.Status == PaymentStatus.AguardandoEntrega ||
             order.Status == PaymentStatus.AguardandoEntregaInGame ||
             order.Status == PaymentStatus.AguardandoRevisaoAdm ||
             order.Status == PaymentStatus.Pendente));

        var quoteQueriesCount = await _db.Logs.CountAsync(log =>
            log.Source == "QuoteQuery" ||
            log.Source == "Quote" ||
            log.Source == "CryptoQuote" ||
            log.Source == "BitcoinQuote");

        return new DashboardMetricsSnapshot
        {
            UsersCount = usersCount,
            PaidSalesCount = paidSalesCount,
            PaidSalesVolumeBtc = paidSalesVolumeBtc,
            PendingOrdersCount = pendingOrdersCount,
            QuoteQueriesCount = quoteQueriesCount
        };
    }
}

