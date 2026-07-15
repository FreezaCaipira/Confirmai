using System.Net;
using System.Net.Http.Json;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Confirmai.Tests;

public class EventConfirmationPaymentWebhookFlowIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public EventConfirmationPaymentWebhookFlowIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EventConfirmationFlow_ConfirmSlotThenWebhook_MarksConfirmationPaid()
    {
        var txId = $"tx-e2e-{Guid.NewGuid():N}"[..20];
        await SeedEventConfirmationAsync(txId, hasPaid: false);

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(BuildEfiWebhookPath(), new
        {
            pix = new[]
            {
                new
                {
                    txid = txId,
                    valor = "25.00",
                    horario = DateTime.UtcNow.ToString("O")
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var confirmation = await db.EventConfirmations.SingleAsync(c => c.PixTxId == txId);

        Assert.True(confirmation.HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, confirmation.PaymentStatus);
        Assert.Equal("EfiBank", confirmation.PaymentGatewayName);
    }

    [Fact]
    public async Task EventConfirmationFlow_WebhookReplay_RemainsIdempotent()
    {
        var txId = $"tx-e2e-replay-{Guid.NewGuid():N}"[..28];
        await SeedEventConfirmationAsync(txId, hasPaid: false);

        using var client = _factory.CreateClient();
        var payload = new
        {
            pix = new[]
            {
                new
                {
                    txid = txId,
                    valor = "25.00",
                    horario = DateTime.UtcNow.ToString("O")
                }
            }
        };

        var first = await client.PostAsJsonAsync(BuildEfiWebhookPath(), payload);
        var second = await client.PostAsJsonAsync(BuildEfiWebhookPath(), payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var confirmations = await db.EventConfirmations.Where(c => c.PixTxId == txId).ToListAsync();

        Assert.Single(confirmations);
        Assert.True(confirmations[0].HasPaid);
        Assert.Equal(EventConfirmationPaymentStatus.Paid, confirmations[0].PaymentStatus);
        Assert.Equal("EfiBank", confirmations[0].PaymentGatewayName);
    }

    private async Task SeedEventConfirmationAsync(string txId, bool hasPaid)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.EventConfirmations.Add(new EventConfirmation
        {
            EventId = 1,
            UserId = "user-e2e-flow",
            PixTxId = txId,
            HasPaid = hasPaid,
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-5)
        });

        await db.SaveChangesAsync();
    }

    private string BuildEfiWebhookPath()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<EfiBankOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return "/api/webhooks/efibank/pix";
        }

        return $"/api/webhooks/efibank/pix?webhookSecret={Uri.EscapeDataString(options.WebhookSecret)}";
    }
}
