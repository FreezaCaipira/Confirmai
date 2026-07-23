using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

public sealed record AdminPaymentsPageResult(
    int TotalCount,
    int TotalPages,
    List<PaymentRecord> Payments);

public sealed class AdminPaymentsQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AdminLogsExportService _exportService;

    public AdminPaymentsQueryService(
        IDbContextFactory<AppDbContext> dbFactory,
        AdminLogsExportService exportService)
    {
        _dbFactory = dbFactory;
        _exportService = exportService;
    }

    public async Task<AdminPaymentsPageResult> GetPaymentsPageAsync(
        string filterUserId,
        decimal? filterMinAmount,
        decimal? filterMaxAmount,
        string filterStatus,
        DateTime? filterDate,
        int currentPage,
        int pageSize)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterUserId))
            query = query.Where(p => (p.User != null && p.User.UserName != null && p.User.UserName.Contains(filterUserId)) || (p.UserId != null && p.UserId.Contains(filterUserId)));
        if (filterMinAmount.HasValue)
            query = query.Where(p => p.Amount >= filterMinAmount.Value);
        if (filterMaxAmount.HasValue)
            query = query.Where(p => p.Amount <= filterMaxAmount.Value);
        if (!string.IsNullOrWhiteSpace(filterStatus))
            query = filterStatus == "paid" ? query.Where(p => p.IsPaid) : query.Where(p => !p.IsPaid);
        if (filterDate.HasValue)
            query = query.Where(p => p.CreatedAt.Date == filterDate.Value.Date);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        var page = Math.Clamp(currentPage, 1, totalPages);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AdminPaymentsPageResult(totalCount, totalPages, payments);
    }

    public async Task<(string Csv, string FileName)> BuildReconciliationExportAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rawRows = await db.EventConfirmations
            .AsNoTracking()
            .Where(c => c.PixTxId != null || c.PaymentStatus != Enums.EventConfirmationPaymentStatus.Pending)
            .OrderByDescending(c => c.ConfirmedAt)
            .Select(c => new
            {
                c.Id,
                c.EventId,
                c.UserId,
                c.ConfirmedAt,
                c.PaymentStatus,
                c.PixTxId,
                c.PaymentGatewayName
            })
            .ToListAsync();

        var rows = rawRows.Select(c => new EventConfirmationReconciliationExportRow(
            c.Id,
            c.EventId,
            c.UserId!,
            c.ConfirmedAt,
            c.PaymentStatus.ToString(),
            c.PixTxId,
            c.PaymentGatewayName));

        var csv = _exportService.BuildEventConfirmationReconciliationCsv(rows);
        var fileName = _exportService.BuildEventConfirmationReconciliationFileName();

        return (csv, fileName);
    }
}
