using Confirmai.Data;
using Confirmai.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

/// <summary>
/// C42-B: agregações das métricas de produto persistidas na tabela de
/// auditoria (<c>Logs</c>) — alimenta <c>/admin/metrics</c>. Não depende de
/// OTLP; consultas usam EventType + Timestamp (índice composto) e extração
/// de <c>method</c>/<c>device</c>/<c>via</c> do MetadataJson via LIKE.
/// </summary>
public sealed class AdminProductMetricsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AdminProductMetricsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public sealed record Window(int Days, int LoginGoogle, int LoginPassword,
        int InvitesOpened, int MembersJoined,
        int PageViews, int PageViewsMobile, int PageViewsDesktop,
        int ChargeableEvents, int FullyPaidEvents);

    public sealed record RouteView(string Route, int Views);

    public sealed record Result(Window Last7, Window Last30,
        IReadOnlyList<RouteView> TopRoutes30, int Registrations30,
        int TotalPageViewRows);

    public async Task<Result> LoadAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var now = DateTime.UtcNow;
        var last7 = await LoadWindowAsync(db, now.AddDays(-7), ct);
        var last30 = await LoadWindowAsync(db, now.AddDays(-30), ct);

        var topRoutes = await db.Logs
            .Where(l => l.EventType == AuditEvents.PageView && l.Timestamp >= now.AddDays(-30))
            .GroupBy(l => l.Message)
            .Select(g => new RouteView(g.Key, g.Count()))
            .OrderByDescending(r => r.Views)
            .Take(10)
            .ToListAsync(ct);

        var registrations30 = await db.Logs
            .CountAsync(l => l.EventType == AuditEvents.UserRegistered && l.Timestamp >= now.AddDays(-30), ct);

        var totalPageViews = await db.Logs
            .CountAsync(l => l.EventType == AuditEvents.PageView, ct);

        return new Result(last7, last30, topRoutes, registrations30, totalPageViews);
    }

    private static async Task<Window> LoadWindowAsync(AppDbContext db, DateTime cutoff, CancellationToken ct)
    {
        var logs = db.Logs.Where(l => l.Timestamp >= cutoff && l.EventType != null);

        var loginGoogle = await CountMetadataAsync(logs, AuditEvents.UserLoginSuccess, "\"method\":\"google\"", ct);
        var loginPassword = await CountMetadataAsync(logs, AuditEvents.UserLoginSuccess, "\"method\":\"password\"", ct);
        var invitesOpened = await logs.CountAsync(l => l.EventType == AuditEvents.GroupInviteOpened, ct);
        var membersJoined = await logs.CountAsync(l => l.EventType == AuditEvents.GroupMemberJoined, ct);
        var pageViews = await logs.CountAsync(l => l.EventType == AuditEvents.PageView, ct);
        var pageViewsMobile = await CountMetadataAsync(logs, AuditEvents.PageView, "\"device\":\"mobile\"", ct);
        var pageViewsDesktop = await CountMetadataAsync(logs, AuditEvents.PageView, "\"device\":\"desktop\"", ct);

        // H3: partidas com cobranca (Price > 0) ja iniciadas na janela, onde
        // toda confirmacao esta paga.
        var chargeable = await db.Events
            .Where(e => e.StartsAt >= cutoff && e.StartsAt <= DateTime.UtcNow
                && e.IsActive && e.Price != null && e.Price > 0
                && !e.Confirmations.Any(c => c.User!.IsMock))
            .Select(e => new { e.Id, Unpaid = e.Confirmations.Count(c => !c.HasPaid) })
            .ToListAsync(ct);

        return new Window(
            Days: (int)(DateTime.UtcNow - cutoff).TotalDays,
            LoginGoogle: loginGoogle,
            LoginPassword: loginPassword,
            InvitesOpened: invitesOpened,
            MembersJoined: membersJoined,
            PageViews: pageViews,
            PageViewsMobile: pageViewsMobile,
            PageViewsDesktop: pageViewsDesktop,
            ChargeableEvents: chargeable.Count,
            FullyPaidEvents: chargeable.Count(e => e.Unpaid == 0));
    }

    private static Task<int> CountMetadataAsync(
        IQueryable<Confirmai.Models.AppLog> logs, string eventType, string metadataFragment, CancellationToken ct) =>
        logs.CountAsync(l => l.EventType == eventType
            && l.MetadataJson != null
            && EF.Functions.Like(l.MetadataJson, $"%{metadataFragment}%"), ct);
}
