using System.Net;
using System.Net.Http.Json;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Confirmai.Tests;

public class PurchaseFlowIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public PurchaseFlowIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BuyerPaymentIsMarkedPaid_AfterWebhookConfirmsPayment()
    {
        const string invoiceId = "inv-flow-1";
        const string buyerId = "buyer-flow-1";
        const string sellerId = "seller-flow-1";
        const string productName = "Produto fluxo integracao";

        await SeedPaymentAsync(invoiceId, buyerId, sellerId, productName);

        var webhookResponse = await TriggerWebhookSettledAsync(invoiceId);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = db.Payments.FirstOrDefault(p => p.PaymentId == invoiceId);
        Assert.NotNull(payment);
        Assert.True(payment!.IsPaid);
    }

    [Fact]
    public async Task DuplicateWebhookSettlement_KeepsSinglePaymentPaid()
    {
        const string invoiceId = "inv-flow-dup-1";
        const string buyerId = "buyer-flow-dup";
        const string sellerId = "seller-flow-dup";
        const string productName = "Produto fluxo duplicado";

        await SeedPaymentAsync(invoiceId, buyerId, sellerId, productName);

        var firstWebhookResponse = await TriggerWebhookSettledAsync(invoiceId);
        var secondWebhookResponse = await TriggerWebhookSettledAsync(invoiceId);

        Assert.Equal(HttpStatusCode.OK, firstWebhookResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondWebhookResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payments = db.Payments.Where(p => p.PaymentId == invoiceId).ToList();
        Assert.Single(payments);
        Assert.True(payments[0].IsPaid);
    }

    private async Task<HttpResponseMessage> TriggerWebhookSettledAsync(string invoiceId)
    {
        var webhookClient = _factory.CreateClient();
        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/btcpay/webhook")
        {
            Content = JsonContent.Create(new { invoiceId, type = "InvoiceSettled" })
        };

        webhookRequest.Headers.Add("X-BTCPay-Secret", "expected-secret");
        return await webhookClient.SendAsync(webhookRequest);
    }

    private async Task SeedPaymentAsync(string invoiceId, string buyerId, string sellerId, string productName)
    {
        // Products.UserId and Payments.UserId are real FKs under Postgres.
        await _factory.EnsureUserAsync(sellerId);
        await _factory.EnsureUserAsync(buyerId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product
        {
            Name = productName,
            Description = "Descricao de fluxo",
            ShortDescription = "Resumo fluxo",
            Price = 0.001m,
            UserId = sellerId
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.Payments.Add(new PaymentRecord
        {
            ProductId = product.Id,
            UserId = buyerId,
            Address = "tb1qflowintegrationaddress",
            PaymentId = invoiceId,
            PaymentMethod = "BTCPayServer",
            Amount = 0.001m,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
