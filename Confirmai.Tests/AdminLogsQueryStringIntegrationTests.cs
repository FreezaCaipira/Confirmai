using System.Net;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class AdminLogsQueryStringIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminLogsQueryStringIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminLogsPage_QueryStringFilters_AreAppliedOnInitialRender()
    {
        var marker = Guid.NewGuid().ToString("N");
        var includedMessage = $"stale-in-range-{marker}";
        var excludedByDateMessage = $"stale-out-of-range-{marker}";
        var excludedByEventMessage = $"other-event-{marker}";

        await SeedLogAsync(includedMessage, AuditEvents.PaymentReconciliationPanelStale, new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc));
        await SeedLogAsync(excludedByDateMessage, AuditEvents.PaymentReconciliationPanelStale, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc));
        await SeedLogAsync(excludedByEventMessage, AuditEvents.PaymentConfirmed, new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc));

        using var client = CreateAuthenticatedClient(userId: "admin-logs-query-int", userName: "admin", roles: "admin");

        var response = await client.GetAsync("/admin/logs?eventType=payment.reconciliation.panel.stale&startDate=2026-05-22&endDate=2026-05-28");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(includedMessage, html, StringComparison.Ordinal);
        Assert.DoesNotContain(excludedByDateMessage, html, StringComparison.Ordinal);
        Assert.DoesNotContain(excludedByEventMessage, html, StringComparison.Ordinal);
    }

    private async Task SeedLogAsync(string message, string eventType, DateTime timestampUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Logs.Add(new AppLog
        {
            Source = AdminAuditSources.Payments,
            Message = message,
            Level = "Warning",
            EventType = eventType,
            EntityType = AuditEntities.Payment,
            Timestamp = timestampUtc
        });

        await db.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(string userId, string userName, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", userName);

        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(',', roles));

        return client;
    }
}
