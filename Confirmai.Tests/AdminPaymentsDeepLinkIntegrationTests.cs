using System.Net;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class AdminPaymentsDeepLinkIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AdminPaymentsDeepLinkIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminPaymentsPage_RendersStalenessLogsDeepLinkWithDefault7DayRange()
    {
        using var client = CreateAuthenticatedClient(userId: "admin-deeplink-int", userName: "admin", roles: "admin");

        var response = await client.GetAsync("/admin/payments");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-6);

        Assert.Contains("/admin/logs?eventType=payment.reconciliation.panel.stale", html, StringComparison.Ordinal);
        Assert.Contains($"startDate={startDate:yyyy-MM-dd}", html, StringComparison.Ordinal);
        Assert.Contains($"endDate={endDate:yyyy-MM-dd}", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminPaymentsPage_RendersGatewayTelemetrySection_WhenGatewaysHaveData()
    {
        // EventConfirmation.UserId and EventId are real FKs under Postgres.
        await _factory.EnsureUserAsync("gateway-user-1");
        await _factory.EnsureUserAsync("gateway-user-2");
        await _factory.EnsureUserAsync("gateway-user-3");
        var eventId = await _factory.SeedFutsalEventAsync(creatorId: "gateway-telemetry-creator");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.EventConfirmations.AddRange(
                new EventConfirmation
                {
                    EventId = eventId,
                    UserId = "gateway-user-1",
                    PixTxId = "gw-telemetry-1",
                    PaymentGatewayName = "EfiBank",
                    PaymentStatus = EventConfirmationPaymentStatus.Pending,
                    HasPaid = false,
                    ConfirmedAt = DateTime.UtcNow.AddMinutes(-45)
                },
                new EventConfirmation
                {
                    EventId = eventId,
                    UserId = "gateway-user-2",
                    PixTxId = "gw-telemetry-2",
                    PaymentGatewayName = "EfiBank",
                    PaymentStatus = EventConfirmationPaymentStatus.Paid,
                    HasPaid = true,
                    ConfirmedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new EventConfirmation
                {
                    EventId = eventId,
                    UserId = "gateway-user-3",
                    PixTxId = "gw-telemetry-3",
                    PaymentGatewayName = "AbacatePay",
                    PaymentStatus = EventConfirmationPaymentStatus.Pending,
                    HasPaid = false,
                    ConfirmedAt = DateTime.UtcNow.AddMinutes(-20)
                });

            await db.SaveChangesAsync();
        }

        using var client = CreateAuthenticatedClient(userId: "admin-gateway-telemetry", userName: "admin", roles: "admin");

        var response = await client.GetAsync("/admin/payments");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("EfiBank", html, StringComparison.Ordinal);
        Assert.Contains("AbacatePay", html, StringComparison.Ordinal);
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
