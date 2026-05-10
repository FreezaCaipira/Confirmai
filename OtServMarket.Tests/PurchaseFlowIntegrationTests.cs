using System.Net;
using System.Net.Http.Json;
using Confirmai.Data;
using Confirmai.Enums;
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
    public async Task BuyerCanOpenOrderDetails_AfterWebhookConfirmsPayment()
    {
        const string invoiceId = "inv-flow-1";
        const string buyerId = "buyer-flow-1";
        const string sellerId = "seller-flow-1";
        const string productName = "Produto fluxo integracao";

        await SeedPaymentAsync(invoiceId, buyerId, sellerId, productName);

        var webhookResponse = await TriggerWebhookSettledAsync(invoiceId);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var snapshot = LoadSettledSnapshot(invoiceId);
        Assert.Equal(buyerId, snapshot.BuyerId);
        Assert.Equal(productName, snapshot.ProductName);

        var buyerClient = CreateAuthenticatedClient(buyerId, "buyerflow");

        var detailsResponse = await buyerClient.GetAsync($"/orders/{snapshot.OrderId}");
        _ = await detailsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        Assert.True(snapshot.ProductId > 0);
    }

    [Fact]
    public async Task DuplicateWebhookSettlement_KeepsSingleOrder_AndBuyerCanStillOpenDetails()
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

        var snapshot = LoadSettledSnapshot(invoiceId, requireSingleOrder: true);
        Assert.Equal(buyerId, snapshot.BuyerId);
        Assert.Equal(sellerId, snapshot.SellerId);

        var buyerClient = CreateAuthenticatedClient(buyerId, "buyerdup");
        var detailsResponse = await buyerClient.GetAsync($"/orders/{snapshot.OrderId}");
        _ = await detailsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
    }

    [Fact]
    public async Task IntruderCannotOpenOrderDetails_AfterWebhookCreatesOrder()
    {
        const string invoiceId = "inv-flow-intruder-1";
        const string buyerId = "buyer-flow-intruder";
        const string sellerId = "seller-flow-intruder";
        const string intruderId = "intruder-flow-user";
        const string productName = "Produto fluxo acesso negado";

        await SeedPaymentAsync(invoiceId, buyerId, sellerId, productName);

        var webhookResponse = await TriggerWebhookSettledAsync(invoiceId);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var snapshot = LoadSettledSnapshot(invoiceId);

        var intruderClient = CreateAuthenticatedClient(intruderId, "intruder", "user");
        var detailsResponse = await intruderClient.GetAsync($"/orders/{snapshot.OrderId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        Assert.True(
            html.Contains("Transacao nao encontrada ou acesso negado.", StringComparison.Ordinal)
            || html.Contains("Transaction not found or access denied.", StringComparison.Ordinal)
            || html.Contains("Transaccion no encontrada o acceso denegado.", StringComparison.Ordinal),
            "Expected localized order access-denied text in HTML response.");
    }

    [Fact]
    public async Task SellerCanOpenOrderDetails_AfterWebhookCreatesOrder()
    {
        const string invoiceId = "inv-flow-seller-1";
        const string buyerId = "buyer-flow-seller";
        const string sellerId = "seller-flow-seller";
        const string productName = "Produto fluxo vendedor";

        await SeedPaymentAsync(invoiceId, buyerId, sellerId, productName);

        var webhookResponse = await TriggerWebhookSettledAsync(invoiceId);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var snapshot = LoadSettledSnapshot(invoiceId);
        Assert.Equal(sellerId, snapshot.SellerId);

        var sellerClient = CreateAuthenticatedClient(sellerId, "seller");
        var detailsResponse = await sellerClient.GetAsync($"/orders/{snapshot.OrderId}");
        _ = await detailsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
    }

    [Fact]
    public async Task AdminCanOpenOrderDetails_AfterWebhookCreatesOrder()
    {
        const string invoiceId = "inv-flow-admin-1";
        const string buyerId = "buyer-flow-admin";
        const string sellerId = "seller-flow-admin";
        const string productName = "Produto fluxo admin";

        await SeedPaymentAsync(invoiceId, buyerId, sellerId, productName);

        var webhookResponse = await TriggerWebhookSettledAsync(invoiceId);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var snapshot = LoadSettledSnapshot(invoiceId);

        var adminClient = CreateAuthenticatedClient("admin-flow-user", "adminflow", "admin");
        var detailsResponse = await adminClient.GetAsync($"/orders/{snapshot.OrderId}");
        _ = await detailsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
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

    private HttpClient CreateAuthenticatedClient(string userId, string userName, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-UserName", userName);

        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(',', roles));

        return client;
    }

    private async Task SeedPaymentAsync(string invoiceId, string buyerId, string sellerId, string productName)
    {
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

    private PurchaseFlowSnapshot LoadSettledSnapshot(string invoiceId, bool requireSingleOrder = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var payment = db.Payments.FirstOrDefault(p => p.PaymentId == invoiceId);
        Assert.NotNull(payment);
        Assert.True(payment!.IsPaid);

        OrderModel? order;
        if (requireSingleOrder)
        {
            var orders = db.Orders.Where(o => o.PaymentId == payment.Id).ToList();
            Assert.Single(orders);
            order = orders[0];
        }
        else
        {
            order = db.Orders.FirstOrDefault(o => o.PaymentId == payment.Id);
            Assert.NotNull(order);
        }

        Assert.Equal(PaymentStatus.AguardandoEntrega, order!.Status);
        Assert.Equal(order.Id, payment.OrderId);
        Assert.False(string.IsNullOrWhiteSpace(order.SellerId));

        var product = db.Products.FirstOrDefault(p => p.Id == payment.ProductId);
        Assert.NotNull(product);

        return new PurchaseFlowSnapshot(
            OrderId: order.Id,
            ProductId: product!.Id,
            BuyerId: order.BuyerId ?? string.Empty,
            SellerId: order.SellerId!,
            ProductName: product.Name);
    }

    private sealed record PurchaseFlowSnapshot(int OrderId, int ProductId, string BuyerId, string SellerId, string ProductName);
}


