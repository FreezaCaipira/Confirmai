using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;
using Confirmai.Services;

namespace Confirmai.Tests;

public class ServerIntegrationEndpointsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public ServerIntegrationEndpointsIntegrationTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPendingTrades_ReturnsOnlyPendingTradesForAuthenticatedServer()
    {
        const int serverAId = 1001;
        const int serverBId = 1002;

        string apiKey;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.AddRange(
                new TibiaServer { Id = serverAId, Name = "Server A", IsActive = true },
                new TibiaServer { Id = serverBId, Name = "Server B", IsActive = true });

            db.Products.AddRange(
                new Product { Id = 2001, Name = "Crystal Coin", Description = "A", Price = 0.001m, UserId = "seller-a" },
                new Product { Id = 2002, Name = "Magic Plate Armor", Description = "B", Price = 0.002m, UserId = "seller-b" },
                new Product { Id = 2003, Name = "Boots of Haste", Description = "C", Price = 0.003m, UserId = "seller-c" });

            db.Payments.AddRange(
                new PaymentRecord { Id = 3001, ServerId = serverAId, ProductId = 2001, UserId = "buyer-a", SellerId = "seller-a", Amount = 0.001m, PaymentId = "inv-a", IsPaid = true, InGamePlayerName = "Knight Alpha" },
                new PaymentRecord { Id = 3002, ServerId = serverBId, ProductId = 2002, UserId = "buyer-b", SellerId = "seller-b", Amount = 0.002m, PaymentId = "inv-b", IsPaid = true },
                new PaymentRecord { Id = 3003, ServerId = serverAId, ProductId = 2003, UserId = "buyer-c", SellerId = "seller-c", Amount = 0.003m, PaymentId = "inv-c", IsPaid = true });

            db.Orders.AddRange(
                new OrderModel
                {
                    Id = 4001,
                    ServerId = serverAId,
                    ProductId = 2001,
                    BuyerId = "buyer-a",
                    SellerId = "seller-a",
                    Amount = 0.001m,
                    IsPaid = true,
                    PaymentId = 3001,
                    Status = PaymentStatus.AguardandoEntregaInGame
                },
                new OrderModel
                {
                    Id = 4002,
                    ServerId = serverBId,
                    ProductId = 2002,
                    BuyerId = "buyer-b",
                    SellerId = "seller-b",
                    Amount = 0.002m,
                    IsPaid = true,
                    PaymentId = 3002,
                    Status = PaymentStatus.AguardandoEntregaInGame
                },
                new OrderModel
                {
                    Id = 4003,
                    ServerId = serverAId,
                    ProductId = 2003,
                    BuyerId = "buyer-c",
                    SellerId = "seller-c",
                    Amount = 0.003m,
                    IsPaid = true,
                    PaymentId = 3003,
                    Status = PaymentStatus.Finalizado,
                    FundsReleased = true
                });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverAId, "integration-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/trades/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PendingTradesEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal(serverAId, payload!.ServerId);
        Assert.Single(payload.Trades);

        var trade = payload.Trades[0];
        Assert.Equal(4001, trade.Id);
        Assert.Equal(2001, trade.ProductId);
        Assert.Equal("Crystal Coin", trade.ProductName);
        Assert.Equal("buyer-a", trade.BuyerUserId);
        Assert.Equal("seller-a", trade.SellerUserId);
        Assert.Equal("Knight Alpha", trade.PlayerName);
        Assert.Equal("inv-a", trade.PaymentInvoiceId);
        Assert.Equal("AguardandoEntregaInGame", trade.Status);
    }

    [Fact]
    public async Task GetPendingTrades_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/server/trades/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmTrade_TransitionsPendingTradeToAwaitingAdminReview()
    {
        const int serverId = 1101;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Server Confirm", IsActive = true });
            db.Products.Add(new Product { Id = 2101, Name = "Demon Armor", Description = "Armor", Price = 0.005m, UserId = "seller-confirm" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 3101,
                ServerId = serverId,
                ProductId = 2101,
                UserId = "buyer-confirm",
                SellerId = "seller-confirm",
                Amount = 0.005m,
                PaymentId = "inv-confirm",
                IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 4101,
                ServerId = serverId,
                ProductId = 2101,
                BuyerId = "buyer-confirm",
                SellerId = "seller-confirm",
                Amount = 0.005m,
                IsPaid = true,
                PaymentId = 3101,
                Status = PaymentStatus.AguardandoEntregaInGame
            });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "confirm-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/confirm", new { tradeId = 4101 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = verifyDb.Orders.First(o => o.Id == 4101);

        Assert.True(order.IsDelivered);
        Assert.True(order.DeliveryPendingApproval);
        Assert.NotNull(order.DeliveredAt);
        Assert.Equal(PaymentStatus.AguardandoRevisaoAdm, order.Status);
    }

    [Fact]
    public async Task ConfirmTrade_ReturnsNotFound_WhenTradeDoesNotBelongToAuthenticatedServer()
    {
        const int authenticatedServerId = 1201;
        const int otherServerId = 1202;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.AddRange(
                new TibiaServer { Id = authenticatedServerId, Name = "Server Auth", IsActive = true },
                new TibiaServer { Id = otherServerId, Name = "Server Other", IsActive = true });
            db.Products.Add(new Product { Id = 2201, Name = "SSA", Description = "Amulet", Price = 0.01m, UserId = "seller-other" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 3201,
                ServerId = otherServerId,
                ProductId = 2201,
                UserId = "buyer-other",
                SellerId = "seller-other",
                Amount = 0.01m,
                PaymentId = "inv-other",
                IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 4201,
                ServerId = otherServerId,
                ProductId = 2201,
                BuyerId = "buyer-other",
                SellerId = "seller-other",
                Amount = 0.01m,
                IsPaid = true,
                PaymentId = 3201,
                Status = PaymentStatus.AguardandoEntregaInGame
            });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(authenticatedServerId, "confirm-notfound-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/confirm", new { tradeId = 4201 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmTrade_ReturnsBadRequest_WhenTradeStatusIsInvalid()
    {
        const int serverId = 1301;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Server Invalid Status", IsActive = true });
            db.Products.Add(new Product { Id = 2301, Name = "UH Rune", Description = "Rune", Price = 0.004m, UserId = "seller-invalid" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 3301,
                ServerId = serverId,
                ProductId = 2301,
                UserId = "buyer-invalid",
                SellerId = "seller-invalid",
                Amount = 0.004m,
                PaymentId = "inv-invalid",
                IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 4301,
                ServerId = serverId,
                ProductId = 2301,
                BuyerId = "buyer-invalid",
                SellerId = "seller-invalid",
                Amount = 0.004m,
                IsPaid = true,
                PaymentId = 3301,
                Status = PaymentStatus.Finalizado,
                FundsReleased = true
            });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "confirm-invalid-status-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/confirm", new { tradeId = 4301 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOffers_ReturnsOnlyActiveOffersForAuthenticatedServerAndItem()
    {
        const int serverAId = 1401;
        const int serverBId = 1402;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.AddRange(
                new TibiaServer { Id = serverAId, Name = "Offer Server A", IsActive = true },
                new TibiaServer { Id = serverBId, Name = "Offer Server B", IsActive = true });

            db.Users.AddRange(
                new ApplicationUser { Id = "seller-a", UserName = "SellerA" },
                new ApplicationUser { Id = "seller-b", UserName = "SellerB" });

            db.Products.AddRange(
                new Product { Id = 2401, Name = "Crystal Coin", Description = "A", Price = 0.001m, UserId = "seller-a" },
                new Product { Id = 2402, Name = "Magic Sword", Description = "B", Price = 0.002m, UserId = "seller-b" });

            db.ItemOffers.AddRange(
                new ItemOffer
                {
                    Id = 5001,
                    ServerId = serverAId,
                    ItemKey = "CRYSTAL COIN",
                    ItemName = "Crystal Coin",
                    Quantity = 15,
                    UnitPrice = 0.001m,
                    PricingGateway = "btc",
                    SellerUserId = "seller-a",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new ItemOffer
                {
                    Id = 5002,
                    ServerId = serverAId,
                    ItemKey = "CRYSTAL COIN",
                    ItemName = "Crystal Coin",
                    Quantity = 20,
                    UnitPrice = 0.0015m,
                    PricingGateway = "btc",
                    SellerUserId = "seller-a",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-2)
                },
                new ItemOffer
                {
                    Id = 5003,
                    ServerId = serverBId,
                    ItemKey = "CRYSTAL COIN",
                    ItemName = "Crystal Coin",
                    Quantity = 25,
                    UnitPrice = 0.002m,
                    PricingGateway = "btc",
                    SellerUserId = "seller-b",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                });

            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverAId, "offers-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/offers?itemKey=crystal%20coin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<OffersEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal(serverAId, payload!.ServerId);
        Assert.Equal("crystal coin", payload.ItemKey);
        Assert.Equal(2, payload.Offers.Count);
        Assert.Contains(payload.Offers, o => o.OfferId == 5001);
        Assert.Contains(payload.Offers, o => o.OfferId == 5002);

        var offer = payload.Offers.First(o => o.OfferId == 5001);
        Assert.Equal(2401, offer.ProductId);
        Assert.Equal("CRYSTAL COIN", offer.ItemKey);
        Assert.Equal("Crystal Coin", offer.ItemName);
        Assert.Equal(15, offer.Quantity);
        Assert.Equal(0.001m, offer.UnitPrice);
        Assert.Equal("seller-a", offer.SellerUserId);
        Assert.Equal("SellerA", offer.SellerUserName);
    }

    [Fact]
    public async Task GetOffers_ReturnsBadRequest_WhenItemKeyIsMissing()
    {
        const int serverId = 1501;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Offer Server", IsActive = true });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "offers-validation-test");
            apiKey = created.RawKey!;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.GetAsync("/api/v1/server/offers");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class PendingTradesEnvelope
    {
        public int ServerId { get; set; }
        public List<PendingTradeDto> Trades { get; set; } = new();
    }

    private sealed class PendingTradeDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BuyerUserId { get; set; } = string.Empty;
        public string? SellerUserId { get; set; }
        public string? PlayerName { get; set; }
        public string? PaymentInvoiceId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class OffersEnvelope
    {
        public int ServerId { get; set; }
        public string? ItemKey { get; set; }
        public List<OfferDto> Offers { get; set; } = new();
    }

    private sealed class OfferDto
    {
        public int OfferId { get; set; }
        public int ProductId { get; set; }
        public string ItemKey { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string SellerUserId { get; set; } = string.Empty;
        public string SellerUserName { get; set; } = string.Empty;
    }

    // ── Reject trade ─────────────────────────────────────────────────

    [Fact]
    public async Task RejectTrade_TransitionsPendingTradeToDisputa()
    {
        const int serverId = 1801;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Reject Server", IsActive = true });
            db.Products.Add(new Product { Id = 2801, Name = "Void Boots", Description = "b", Price = 0.008m, UserId = "seller-r" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 3801, ServerId = serverId, ProductId = 2801,
                UserId = "buyer-r", SellerId = "seller-r",
                Amount = 0.008m, PaymentId = "inv-reject", IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 4801, ServerId = serverId, ProductId = 2801,
                BuyerId = "buyer-r", SellerId = "seller-r",
                Amount = 0.008m, IsPaid = true, PaymentId = 3801,
                Status = PaymentStatus.AguardandoEntregaInGame
            });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "reject-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/reject",
            new { tradeId = 4801, reason = "player offline" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RejectTradeResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Rejected);
        Assert.Equal("Disputa", body.Status);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = verifyDb.Orders.First(o => o.Id == 4801);
        Assert.Equal(PaymentStatus.Disputa, order.Status);
    }

    [Fact]
    public async Task RejectTrade_ReturnsNotFound_WhenTradeDoesNotBelongToServer()
    {
        const int authenticatedServerId = 1901;
        const int otherServerId = 1902;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.AddRange(
                new TibiaServer { Id = authenticatedServerId, Name = "Reject Auth", IsActive = true },
                new TibiaServer { Id = otherServerId, Name = "Reject Other", IsActive = true });
            db.Products.Add(new Product { Id = 2901, Name = "Item", Description = "d", Price = 0.01m, UserId = "s" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 3901, ServerId = otherServerId, ProductId = 2901,
                UserId = "b", SellerId = "s", Amount = 0.01m, PaymentId = "inv-r-other", IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 4901, ServerId = otherServerId, ProductId = 2901,
                BuyerId = "b", SellerId = "s", Amount = 0.01m, IsPaid = true, PaymentId = 3901,
                Status = PaymentStatus.AguardandoEntregaInGame
            });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(authenticatedServerId, "reject-notfound-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/reject",
            new { tradeId = 4901 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RejectTrade_ReturnsBadRequest_WhenTradeStatusIsInvalid()
    {
        const int serverId = 2001;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Reject BadStatus", IsActive = true });
            db.Products.Add(new Product { Id = 3001, Name = "P", Description = "d", Price = 0.01m, UserId = "s" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 4001, ServerId = serverId, ProductId = 3001,
                UserId = "b", SellerId = "s", Amount = 0.01m, PaymentId = "inv-r-bad", IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 5001, ServerId = serverId, ProductId = 3001,
                BuyerId = "b", SellerId = "s", Amount = 0.01m, IsPaid = true, PaymentId = 4001,
                Status = PaymentStatus.Finalizado, FundsReleased = true
            });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "reject-badstatus-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/reject",
            new { tradeId = 5001 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectTrade_WritesDeliveryAuditLog()
    {
        const int serverId = 2101;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Audit Reject Server", IsActive = true });
            db.Products.Add(new Product { Id = 3101, Name = "P", Description = "d", Price = 0.01m, UserId = "s" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 4101, ServerId = serverId, ProductId = 3101,
                UserId = "b", SellerId = "s", Amount = 0.01m, PaymentId = "inv-audit-reject", IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 5101, ServerId = serverId, ProductId = 3101,
                BuyerId = "b", SellerId = "s", Amount = 0.01m, IsPaid = true, PaymentId = 4101,
                Status = PaymentStatus.AguardandoEntregaInGame
            });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "reject-audit-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/reject",
            new { tradeId = 5101, reason = "inventory full" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditLog = verifyDb.DeliveryAuditLogs
            .FirstOrDefault(l => l.OrderId == 5101 && l.EventType == "RejectTrade");
        Assert.NotNull(auditLog);
        Assert.False(auditLog!.Success);
        Assert.Contains("inventory full", auditLog.Detail);
    }

    [Fact]
    public async Task ConfirmTrade_WritesDeliveryAuditLog()
    {
        const int serverId = 2201;
        string apiKey;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ServerApiKeyService>();

            db.Servers.Add(new TibiaServer { Id = serverId, Name = "Audit Confirm Server", IsActive = true });
            db.Products.Add(new Product { Id = 3201, Name = "P", Description = "d", Price = 0.01m, UserId = "s" });
            db.Payments.Add(new PaymentRecord
            {
                Id = 4201, ServerId = serverId, ProductId = 3201,
                UserId = "b", SellerId = "s", Amount = 0.01m, PaymentId = "inv-audit-confirm", IsPaid = true
            });
            db.Orders.Add(new OrderModel
            {
                Id = 5201, ServerId = serverId, ProductId = 3201,
                BuyerId = "b", SellerId = "s", Amount = 0.01m, IsPaid = true, PaymentId = 4201,
                Status = PaymentStatus.AguardandoEntregaInGame
            });
            await db.SaveChangesAsync();

            var created = await apiKeyService.CreateKeyAsync(serverId, "confirm-audit-test");
            apiKey = created.RawKey!;
        }

        await _factory.EnsureLuaDeliveryEnabledAsync();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var response = await client.PostAsJsonAsync("/api/v1/server/trades/confirm",
            new { tradeId = 5201 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditLog = verifyDb.DeliveryAuditLogs
            .FirstOrDefault(l => l.OrderId == 5201 && l.EventType == "ConfirmTrade");
        Assert.NotNull(auditLog);
        Assert.True(auditLog!.Success);
    }

    private sealed class RejectTradeResponse
    {
        public int ServerId { get; set; }
        public int TradeId { get; set; }
        public bool Rejected { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}